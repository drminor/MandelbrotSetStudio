using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSS.Common
{
	public delegate JobTreePath AddAJobToATree(Job job, IJobTreeBranch tree);

	/// <remarks>
	///	New jobs are inserted into order by the date the job was created.
	///	The new job's Parent identifies the job from which this job was created.
	///
	///	New jobs are added as a sibling to it "parent"
	///	a) if the new Job is being added as the last job
	///	b) the job is not a Zoom-In or Zoom-Out and the job that is currently the last is not a Zoom-In or Zoom-Out				
	///  
	///	Otherwise new jobs are added as a "Parked Alternate" to the Job just before the point of insertion.
	///	
	///	Here are the steps:
	///		1. Determine the point of insertion
	///		2. If the job will be added to the end or if it is not a Zoom-In or Zoom-Out and the job currently in the last position is not a Zoom-In or Zoom-Out type job
	///		then insert the new job just before the job at the insertion point.
	///
	///		3. Otherwise
	///			a. Add the new Job as child of job currently at the point of insertion(if the job is a Zoom-In or Zoom-Out type job
	///			or b. Add the job as a child of the job currently in the last position (if the job currently in the last position is Zoom-In or Zoom-Out type job.
	///			c. Make the new newly added node, active by calling MakeBranchActive
	/// </remarks>

	public class JobTree : IJobTree
	{
		private readonly ReaderWriterLockSlim _jobsLock;
		private readonly IJobTreeBranch _root;

		private JobTreePath? _currentPath;
		private JobTreeItem? _selectedItem;

		#region Constructor

		public JobTree(List<Job> jobs, bool checkHomeJob)
		{
			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

			jobs = jobs.OrderBy(x => x.Id.ToString()).ToList();
			ReportInput(jobs);

			Debug.WriteLine($"Loading {jobs.Count()} jobs.");

			AddAJobToATree addMethodExp = AddInternalExp;
			var jobTreeBranch = BuildTree(jobs, checkHomeJob, addMethodExp, out var currentPath);
			ReportOutput(jobTreeBranch, currentPath);

			AddAJobToATree addMethod1 = AddInternal;
			_root = BuildTree(jobs, checkHomeJob, addMethod1, out _currentPath);
			ReportOutput(_root, _currentPath);

			//Debug.Assert(_root.RootItem.Job.Id == ObjectId.Empty, "Creating a Root JobTreeItem that has a JobId != ObjectId.Empty.");
			Debug.Assert(!IsDirty, "IsDirty should be false as the constructor is exited.");
		}

		#endregion

		#region Public Properties

		public ObservableCollection<JobTreeItem> JobItems => _root.Children;

		public Job CurrentJob
		{
			get => DoWithReadLock(() =>
			{
				var currentJob = _currentPath?.Job;
				if (currentJob == null)
				{
					Debug.WriteLine("WARNING: In CurrentJob:Getter, the CurrentPath is null. Returning the Home Job.");
					currentJob = JobItems[0].Job;
				}
				return currentJob;
			});
			
			set => DoWithWriteLock(() =>
			{
				if (value != _currentPath?.Job)
				{
					if (value != null)
					{
						if (!MoveCurrentTo(value, _root, out _currentPath))
						{
							Debug.WriteLine($"WARNING: Could not MoveCurrent to job: {value.Id}.");
						}
					}
				}
			});
		}

		// TODO: Consider having the Job Tree keep track of "CanGoBack" / "CanGoForward" as to make these real properties.
		public bool CanGoBack => DoWithReadLock(() => { return CanMoveBack(_currentPath); });

		public bool CanGoForward => DoWithReadLock(() => { return CanMoveForward(_currentPath); });

		public bool IsDirty { get; set; }

		public bool AnyJobIsDirty => DoWithReadLock(() => { return GetJobs(_root).Any(x => x.IsDirty); });

		public JobTreeItem? SelectedItem
		{
			get => _selectedItem;
			set
			{
				if (value != _selectedItem)
				{
					UpdateIsSelected(_selectedItem, false, UseRealRelationShipsToUpdateSelected, _root);
					_selectedItem = value;
					UpdateIsSelected(_selectedItem, true, UseRealRelationShipsToUpdateSelected, _root);
				}
			}
		}

		public bool UseRealRelationShipsToUpdateSelected { get; set; }

		#endregion

		#region Public Methods

		public JobTreePath Add(Job job, bool selectTheAddedJob)
		{
			_jobsLock.EnterWriteLock();

			JobTreePath newPath;

			try
			{
				newPath = AddInternal(job, currentBranch: _root);
				IsDirty = true;
			}
			finally
			{
				_jobsLock.ExitWriteLock();
			}

			if (selectTheAddedJob)
			{
				ExpandAndSetCurrent(newPath);
				_currentPath = newPath;
			}

			return newPath;
		}

		public bool RestoreBranch(ObjectId jobId)
		{
			Debug.WriteLine($"Restoring Branch: {jobId}.");

			// TODO: RestoreBranch does not support CanvasSizeUpdateJobs
			if (!TryFindJobPathById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
			{
				throw new InvalidOperationException($"Cannot find job: {jobId} that is being restored.");
			}

			while(path != null && !path.IsParkedAlternate)
			{
				path = path.GetParentPath();
			}

			if (path == null || !path.IsParkedAlternate)
			{
				throw new InvalidOperationException("Cannot restore this branch, it is not a \"parked\" alternate.");
			}

			var result = RestoreBranch(path);

			return result;
		}

		public bool RestoreBranch(JobTreePath path)
		{
			JobTreePath newPath;

			var node = path.Item;

			if (node.TransformType == TransformType.CanvasSizeUpdate)
			{
				var parentPath = path.GetParentPath()!;
				newPath = MakeBranchActive(parentPath).Combine(node);
			}
			else
			{
				newPath = MakeBranchActive(path);
			}

			ExpandAndSetCurrent(newPath);
			_currentPath = newPath;
			IsDirty = true;
			return true;
		}

		public bool RemoveBranch(ObjectId jobId)
		{
			// TODO: RemoveBranch does not support removing CanvasSizeUpdate nodes.
			if (!TryFindJobPathById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
			{
				return false;
			}

			var result = RemoveBranch(path);
			return result;
		}
		
		public bool RemoveBranch(JobTreePath path)
		{
			var jobTreeItem = path.Item;

			if (jobTreeItem.IsRoot || jobTreeItem.IsHome)
			{
				throw new InvalidOperationException("Removing the Home node is not yet supported.");
			}

			bool result;

			if (path.TransformType == TransformType.CanvasSizeUpdate)
			{
				var csuParentItem = path.GetParentPath()!.Item;
				result = csuParentItem.Remove(jobTreeItem);
				IsDirty = true;

				return result;
			}

			if (path.IsActiveAlternate)
			{
				// Park this item by restoring the most recent alternate branches before removing.
				Debug.WriteLine($"Making the branch being removed a parked alternate.");

				var selectedParkedAltPath = SelectMostRecentAlternate(path);
				var activeAltPath = MakeBranchActive(selectedParkedAltPath);

				// Update the path to reflect that the item being removed is now a child of the newly activated alternate.
				path = activeAltPath.Combine(jobTreeItem);
			}

			var parentItem = path.GetParentItemOrRoot();
			var idx = parentItem.Children.IndexOf(jobTreeItem);

			if (path.TransformType == TransformType.ZoomIn)
			{
				// TODO: Determine if this JobTreeItem with TransformType = ZoomIn is the last node of the current branch as we execute RemoveBranch
			}

			if (idx == 0)
			{
				_currentPath = parentItem.IsRoot ? path.GetRoot().Combine(parentItem.Children[0]) : path.GetParentPath();
			}
			else
			{
				_currentPath = path.CreateSiblingPath(parentItem.Children[idx - 1]);
			}

			// TODO: Examine how we "tear" down a set of JobTreeItems.
			result = parentItem.Remove(jobTreeItem);

			if (_currentPath != null)
			{
				ExpandAndSetCurrent(_currentPath);
			}

			IsDirty = true;

			return result;
		}

		public bool TryGetPreviousJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job)
		{
			if (_currentPath == null)
			{
				job = null;
				return false;
			}

			var backPath = GetPreviousJobPath(_currentPath, skipPanJobs);
			job = backPath?.Job;

			return job != null;
		}

		public bool MoveBack(bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var backPath = GetPreviousJobPath(_currentPath, skipPanJobs);

			if (backPath != null)
			{
				_currentPath = backPath;
				ExpandAndSetCurrent(backPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetNextJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job)
		{
			if (_currentPath == null)
			{
				job = null;
				return false;
			}

			var forwardPath = GetNextJobPath(_currentPath, skipPanJobs);
			job = forwardPath?.Job;

			return job != null;
		}

		public bool MoveForward(bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var forwardPath = GetNextJobPath(_currentPath, skipPanJobs);

			if (forwardPath != null)
			{
				_currentPath = forwardPath;
				ExpandAndSetCurrent(forwardPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy)
		{
			if (job.ParentJobId == null)
			{
				throw new ArgumentException("The job must have a non-null ParentJobId.", nameof(job));
			}

			_jobsLock.EnterUpgradeableReadLock();

			try
			{
				if (TryFindJobParentPath(job, _root, out var parentPath))
				{
					JobTreeItem parentNode;

					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						// The parentPath points to the "original job" for which the CanvasSizeUpdate job was created.
						// We need to get its parentPath to continue.
						parentNode = parentPath.GetParentPath()!.Item;
					}
					else
					{
						parentNode = parentPath.Item;;
					}

					var proxyJobTreeItem = parentNode.AlternateDispSizes?.FirstOrDefault(x => x.Job.CanvasSizeInBlocks == canvasSizeInBlocks);
					proxy = proxyJobTreeItem?.Job;
					return proxy != null;
				}
				else
				{
					proxy = null;
					return false;
				}
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		#endregion

		#region Public Methods - Collection

		public JobTreePath? GetCurrentPath()
		{
			return DoWithReadLock(() =>
			{
				return _currentPath == null ? null : _currentPath;
			});
		}

		public JobTreePath? GetPath(ObjectId jobId)
		{
			// TODO: GetPath does not support getting CanvasSizeUpdate Jobs.
			var path = DoWithReadLock(() =>
			{
				return GetJobPathById(jobId, _root, includeCanvasSizeUpdateJobs: false);
			});

			return path;
		}

		public IEnumerable<Job> GetJobs()
		{
			_jobsLock.EnterReadLock();

			try
			{
				var result = GetJobs(_root);
				return result;
			}
			finally
			{
				_jobsLock.ExitReadLock();
			}
		}

		public Job? GetJob(ObjectId jobId)
		{
			_ = TryFindJob(jobId, _root, includeCanvasSizeUpdateJobs: false, out var result);
			return result;
		}

		public Job? GetParent(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindJob(job.ParentJobId.Value, _root, includeCanvasSizeUpdateJobs: false, out var result);
				return result;
			}
		}

		public List<Job>? GetJobAndDescendants(ObjectId jobId)
		{
			return DoWithReadLock(() =>
			{
				List<Job>? result;

				if (TryFindJobPathById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
				{
					result = new List<Job> { path.Item.Job };
					result.AddRange(GetJobs(path));
				}
				else
				{
					result = null;
				}

				return result;
			});
		}

		#endregion

		#region Private Add Methods

		private JobTreePath AddInternal(Job job, IJobTreeBranch currentBranch)
		{
			JobTreePath newPath;
			
			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				newPath = AddCanvasSizeUpdateJob(job, currentBranch);
			}
			else
			{
				var parentPath = GetJobParentPath(job, currentBranch);
				newPath = AddAtParentPath(job, parentPath);
			}

			return newPath;
		}

		private JobTreePath AddCanvasSizeUpdateJob(Job job, IJobTreeBranch currentBranch)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's TransformType is {job.TransformType}.");
			}

			if (job.ParentJobId == null)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's parentJobId is null.");
			}

			var parentPath = GetJobParentPath(job, currentBranch);
			var parentNode = parentPath.Item;;

			var canvasSizeUpdateNode = parentNode.AddCanvasSizeUpdateJob(job);
			var newPath = parentPath.Combine(canvasSizeUpdateNode);

			return newPath;
		}

		private JobTreePath AddAtParentPath(Job job, JobTreePath parentPath)
		{
			JobTreePath newPath;

			// This is the JobTreeItem for the new Job's real parent.
			var parentNode = parentPath.Item;

			// Add the new Job to the list of it's parent's "real" children.
			// The index is the position of the new job among its siblings which are sorted by the CreatedDate, ascending.
			var index = parentNode.AddRealChild(job);

			// Get the job already in the tree for which the job being added will directly follow.
			JobTreePath preceedingPath;

			if (index == 0)
			{
				// Find the sibling of the parent, that comes just before the parent.
				if (parentPath.TryGetParentPath(out var grandparentPath))
				{
					var grandparentId = grandparentPath.Job.Id;
					var grandparentBranch = grandparentPath.GetParentBranch();

					if (TryFindJobPathById(grandparentId, grandparentBranch, includeCanvasSizeUpdateJobs: false, out var realGrandparentPath))
					{
						preceedingPath = realGrandparentPath;
					}
					else
					{
						throw new InvalidOperationException("Cant find a child of a known parent.");
					}
				}
				else
				{
					preceedingPath = parentPath;
				}
			}
			else
			{
				var preceedingJob1 = parentNode.RealChildJobs.Values[index - 1];
				preceedingPath = GetJobPath(preceedingJob1, parentPath.GetParentBranch());
			}

			var preceedingJob = preceedingPath.Item;

			// Does the preceeding sibling job (in date order) move the map to a different Zoom level.
			var addingJobAsAnAlt = preceedingJob.TransformType is TransformType.ZoomIn or TransformType.ZoomOut;

			if (addingJobAsAnAlt)
			{
				// Add the new node as a Parked ALT.
				newPath = AddAsParkedAlt(job, preceedingPath);
			}
			else
			{
				// Add the new node in-line after the preceeding ALT Job
				newPath = AddAfter(job, preceedingPath, parentPath);
			}

			return newPath;
		}

		private JobTreePath AddAsParkedAlt(Job job, JobTreePath preceedingPath)
		{
			JobTreePath activeAltPath;

			if (preceedingPath.IsActiveAlternate)
			{
				// The preceeding node is the Active ALT.
				// Add the new job as a sibling to the branch on which my real parent's JobTreeItem was found (in sort order by date created.)
				activeAltPath = preceedingPath;
			}
			else if (preceedingPath.IsParkedAlternate)
			{
				// The parent of the preceeding node is the Active ALT
				var parkedParentPath = preceedingPath.GetParentPath()!;
				activeAltPath = parkedParentPath;
			}
			else
			{
				// The preceeding node has not yet been made an Alternate.
				Debug.WriteLine($"Found a Job that is a new Alternate. Marking existing node: {preceedingPath.LastTerm} as the Active ALT.");
				preceedingPath.Item.IsActiveAlternate = true;
				activeAltPath = preceedingPath;
			}

			Debug.WriteLine($"Adding job: {job.Id}, as a Parked ALT to Active ALT: {activeAltPath.Job.Id}.");

			var parkedAltPath = AddJob(job, activeAltPath);

			// TODO: See if we can avoid making the just added job be on the 'Main' branch.
			var newPath = MakeBranchActive(parkedAltPath);

			return newPath;
		}

		private JobTreePath AddAfter(Job job, JobTreePath preceedingPath, JobTreePath parentPath)
		{
			JobTreePath newPath;

			if (preceedingPath.IsParkedAlternate)
			{
				if (job.ParentJobId == preceedingPath.Job.Id) // preceedingPath.LastTerm == parentPath.LastTerm)
				{
					// Add the job as a child of the Parked Alt
					newPath = AddJob(job, preceedingPath);
				}
				else
				{
					newPath = AddInLine(job, preceedingPath.GetParentPath()!);
				}
			}
			else
			{
				// Add the job as a sibling of its parent 
				newPath = AddInLine(job, preceedingPath);
			}

			return newPath;
		}

		private JobTreePath AddInLine(Job job, JobTreePath parentPath)
		{
			Debug.WriteLine($"Adding job: {job.Id}, in-line after: {parentPath.Job.Id}.");

			IJobTreeBranch parentBranchToUse = parentPath.GetParentBranch();

			if (parentPath.IsActiveAlternate)
			{
				parentBranchToUse = parentBranchToUse.GetParentBranch();
			}
			else
			{
				//Debug.Assert(!parentPath.IsParkedAlternate, "AddJobInLine is adding a job to an Active Alt node. (The value of the specified path to which we are adding in-line is a Parked Alt node.");

				// The grandparentPath points to the locgial parent. This identifies the branch on which the parent is currently attached.
				// If the parent is a PRK then this grandparent is the ALT on the active branch
				//parentBranchToUse = parentPath.GetParentBranch();

				// Add the new job as a sibling to the branch on which my real parent's JobTreeItem was found (in sort order by date created.)
				Debug.Assert(parentBranchToUse.IsEmpty || !parentBranchToUse.LastTerm?.IsActiveAlternate == true, "AddJobInLine is adding a job to an Active Alt node.");
			}

			var result = AddJob(job, parentBranchToUse);

			return result;
		}

		private JobTreePath AddJob(Job job, IJobTreeBranch parentBranch)
		{
			var parentNode = parentBranch.GetItemOrRoot();
			var newNode = parentNode.AddJob(job);

			if (parentNode.IsActiveAlternate)
			{
				newNode.IsParkedAlternate = true;
			}

			var result = parentBranch.Combine(newNode);

			return result;
		}

		#endregion

		#region Private Add Methods -- Experimental

		private JobTreePath AddInternalExp(Job job, IJobTreeBranch currentBranch)
		{
			JobTreePath newPath;

			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				newPath = AddCanvasSizeUpdateJob(job, currentBranch);
			}
			else
			{
				var parentPath = GetJobParentPath(job, currentBranch);
				newPath = AddAtParentPathExp(job, parentPath);
			}

			return newPath;
		}

		private JobTreePath AddAtParentPathExp(Job job, JobTreePath parentPath)
		{
			JobTreePath newPath;

			// This is the JobTreeItem for the new Job's real parent.
			var parentNode = parentPath.Item;

			// Add the new Job to the list of it's parent's "real" children.
			// The index is the position of the new job among its siblings which are sorted by the CreatedDate, ascending.
			var index = parentNode.AddRealChild(job);

			// Get the job already in the tree for which the job being added will directly follow.
			JobTreePath preceedingPath;

			if (index == 0)
			{
				Debug.Assert(parentNode.RealChildJobs.Count == 1, "Our index is Zero, but we're not the first.");

				// Find the sibling of the parent, that comes just before the parent.
				if (parentPath.TryGetParentPath(out var grandparentPath))
				{
					var grandparentId = grandparentPath.Job.Id;
					var grandparentBranch = grandparentPath.GetParentBranch();

					if (TryFindJobPathById(grandparentId, grandparentBranch, includeCanvasSizeUpdateJobs: false, out var realGrandparentPath))
					{
						preceedingPath = realGrandparentPath;
					}
					else
					{
						throw new InvalidOperationException("Cant find a child of a known parent.");
					}
				}
				else
				{
					preceedingPath = parentPath;
				}
			}
			else
			{
				var preceedingJob1 = parentNode.RealChildJobs.Values[index - 1];
				preceedingPath = GetJobPath(preceedingJob1, parentPath.GetParentBranch());
			}

			var preceedingJob = preceedingPath.Item;

			// Does the preceeding sibling job (in date order) move the map to a different Zoom level.
			var addingJobAsAnAlt = preceedingJob.TransformType is TransformType.ZoomIn or TransformType.ZoomOut;

			if (addingJobAsAnAlt)
			{
				// Add the new node as a child of the preceeding job.
				newPath = AddJobExp(job, preceedingPath);
			}
			else
			{
				// Add the new node in-line after the preceeding Job
				newPath = AddInLineExp(job, preceedingPath);
			}

			return newPath;
		}

		private JobTreePath AddInLineExp(Job job, JobTreePath parentPath)
		{
			Debug.WriteLine($"Adding job: {job.Id}, in-line after: {parentPath.Job.Id}.");
			var grandparentBranch = parentPath.GetParentBranch();
			var result = AddJobExp(job, grandparentBranch);

			return result;
		}

		private JobTreePath AddJobExp(Job job, IJobTreeBranch parentBranch)
		{
			var parentNode = parentBranch.GetItemOrRoot();
			var newNode = parentNode.AddJob(job);
			var result = parentBranch.Combine(newNode);

			return result;
		}

		#endregion

		#region Private Branch Methods

		/// <summary>
		///	1.  Get the jobTreeItem
		///	2.  Find its Parent(The active alternate that will be parked)
		///	3.  Remove the jobTreeItem from its Parent
		///	4.  Get a list of our children and remove them.
		///	
		///			-- SwitchAltBranches --
		///	5.  Get a list of all items from the main branch starting with the item just after the Active Alt, aka Parent
		///	6.  The current Alt becomes our child -- [The Alternate currently in the main trunk]
		///	7.  Move all children of this first child[The other alternates] and make them our children
		///	8.  The items that were identified in step 5 are then added as children once current, now parked Alt
		///			-- SwitchAltBranches
		///	
		///	9.  Move us from being a child of the(once Active) now Parked Alt to be a child of the Grandparent
		///	10. Move our children to the Grandparent(following us.)
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private JobTreePath MakeBranchActive(JobTreePath path)
		{
			if (path.IsEmpty)
			{
				throw new ArgumentException("Path cannot be empty.");
			}

			var parkedAltNode = path.Item;

			if (parkedAltNode.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException("MakeActiveBranch does not support CanvasSizeUpdates.");
			}

			var activeAltNode = parkedAltNode.ParentNode;

			if (activeAltNode is null)
			{
				throw new InvalidOperationException("Call to MakeBranchActive found no Active Alt parent of the Parked Alt being made active.");
			}

			// The parked ALT's children contains all of the job following the parked ALT.
			var ourChildern = new List<JobTreeItem>(parkedAltNode.Children);

			SwitchAltBranches(parkedAltNode, activeAltNode);

			var parentPath = path.GetParentPath()!;
			var grandparentItem = parentPath.GetParentItemOrRoot();
			var grandparentBranch = parentPath.GetParentBranch();

			// Move the once Parked, now Active Alt node to the grandparent
			_ = parkedAltNode.Move(grandparentItem);
			var newPath = grandparentBranch.Combine(parkedAltNode);

			// Move each job following the once Parked, now Active Alt node to the grandparent
			for (var i = 0; i < ourChildern.Count; i++)
			{
				var child = ourChildern[i];
				_ = child.Move(grandparentItem);
			}

			return newPath;
		}

		/// <summary>
		/// 1. Get a list of the Jobs following the Active Alt
		/// 2. Move the Active Alt from its parent(main trunk) to become a child of the node becoming Active, i.e., the Parked Alt node.
		/// 3. Move each child of the Active Alt to become a child of the node being parked -- excluding the node being parked.
		/// 4. Move each of the nodes following the (once) ActiveAlt to become a child of the node being parked; i.e., the (once) Active Alt node.
		/// </summary>
		/// <param name="parkedAlt"></param>
		/// <param name="activeAlt"></param>
		private void SwitchAltBranches(JobTreeItem parkedAlt, JobTreeItem activeAlt)
		{
			/* 	
				Nodes have children in three cases:
				1. The root node's children contain the list of jobs currently in play. Some of these may be active alternates.
				2. An active alternate node's children contains all of the other alternates currently not in play, aka the "parked alternates.
				3. A parked alternate node's children contains the jobs that follow this alternate that would also be made active.
			
				An active alternate may be "parked" if its part of a trunk that is parked, if that trunk is made active, this will be the active node.
			*/

			if (!parkedAlt.IsParkedAlternate)
			{
				throw new InvalidOperationException("The newNode being Switched to become the Active ALT is not a Parked ALT.");
			}

			if (!activeAlt.IsActiveAlternate)
			{
				throw new InvalidOperationException("The oldNode being Switched out to become a Parked ALT is not the Active ALT.");
			}

			var parentNode = activeAlt.ParentNode;

			if (parentNode is null)
			{
				throw new InvalidOperationException("Call to SwitchAltBranch found the Active ALT to have no parent.");
			}

			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(activeAlt);

			//var strSiblings = string.Join("; ", siblings.Select(x => x.IdAndParentId));
			//var strNewNodeChildren = string.Join("; ", newNode.Children.Select(x => x.IdAndParentId));
			//Debug.WriteLine($"CurrentAlt: {currentAlt.Job.Id}, CurrentAlt ParentNode: {parentNode.Job.Id}, Siblings = {strSiblings}. current Pos: {currentPosition}, NewNode: {newNode.IdAndParentId}, NewNode Children = {strSiblings}.");

			Debug.WriteLine($"Switching Branches. CurrentAlt: {activeAlt.Job.Id}, CurrentAlt ParentNode: {parentNode.Job.Id}. Current Pos: {currentPosition}, NewNode: {parkedAlt.IdAndParentId}.");

			// Get a list of the items after the current position of the parent. We will use this later to add them to the node being parked.
			var jobsFollowingActiveAlt = siblings.Skip(currentPosition + 1).ToList();

			// Move the Active Alt node from the grandparent node to be a child of the node becoming the Active Alt node.
			// The new Active Alt node stores all of the parked ALTs in its list of Children.
			_ = activeAlt.Move(parkedAlt);

			// Move all children of the Active Alt node to be children of the node becoming the Active Alt node. Don't move the node becoming the Active Alt node.
			var otherAlts = new List<JobTreeItem>(activeAlt.Children);
			for (var i = 0; i < otherAlts.Count; i++)
			{
				var otherAlt = otherAlts[i];
				if (otherAlt != parkedAlt)
				{
					_ = otherAlt.Move(parkedAlt);
				}
			}
			// The job being added is the active alternate among it's peer alternates..
			activeAlt.IsActiveAlternate = false;
			activeAlt.IsParkedAlternate = true;
			parkedAlt.IsActiveAlternate = true;
			parkedAlt.IsParkedAlternate = false;

			// All of the jobs that followed the Active Alt node are now added to the Parked Alt node.
			var newlyDesignatedParkedAlt = activeAlt;

			for (var i = 0; i < jobsFollowingActiveAlt.Count; i++)
			{
				var successor = jobsFollowingActiveAlt[i];
				_ = successor.Move(newlyDesignatedParkedAlt);
			}
		}

		private JobTreePath SelectMostRecentAlternate(JobTreePath currentAltPath)
		{
			var parkedAlts = currentAltPath.Children;
			var mostRecentParkedAlt = parkedAlts.Aggregate((i1, i2) => (i1.CompareTo(i2) > 0) ? i1 : i2);

			var result = currentAltPath.Combine(mostRecentParkedAlt);

			return result;
		}

		#endregion

		#region Private Load and Export Job Methods

		private IJobTreeBranch BuildTree(List<Job> jobs, bool checkHomeJob, AddAJobToATree addMethod, out JobTreePath? currentPath)
		{
			Job homeJob;

			if (checkHomeJob)
			{
				// Make it an error for their not be one and only one Job of type Home.
				homeJob = GetHomeJob(jobs);
			}
			else
			{
				// Use the first job, unconditionally
				homeJob = jobs.Take(1).First();
			}

			// CreateRoot returns a JobTreePath pointing to the homeJob.
			var root = JobTreeItem.CreateRoot(homeJob, out currentPath);

			//if (numberOfJobsWithNullParentId > 1)
			//{
			//	Debug.WriteLine($"Loading {jobs.Count()} jobs.");
			//	_xroot = BuildTree(jobs, out _xcurrentPath);
			//}
			//else
			//{
			//	Debug.WriteLine($"Loading {jobs.Count()} jobs from an older project.");
			//	_xroot = BuildTreeForOldPro(jobs, out _xcurrentPath);
			//}


			// Have BuildTree start with the homeJob, and not the root, so that it will not add the Home Job a second time.
			currentPath = BuildTree(jobs, currentPath, addMethod);

			return root;
		}

		private void ReportInput(IList<Job> jobs)
		{
			Debug.WriteLine("INPUT Report");
			Debug.WriteLine("Id\t\t\t\tParentId\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var homeJob = jobs.FirstOrDefault(x => x.TransformType == TransformType.Home);

			if (homeJob != null)
			{
				var strParentJobId = homeJob.ParentJobId.HasValue ? homeJob.ParentJobId.ToString() : "null";
				Debug.WriteLine($"{homeJob.Id}\t{strParentJobId}\t{homeJob.DateCreated}\t{homeJob.TransformType}\t{homeJob.Id.Timestamp}");
			}
			else
			{
				Debug.WriteLine("No Home Node Found.");
			}

			var wlist = jobs.Where(x => x != homeJob).OrderBy(x => x.Id.ToString()).ToList();

			foreach (var j in wlist)
			{
				var strParentJobId = j.ParentJobId.HasValue ? j.ParentJobId.ToString() : "null";
				Debug.WriteLine($"{j.Id}\t{strParentJobId}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		private void ReportOutput(IJobTreeBranch start, JobTreePath? currentPath)
		{
			Debug.WriteLine($"OUTPUT Report for currentPath: {currentPath}");
			Debug.WriteLine("Id\t\t\t\t\t\t\tParentId\t\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var jwps = GetJobsWithParentage(start);

			foreach (var jwp in jwps)
			{
				var j = jwp.Item1;
				var p = jwp.Item2;
				Debug.WriteLine($"{j.Id}\t{p?.Id.ToString() ?? "null\t\t\t\t\t"}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		private JobTreePath? BuildTree(IList<Job> jobs, IJobTreeBranch currentBranch, AddAJobToATree addMethod)
		{
			var visited = 1;
			LoadChildItems(jobs, currentBranch, addMethod, ref visited);

			if (visited != jobs.Count)
			{
				Debug.WriteLine($"WARNING: Only {visited} jobs out of {jobs.Count} were included during build.");
			}

			// Use the very top of the tree. The value of current branch given to this method may be pointing to the HomeNode instead of to the root.
			var tree = currentBranch.GetRoot();

			_ = MoveCurrentTo(jobs[0], tree, out var path);

			return path;
		}

		private Job GetHomeJob(IList<Job> jobs)
		{
			if (!jobs.Any())
			{
				throw new ArgumentException("The list of jobs cannot be empty when constructing a JobTree.", nameof(jobs));
			}

			var numberOfJobsWithNoParent = jobs.Count(x => !x.ParentJobId.HasValue && x.TransformType != TransformType.CanvasSizeUpdate);

			if (numberOfJobsWithNoParent > 1)
			{
				Debug.WriteLine($"WARNING: Found {numberOfJobsWithNoParent} jobs with a null ParentJobId. Expecting exactly one.");
			}

			var homeJob = jobs.FirstOrDefault(x => x.TransformType == TransformType.Home);

			if (homeJob == null)
			{
				throw new InvalidOperationException("There is no Job with TransformType = Home.");
			}

			return homeJob;
		}

		private void LoadChildItems(IList<Job> jobs, IJobTreeBranch currentBranch, AddAJobToATree addMethod, ref int visited)
		{
			var currentPath = currentBranch.GetCurrentPath();

			var childJobs = GetChildren(currentPath, jobs);
			foreach (var job in childJobs)
			{
				visited++;

				if (job.TransformType == TransformType.CanvasSizeUpdate)
				{
					Debug.Assert(!jobs.Any(x => x.ParentJobId == job.Id), "Found a CanvasSizeUpdateJob that has children.");
					_ = AddCanvasSizeUpdateJob(job, currentBranch);
				}
				else
				{
					//var path = AddInternal(job, currentBranch);
					var path = addMethod(job, currentBranch);

					ValidateAddInternal(job, currentBranch);

					LoadChildItems(jobs, path, addMethod, ref visited);
				}
			}
		}

		[Conditional("DEBUG")]
		private void ValidateAddInternal(Job job, IJobTreeBranch currentBranch)
		{
			if (!TryFindJobPath(job, currentBranch.GetRoot(), out _))
			{
				throw new InvalidOperationException("Cannot find job just loaded.");
			}
		}

		private IList<Job> GetChildren(JobTreePath? currentPath, IList<Job> jobs)
		{
			var parentJobId = currentPath == null ? (ObjectId?)null : currentPath.Item.Job.Id;
			parentJobId = parentJobId == ObjectId.Empty ? null : parentJobId;
			var result = jobs.Where(x => x.ParentJobId == parentJobId).OrderBy(x => x.Id.Timestamp).ToList();

			return result;
		}

		//private List<Job> PrepareRemaingJobs(IList<Job> jobs, Job homeJob)
		//{
		//	//foreach (var job in jobs)
		//	//{
		//	//	if (job.ParentJobId == null)
		//	//	{
		//	//		job.ParentJobId = homeJob.Id;
		//	//	}

		//	//	//if (job.TransformType == TransformType.CanvasSizeUpdate)
		//	//	//{
		//	//	//	job.TransformType = TransformType.ZoomOut;
		//	//	//}
		//	//}

		//	var remainingJobs = jobs.Where(x => x != homeJob).OrderBy(x => x.Id.ToString()).ToList();

		//	return remainingJobs;
		//}

		private IList<Job> GetJobs(IJobTreeBranch currentBranch)
		{
			// TODO: Consider implementing an IEnumerator<JobTreeItem> for the JobTree class.
			var result = new List<Job>();

			foreach (var child in currentBranch.Children)
			{
				result.Add(child.Job);

				if (child.AlternateDispSizes != null)
				{
					result.AddRange(child.AlternateDispSizes.Select(x => x.Job));
				}

				var jobList = GetJobs(currentBranch.Combine(child));
				result.AddRange(jobList);
			}

			return result;
		}

		private List<Tuple<Job, Job?>> GetJobsWithParentage(IJobTreeBranch currentBranch)
		{
			var result = new List<Tuple<Job, Job?>>();

			foreach (var child in currentBranch.Children)
			{
				result.Add(new Tuple<Job, Job?>(child.Job, currentBranch.Job));
				if (child.AlternateDispSizes != null)
				{
					result.AddRange
						(
							child.AlternateDispSizes.Select(x => new Tuple<Job, Job?>(x.Job, child.Job))
						);
				}

				var jobList = GetJobsWithParentage(currentBranch.Combine(child));
				result.AddRange(jobList);
			}

			return result;
		}

		#endregion

		#region Private Collection Methods, With Support for CanvasSizeUpdates

		private bool TryFindJob(ObjectId jobId, IJobTreeBranch currentBranch, bool includeCanvasSizeUpdateJobs, [MaybeNullWhen(false)] out Job job)
		{
			job = GetJobPathById(jobId, currentBranch, includeCanvasSizeUpdateJobs)?.Job;
			return job != null;
		}

		private JobTreePath? GetJobPathById(ObjectId jobId, IJobTreeBranch currentBranch, bool includeCanvasSizeUpdateJobs)
		{
			return TryFindJobPathById(jobId, currentBranch, includeCanvasSizeUpdateJobs, out var path) ? path : null;
		}

		private JobTreePath GetJobPath(Job job, IJobTreeBranch currentBranch)
		{
			return TryFindJobPath(job, currentBranch, out var path)
				? path
                : throw new InvalidOperationException($"Cannot find Job: {job.Id} in the JobTree.");
		}

		private JobTreePath GetJobParentPath(Job job, IJobTreeBranch currentBranch)
		{
			return job.ParentJobId == null
				? throw new ArgumentException("Cannot get the JobParentPath if the Job's ParentJobId is null.")
				: TryFindJobPathById(job.ParentJobId.Value, currentBranch, includeCanvasSizeUpdateJobs:false, out var path)
					? path
					: throw new InvalidOperationException($"Cannot find the parent JobTreeItem for job: {job.Id}, that has ParentJob: {job.ParentJobId}.");
		}

		private bool TryFindJobParentPath(Job job, IJobTreeBranch currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			path = null;

			return job.ParentJobId != null
				&& TryFindJobPathById(job.ParentJobId.Value, currentBranch, includeCanvasSizeUpdateJobs: false, out path);
		}

		private bool TryFindJobPath(Job job, IJobTreeBranch currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			path = currentBranch.GetCurrentPath();

			return job.TransformType == TransformType.CanvasSizeUpdate
					? TryFindCanvasSizeUpdatePath(job, currentBranch, out path)
					: TryFindJobPathById(job.Id, currentBranch, includeCanvasSizeUpdateJobs: false, out path);
		}

		private bool TryFindJobPathById(ObjectId jobId, IJobTreeBranch currentBranch, bool includeCanvasSizeUpdateJobs, [MaybeNullWhen(false)] out JobTreePath path)
		{
			path = currentBranch.GetCurrentPath();
			return (jobId == path?.LastTerm?.JobId) || TryFindJobPathByIdInternal(jobId, currentBranch, includeCanvasSizeUpdateJobs, out path);
		}

		private bool TryFindJobPathByIdInternal(ObjectId jobId, IJobTreeBranch currentBranch, bool includeCanvasSizeUpdateJobs, [MaybeNullWhen(false)] out JobTreePath path)
		{
			//var tc = currentBranch.Children;
			if (NodeContainsJob(currentBranch, x => x.Job.Id == jobId, out path))
			{
				return true;
			}

			if (includeCanvasSizeUpdateJobs && NodeContainsAltDispJob(currentBranch, x => x.Job.Id == jobId, out path))
			{
				return true;
			}

			var jobTreeItem = currentBranch.GetItemOrRoot();


			foreach (var child in jobTreeItem.Children)
			{
				var cPath = currentBranch.Combine(child);

				//IJobTreeBranch cb = cPath;
				//var testBranchChildren = cb.Children;
				//var testPathChildren = cPath.Children;
				//var testBranchItem = cb.GetItemOrRoot();
				//var testPathItem = cPath.Item;

				if (TryFindJobPathByIdInternal(jobId, currentBranch.Combine(child), includeCanvasSizeUpdateJobs, out var localPath))
				{
					path = currentBranch.Combine(localPath);
					return true;
				}
			}

			path = null;
			return false;
		}


		private bool NodeContainsJob(IJobTreeBranch branch, Func<JobTreeItem, bool> predicate, [MaybeNullWhen(false)] out JobTreePath path)
		{
			var foundNode = branch.Children.FirstOrDefault(predicate);
			path = foundNode == null ? null : branch.Combine(foundNode);
			return path != null;
		}

		private bool NodeContainsAltDispJob(IJobTreeBranch branch, Func<JobTreeItem, bool> predicate, [MaybeNullWhen(false)] out JobTreePath path)
		{
			var foundNode = branch.AlternateDispSizes?.FirstOrDefault(predicate);
			path = foundNode == null ? null : branch.Combine(foundNode);
			return path != null;
		}

		private bool TryFindCanvasSizeUpdatePath(Job job, IJobTreeBranch currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			var parentJobId = job.ParentJobId;

			if (parentJobId == null)
			{
				throw new ArgumentException("When finding a CanvasSizeUpdate, the job must have a parent.", nameof(job));
			}

			if (TryFindJobPathById(parentJobId.Value, currentBranch, includeCanvasSizeUpdateJobs: false, out var parentPath))
			{
				var foundNode = parentPath.Item.AlternateDispSizes?.FirstOrDefault(x => x.Job.Id == job.Id);
				path = foundNode == null ? null : parentPath.Combine(foundNode);
				return path != null;
			}
			else
			{
				path = null;
				return false;
			}
		}

		#endregion

		#region Private Navigate Methods

		private JobTreePath? GetNextJobPath(JobTreePath path, bool skipPanJobs)
		{
			var currentItem = path.Item;

			JobTreePath? result;

			var parentJobTreeItem = path.GetParentItemOrRoot();
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (currentItem.ParentJobId.HasValue && currentItem.IsActiveAlternate && currentItem.Children.Any())
			{
				// The new job will be a child of the current job
				result = path.Combine(currentItem.Children[0]);
			}
			else
			{
				if (TryGetNextJobTreeItem(siblings, currentPosition, skipPanJobs, out var nextJobTreeItem))
				{
					//The new job will be a sibling of the current job
					result = path.Combine(nextJobTreeItem);
				}
				else
				{
					result = null;
				}
			}

			return result;
		}

		private bool TryGetNextJobTreeItem(IList<JobTreeItem> jobTreeItems, int currentPosition, bool skipPanJobs, [MaybeNullWhen(false)] out JobTreeItem nextJobTreeItem)
		{
			if (skipPanJobs)
			{
				nextJobTreeItem = jobTreeItems.Skip(currentPosition + 1).FirstOrDefault(x => x.Job.TransformType is TransformType.Home or TransformType.ZoomIn or TransformType.ZoomOut);
			}
			else
			{
				nextJobTreeItem = jobTreeItems.Skip(currentPosition + 1).FirstOrDefault();
			}

			return nextJobTreeItem != null;
		}

		private bool CanMoveForward(JobTreePath? path)
		{
			var currentItem = path?.LastTerm;

			if (path == null || currentItem == null)
			{
				return false;
			}

			var parentJobTreeItem = path.GetParentItemOrRoot();
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			return !(currentPosition == siblings.Count - 1);
		}

		private JobTreePath? GetPreviousJobPath(JobTreePath path, bool skipPanJobs)
		{
			var currentItem = path.LastTerm;

			if (currentItem == null)
			{
				return null;
			}

			var parentJobTreeItem = path.GetParentItemOrRoot();
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);
			var previousJobTreeItem = GetPreviousJobTreeItem(siblings, currentPosition, skipPanJobs);

			while (previousJobTreeItem == null && path.Count > 1)
			{
				path = path.GetParentPath()!;
				currentItem = path.Item;

				var grandparentNode = path.GetParentItemOrRoot();
				var ancestors = grandparentNode.Children;
				currentPosition = ancestors.IndexOf(currentItem);
				previousJobTreeItem = GetPreviousJobTreeItem(ancestors, currentPosition + 1, skipPanJobs);
			}

			if (previousJobTreeItem != null)
			{
				var result = path.Combine(previousJobTreeItem);

				return result;
			}
			else
			{
				return null;
			}
		}

		private JobTreeItem? GetPreviousJobTreeItem(IList<JobTreeItem> jobTreeItems, int currentPosition, bool skipPanJobs)
		{
			JobTreeItem? result;

			if (skipPanJobs)
			{
				result = jobTreeItems.SkipLast(jobTreeItems.Count - currentPosition).LastOrDefault(x => x.Job.TransformType is TransformType.Home or TransformType.ZoomIn or TransformType.ZoomOut);
			}
			else
			{
				result = jobTreeItems.SkipLast(jobTreeItems.Count - currentPosition).LastOrDefault();
			}

			return result;
		}

		private bool CanMoveBack(JobTreePath? path)
		{
			var currentItem = path?.LastTerm;

			if (path == null || currentItem == null)
			{
				return false;
			}

			var parentJobTreeItem = path.GetParentItemOrRoot();
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (currentPosition > 0)
			{
				// We can move to the previous item at the current level.
				return true;
			}
			else
			{
				// If we can go up, return true.
				return path.Count > 1;
			}
		}

		private bool MoveCurrentTo(Job job, IJobTreeBranch currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (TryFindJobPath(job, currentBranch, out path))
			{
				ExpandAndSetCurrent(path);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void ExpandAndSetCurrent(JobTreePath path)
		{
			if (path.IsEmpty)
			{
				return;
			}

			foreach (var p in path.Terms.SkipLast(1))
			{
				p.IsExpanded = true;
			}

			var lastTerm = (path.TransformType == TransformType.CanvasSizeUpdate ? path.GetParentPath()! : path).Item;
			lastTerm.IsCurrent = true;
		}

		private void UpdateIsSelected(JobTreeItem? jobTreeItem, bool isSelected, bool useRealRelationships, IJobTreeBranch startPos)
		{
			if (useRealRelationships)
			{
				UpdateIsSelectedReal(jobTreeItem, isSelected, startPos);
			}
			else
			{
				UpdateIsSelectedLogical(jobTreeItem, isSelected, startPos);
			}
		}

		private void UpdateIsSelectedLogical(JobTreeItem? jobTreeItem, bool isSelected, IJobTreeBranch startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.IsSelected = isSelected;

				if (!TryFindJobPath(jobTreeItem.Job, startPos, out var path))
				{
					return;
				}

				//var ancestors = jobTreeItem.GetAncestors();
				//var path2 = _xroot.Combine(ancestors);
				//var strPath1 = string.Join("; ", path.Terms.Select(x => x.JobId.ToString()));
				//var strPath2 = string.Join("; ", path2.Terms.Select(x => x.JobId.ToString()));
				//Debug.WriteLine($"Path: {strPath1}\nPath2: {strPath2}.");

				var strPath1 = string.Join("\n\t", path.Terms.Select(x => $"Id:{x.JobId}, ParentId:{x.ParentJobId}, Alt:{x.IsActiveAlternate}, Prk:{x.IsParkedAlternate}"));
				Debug.WriteLine($"Path: {strPath1}.");

				var backPath = GetPreviousJobPath(path, skipPanJobs: true);

				if (backPath == null)
				{
					return;
				}

				// The backPath points to the first job previous to the give job that has a TransformType of Zoom-In or Zoom-Out or Home.
				var parentNode = backPath.Item;

				// Set the parent node's IsParentOfSelected
				parentNode.IsParentOfSelected = isSelected;

				//// Set each sibling node's IsSiblingSelected
				//foreach (var siblingItem in parentNode.Children)
				//{
				//	siblingItem.IsSiblingOfSelected = isSelected;
				//}

				if (jobTreeItem.RealChildJobs.Any())
				{
					// Use the prior job's parent path to start the search for each child.
					var parentBranch = backPath.GetParentBranch();

					// Set each child node's IsChildOfSelected
					foreach (var realChildJob in jobTreeItem.RealChildJobs.Values)
					{
						if (TryFindJobPath(realChildJob, parentBranch, out var childPath))
						{
							childPath.Item.IsChildOfSelected = isSelected;
						}
					}
				}
				else
				{
					var forwardPath = GetNextJobPath(path, skipPanJobs: false);

					if (forwardPath != null)
					{
						forwardPath.Item.IsChildOfSelected = true;
					}
				}

			}
		}

		private void UpdateIsSelectedReal(JobTreeItem? jobTreeItem, bool isSelected, IJobTreeBranch startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.IsSelected = isSelected;

				if (TryFindJobParentPath(jobTreeItem.Job, startPos, out var realParentPath))
				{
					var realParentNode = realParentPath.Item;

					// Set the parent node's IsParentOfSelected
					realParentNode.IsParentOfSelected = isSelected;

					// Use the logical grandparent path (or root) to start the search for each sibling
					var grandparentBranch = realParentPath.GetParentBranch();

					// Set each sibling node's IsSiblingSelected
					foreach (var realSiblingJob in realParentNode.RealChildJobs.Values)
					{
						if (TryFindJobPath(realSiblingJob, grandparentBranch, out var siblingPath))
						{
							siblingPath.Item.IsSiblingOfSelected = isSelected;
						}
					}

					// Use the real parent path to start the search for each child.
					var logicalParentBranch = realParentPath;

					// Set each child node's IsChildOfSelected
					foreach (var realChildJob in jobTreeItem.RealChildJobs.Values)
					{
						if (TryFindJobPath(realChildJob, logicalParentBranch, out var childPath))
						{
							childPath.Item.IsChildOfSelected = isSelected;
						}
					}

				}
			}
		}

		#endregion

		#region Lock Helpers

		private T DoWithReadLock<T>(Func<T> function)
		{
			_jobsLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				_jobsLock.ExitReadLock();
			}
		}

		private void DoWithWriteLock(Action action)
		{
			_jobsLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_jobsLock.ExitWriteLock();
			}
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			((IDisposable)_jobsLock).Dispose();
		}

		#endregion
	}
}

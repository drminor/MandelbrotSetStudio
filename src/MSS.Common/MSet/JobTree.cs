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
	/// <remarks>
	///	New jobs are inserted into order by the date the job was created.
	///	The new job's Parent identifies the job from which this job was created.
	///
	///	New jobs are added as a sibling to it "parent"
	///	a) if the new Job is being added as the last job
	///	b) the job is not a Zoom-In or Zoom-Out and the job that is currently the last is not a Zoom-In or Zoom-Out				
	///  
	///	Otherwise new jobs are added as a "Parked Alteranate" to the Job just before the point of insertion.
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
		private readonly JobTreePath _root;

		private JobTreePath? _currentPath;
		private JobTreeItem? _selectedItem;

		#region Constructor

		public JobTree(List<Job> jobs, bool checkHomeJob)
		{
			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

			jobs = jobs.OrderBy(x => x.Id.ToString()).ToList();
			ReportInput(jobs);

			Debug.WriteLine($"Loading {jobs.Count()} jobs.");

			if (checkHomeJob)
			{
				_root = BuildTreeCheckHomeJob(jobs, out _currentPath);
			}
			else
			{
				 _root = BuildTreeFirstJobIsHome(jobs, out _currentPath);
			}

			Debug.Assert(!IsDirty, "IsDirty should be false as the constructor is exited.");

			ReportOutput(_root);
		}

		private JobTreePath BuildTreeCheckHomeJob(List<Job> jobs, out JobTreePath? currentPath)
		{
			// Make it an error for their not be one and only one Job of type Home.
			var homeJob = GetHomeJob(jobs);
			var root = JobTreeItem.CreateRoot(homeJob);
			currentPath = BuildTree(jobs, root);

			return root;
		}

		private JobTreePath BuildTreeFirstJobIsHome(List<Job> jobs, out JobTreePath? currentPath)
		{
			// Take the first job and call it home, regardless of its type.
			// TODO: What happens if there are other jobs with a ParentJobId == null?
			var root = JobTreeItem.CreateRoot();
			currentPath = BuildTree(jobs, root);

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

		private void ReportOutput(JobTreePath start)
		{
			Debug.WriteLine("OUTPUT Report");
			Debug.WriteLine("Id\t\t\t\tParentId\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var jwps = GetJobsWithParentage(start);

			foreach (var jwp in jwps)
			{
				var j = jwp.Item1;
				var p = jwp.Item2;
				Debug.WriteLine($"{j.Id}\t{p?.Id.ToString() ?? "null"}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		#endregion

		#region Public Properties

		public ObservableCollection<JobTreeItem> JobItems => _root.Tree.Children;

		public Job CurrentJob
		{
			get => DoWithReadLock(() =>
			{
				var currentJob = _currentPath?.Job;
				if (currentJob == null)
				{
					currentJob = JobItems[0].Job;
				}
				return currentJob;
			});
			
			set => DoWithWriteLock(() =>
			{
				if (value != _currentPath?.Job)
				{
					//UpdateIsSelectedForRelatives(_currentPath?.LastTerm, isSelected: false);
					var moved = MoveCurrentTo(value, _root, out _currentPath);
					//UpdateIsSelectedForRelatives(_currentPath?.LastTerm, isSelected: true);

					if (!moved && value != null)
					{
						Debug.WriteLine($"WARNING: Could not MoveCurrent to job: {value.Id}.");
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
					UpdateIsSelectedForRelatives(_selectedItem, _root, false);
					_selectedItem = value;
					UpdateIsSelectedForRelatives(_selectedItem, _root, true);
				}
			}
		}

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
			if (!TryFindJobTreeItemById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
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
			if (path.IsEmpty)
			{
				throw new ArgumentException("Path cannot be empty.");
			}

			JobTreePath newPath;

			var node = path.GetItemUnsafe();

			if (node.TransformType == TransformType.CanvasSizeUpdate)
			{
				var parentPath = path.GetParentPathUnsafe();
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
			if (!TryFindJobTreeItemById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
			{
				return false;
			}

			var result = RemoveBranch(path);
			return result;
		}
		
		public bool RemoveBranch(JobTreePath path)
		{
			if (path.IsEmpty)
			{
				throw new ArgumentException("Path cannot be empty");
			}

			var jobTreeItem = path.GetItemUnsafe();

			bool result;

			if (jobTreeItem.TransformType == TransformType.CanvasSizeUpdate)
			{
				var csuParentPath = path.GetParentPathUnsafe();
				result = csuParentPath.GetItemUnsafe().Remove(jobTreeItem);
				IsDirty = true;

				return result;
			}

			if (jobTreeItem.IsActiveAlternate)
			{
				// Restore the most recent alternate branches before removing.
				Debug.WriteLine($"Making the branch being removed a parked alternate.");

				var alternateToMakeActive = SelectMostRecentAlternate(jobTreeItem.Children);
				var selectedAltPath = path.Combine(alternateToMakeActive);

				var newPath = MakeBranchActive(selectedAltPath);

				// Update the path to reflect that the item being removed is now a child of the newly activated alternate.
				path = newPath.Combine(jobTreeItem);
			}

			var parentPath = path.GetParentPathUnsafe();
			var parent = parentPath.GetItemUnsafe();
			var idx = parent.Children.IndexOf(jobTreeItem);

			if (jobTreeItem.TransformType == TransformType.ZoomIn)
			{
				// TODO: Determine if this JobTreeItem with TransformType = ZoomIn is the last node of the current branch as we execute RemoveBranch
			}

			if (parent.IsActiveAlternate || idx == 0)
			{
				if (parentPath == null)
				{
					throw new InvalidOperationException("Removing the Home node is not yet supported.");
				}

				_currentPath = parentPath;
			}
			else
			{
				_currentPath = parentPath.Combine(parent.Children[idx - 1]);
			}

			// TODO: Examine how we "tear" down a set of JobTreeItems.
			jobTreeItem.RealChildJobs.Clear();

			result = parent.Remove(jobTreeItem);

			if (parent.IsActiveAlternate && !parent.Children.Any())
			{
				parent.IsActiveAlternate = false;
			}

			ExpandAndSetCurrent(_currentPath);

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
			_jobsLock.EnterUpgradeableReadLock();

			try
			{
				if (TryFindJobTreeParent(job, _root, out var parentPath))
				{
					JobTreeItem parentNode;

					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						// The parentPath points to the "original job" for which the CanvasSizeUpdate job was created.
						// We need to get its parentPath to continue.
						parentNode = parentPath.GetParentPathUnsafe().GetItemUnsafe();
					}
					else
					{
						parentNode = parentPath.GetItemUnsafe();
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
				return GetJobPath(jobId, _root, includeCanvasSizeUpdateJobs: false);
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
			_ = TryFindJob(jobId, _root, out var result);
			return result;
		}

		//TODO: Use ParentNode instead.
		public Job? GetParent(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindJob(job.ParentJobId.Value, _root, out var result);
				return result;
			}
		}

		public List<Job>? GetJobAndDescendants(ObjectId jobId)
		{
			return DoWithReadLock(() =>
			{
				List<Job>? result;

				if (TryFindJobTreeItemById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
				{
					result = new List<Job> { path.GetItemUnsafe().Job };
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

		private JobTreePath AddInternal(Job job, JobTreePath currentBranch)
		{
			JobTreePath newPath;
			
			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				newPath = AddCanvasSizeUpdateJob(job, currentBranch);
			}
			else
			{
				var parentPath = GetSourceJob(job, currentBranch);
				newPath = AddAtParentPath(job, parentPath);
			}

			return newPath;
		}

		//private JobTreePath AddHomeJob(Job job, JobTreePath currentBranch)
		//{
		//	if (job.ParentJobId != null)
		//	{
		//		throw new InvalidOperationException($"Attempting to add a Home job, but the job's parentJobId is {job.ParentJobId}.");
		//	}

		//	if (job.TransformType != TransformType.Home)
		//	{
		//		throw new InvalidOperationException($"Attempting to add a Home job, but the job's TransformType is {job.TransformType}.");
		//	}

		//	var node = currentBranch.LastTerm;
		//	if (!node.IsRoot)
		//	{
		//		throw new InvalidOperationException("Attempting to add a Home job to a branch whose IsRoot property is false.");
		//	}

		//	if (node.Children.Any())
		//	{
		//		throw new InvalidOperationException("Attempting to add a Home job to a parent node that already hase one or more children.");
		//	}

		//	var homeNode = node.AddJob(job);
		//	var newPath = currentBranch.Combine(homeNode);

		//	return newPath;
		//}

		private JobTreePath AddCanvasSizeUpdateJob(Job job, JobTreePath currentBranch)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's TransformType is {job.TransformType}.");
			}

			if (job.ParentJobId == null)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's parentJobId is null.");
			}

			var parentPath = GetSourceJob(job, currentBranch);
			var parentNode = parentPath.GetItemUnsafe();

			var canvasSizeUpdateNode = parentNode.AddCanvasSizeUpdateJob(job);
			var newPath = parentPath.Combine(canvasSizeUpdateNode);

			return newPath;
		}

		private JobTreePath AddAtParentPath(Job job, JobTreePath parentPath)
		{
			JobTreePath newPath;

			// This is the JobTreeItem for the new Job's real parent.
			var parentNode = parentPath.GetItemUnsafe();

			// Add the new Job to the list of it's parent's "real" children.
			// The index is the position of the new job among its siblings which are sorted by the CreatedDate, ascending.
			var index = parentNode.AddRealChild(job);

			// Get the job already in the tree for which the job being added will directly follow.
			//Job preceedingJob;

			JobTreePath preceedingPath;

			if (index == 0)
			{
				Debug.Assert(parentNode.RealChildJobs.Count == 1, "Our index is Zero, but we're not the first.");

				// Find the sibling of the parent, that comes just before the parent.
				//var grandparentNode = GetParentComponent(parentPath);
				//var parentNodePos = grandparentNode.Children.IndexOf(parentNode);

				//var grandparentId = parentNode.ParentJobId;
				var grandparentPath = parentPath.GetParentPathUnsafe();

				if (!grandparentPath.IsEmpty)
				{
					var grandparentId = grandparentPath.Job.Id;

					if (TryFindJobTreeItemById(grandparentId, grandparentPath, includeCanvasSizeUpdateJobs: false, out var realGrandparentPath))
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

				//if (parentNodePos == -1)
				//{
				//	throw new InvalidOperationException($"Cannot find the ParentNode in its Parent's list of child nodes.");
				//}

				//if (parentNodePos == 0)
				//{
				//	if (grandparentNode.IsRoot)
				//	{
				//		preceedingJob = parentNode.Job;
				//	}
				//	else
				//	{
				//		preceedingJob = grandparentNode.Job;
				//	}
				//}
				//else
				//{
				//	var nodePreceedingParent = grandparentNode.Children[parentNodePos - 1];

				//	// Find the last job of that sibling.
				//	preceedingJob = nodePreceedingParent.Children.Count > 0 ? nodePreceedingParent.Children[^1].Job : nodePreceedingParent.Job;
				//	//preceedingJob = nodePreceedingParent.Children[^1].Job;
				//}
			}
			else
			{
				var preceedingJob1 = parentNode.RealChildJobs.Values[index - 1];
				preceedingPath = GetJobPath(preceedingJob1, parentPath.GetRootPath());
			}

			var preceedingJob = preceedingPath.GetItemUnsafe();

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
				var parkedParentPath = preceedingPath.GetParentPathUnsafe();
				activeAltPath = parkedParentPath;
			}
			else
			{
				// The preceeding node has not yet been made an Alternate.
				Debug.WriteLine($"Found a Job that is a new Alternate. Marking existing node: {preceedingPath.LastTerm} as the Active ALT.");
				preceedingPath.GetItemUnsafe().IsActiveAlternate = true;
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

			//if (preceedingPath.IsParkedAlternate)
			//{
			//	// Add the new job as a child of the Parked Alternate
			//	var activeAltPath = MakeBranchActive(preceedingPath);
			//	newPath = AddJob(job, activeAltPath);
			//}
			//else
			//{
			//	// Add the new job as a sibling to it's parent
			//	Debug.Assert(preceedingPath.LastTerm.ParentNode == parentPath.LastTerm.ParentNode, "The Preceeding Node's Logical Parent and the Parent Node's Logical Parent are not the same.");
			//	newPath = AddInLine(job, parentPath);
			//}

			if (preceedingPath.IsParkedAlternate)
			{
				//// Switch the preceeding job in so that it is on the 'main' branch.
				//preceedingPath = MakeBranchActive(preceedingPath);
				if (job.ParentJobId == preceedingPath.Job.Id) // preceedingPath.LastTerm == parentPath.LastTerm)
				{
					// Add the job as a child of the Parked Alt
					newPath = AddJob(job, preceedingPath);
				}
				else
				{
					newPath = AddInLine(job, preceedingPath.GetParentPathUnsafe());
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

			Debug.Assert(!parentPath.IsParkedAlternate, "AddJobInLine is adding a job to an Active Alt node. (The value of the specified path to which we are adding in-line is a Parked Alt node.");

			// The grandparentPath points to the locgial parent. This identifies the branch on which the parent is currently attached.
			// If the parent is a PRK then this grandparent is the ALT on the active branch
			var grandparentPath = parentPath.GetParentPathUnsafe();

			// Add the new job as a sibling to the branch on which my real parent's JobTreeItem was found (in sort order by date created.)
			Debug.Assert(grandparentPath.IsEmpty || !grandparentPath.IsActiveAlternate, "AddJobInLine is adding a job to an Active Alt node.");
			var result = AddJob(job, grandparentPath);

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

			var parkedAltNode = path.GetItemUnsafe();

			if (parkedAltNode.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException("MakeActiveBranch does not support CanvasSizeUpdates.");
			}

			var activeAltNode = parkedAltNode.ParentNode;

			if (activeAltNode is null)
			{
				throw new InvalidOperationException("Call to MakeBranchActive found no Active Alt parent of the Parked Alt being made active.");
			}

			// Remove the Parked Alt node being restored from the collection of Parked Alt nodes held by the Active Alt Node.
			//_ = activeAltNode.Remove(parkedAltNode);

			// The parked ALT's children contains all of the job following the parked ALT.

			var ourChildern = new List<JobTreeItem>(parkedAltNode.Children);
			//parkedAltNode.Children.Clear();

			SwitchAltBranches(parkedAltNode, activeAltNode);

			// Add the new job to the end of the main trunk.
			var grandparentPath = path.GetGrandparentPathUnSafe();
			var grandparentItem = grandparentPath.GetItemUnsafe();

			// Add the once Parked, now Active Alt node to the grandparent
			//var newPath = AddJobTreeItem(parkedAltNode, grandparentPath);
			_ = parkedAltNode.Move(grandparentItem);
			var newPath = grandparentPath.Combine(parkedAltNode);

			for (var i = 0; i < ourChildern.Count; i++)
			{
				var child = ourChildern[i];
				//_ = AddJobTreeItem(child, grandparentPath);

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

			// Move all children of the Active Alt node to be children of the node becoming the Active Alt node. Dont' move the node becoming the Active Alt node.
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

		private JobTreeItem SelectMostRecentAlternate(IList<JobTreeItem> siblings)
		{
			var result = siblings.Aggregate((i1, i2) => (i1.CompareTo(i2) > 0) ? i1 : i2);
			return result;
		}

		#endregion

		#region Private Load and Export Job Methods

		private JobTreePath? BuildTree(IList<Job> jobs, JobTreePath root)
		{
			var visited = 1;
			LoadChildItems(jobs, root, ref visited);

			if (visited != jobs.Count)
			{
				Debug.WriteLine($"WARNING: Only {visited} jobs out of {jobs.Count} were included during build.");
			}

			_ = MoveCurrentTo(jobs[0], root, out var path);

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

			//if (numberOfJobsWithNullParentId > 1)
			//{
			//	Debug.WriteLine($"Loading {jobs.Count()} jobs.");
			//	_root = BuildTree(jobs, out _currentPath);
			//}
			//else
			//{
			//	Debug.WriteLine($"Loading {jobs.Count()} jobs from an older project.");
			//	_root = BuildTreeForOldPro(jobs, out _currentPath);
			//}

			return homeJob;
		}

		private void LoadChildItems(IList<Job> jobs, JobTreePath currentBranch, ref int visited)
		{
			var childJobs = GetChildren(currentBranch.Job.Id, jobs);
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
					var path = AddInternal(job, currentBranch);
					ValidateAddInternal(job, currentBranch);

					LoadChildItems(jobs, path, ref visited);
				}
			}
		}

		[Conditional("DEBUG")]
		private void ValidateAddInternal(Job job, JobTreePath currentBranch)
		{
			if (!TryFindJobTreeItem(job, currentBranch.GetRootPath(), out _))
			{
				throw new InvalidOperationException("Cannot find job just loaded.");
			}
		}

		private IList<Job> GetChildren(ObjectId parentJobId, IList<Job> jobs)
		{
			ObjectId? parentIdToFind;

			if (parentJobId == ObjectId.Empty)
			{
				Debug.WriteLine($"As the JobTree is being built, the parentJobId specified in the call to GetChildren is ObjectId.Empty, using Null instead.");
				parentIdToFind = null;
			}
			else
			{
				parentIdToFind = parentJobId;
			}
			
			var result = jobs.Where(x => x.ParentJobId == parentIdToFind).OrderBy(x => x.Id.Timestamp).ToList();

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

		private IList<Job> GetJobs(JobTreePath currentBranch)
		{
			// TODO: Consider implementing an IEnumerator<JobTreeItem> for the JobTree class.
			var result = new List<Job>();

			var parentNode = currentBranch.GetParentItemOrRoot();

			foreach (var child in parentNode.Children)
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

		private List<Tuple<Job, Job?>> GetJobsWithParentage(JobTreePath currentBranch)
		{
			var result = new List<Tuple<Job, Job?>>();

			var parentJob = currentBranch.LastTerm?.Job;
			var parentNode = currentBranch.GetParentItemOrRoot();

			foreach (var child in parentNode.Children)
			{
				result.Add(new Tuple<Job, Job?>(child.Job, parentJob));
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

		private JobTreePath? GetJobPath(ObjectId jobId, JobTreePath currentBranch, bool includeCanvasSizeUpdateJobs = false)
		{
			return TryFindJobTreeItemById(jobId, currentBranch, includeCanvasSizeUpdateJobs, out var path) ? path : null;
		}

		private JobTreePath GetJobPath(Job job, JobTreePath currentBranch)
		{
			var includeCanvasSizeUpdateJobs = job.TransformType == TransformType.CanvasSizeUpdate;
			if (TryFindJobTreeItemById(job.Id, currentBranch, includeCanvasSizeUpdateJobs, out var path))
			{
				return path;
			}
			else
			{
				throw new InvalidOperationException($"Cannot find Job: {job.Id} in the JobTree.");
			}
		}

		private bool TryFindJobTreeItemById(ObjectId jobId, JobTreePath currentBranch, bool includeCanvasSizeUpdateJobs, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (includeCanvasSizeUpdateJobs)
			{
				throw new NotImplementedException("TryFindJobTreeItemById does not yet locate jobs with TransformType = CanvasSizeUpdate.");
			}
			var jobTreeItem = currentBranch.LastTerm ?? currentBranch.Tree;
			var foundNode = jobTreeItem.Children.FirstOrDefault(x => x.JobId == jobId);

			if (foundNode != null)
			{
				path = currentBranch.Combine(foundNode);
				return true;
			}
			else
			{
				foreach (var child in jobTreeItem.Children)
				{

					if (TryFindJobTreeItemById(jobId, currentBranch.Combine(child), includeCanvasSizeUpdateJobs, out var localPath))
					{
						path = currentBranch.Combine(localPath);
						return true;
					}
				}

				path = null;
				return false;
			}
		}

		//private bool TryFindJobTreeItemFromPath(Job job, JobTreePath? parentPath, [MaybeNullWhen(false)] out JobTreePath path)
		//{
		//	var parentNode = parentPath == null ? _root : parentPath.LastTerm;


		//	var result = TryFindJobTreeItem(job, parentNode, out path);

		//	return result;
		//}

		private bool TryFindJobTreeItem(Job job, JobTreePath currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"TryFindJobTreeItem does not support finding jobs with TransformType = CanvasSizeUpdate. Use {nameof(TryFindCanvasSizeUpdateTreeItem)} instead.");
			}

			var parentNode = currentBranch.GetParentItemOrRoot();

			var foundNode = parentNode.Children.FirstOrDefault(x => x.Job.Id == job.Id);

			if (foundNode != null)
			{
				path = currentBranch.Combine(foundNode);
				return true;
			}
			else
			{
				foreach (var child in parentNode.Children)
				{
					if (TryFindJobTreeItem(job, currentBranch.Combine(child), out var localPath))
					{
						path = currentBranch.Combine(localPath);
						return true;
					}
				}

				path = null;
				return false;
			}
		}

		private JobTreePath GetSourceJob(Job job, JobTreePath currentBranch)
		{
			JobTreePath result;

			var parentJobId = job.ParentJobId;

			if (parentJobId == null)
			{
				if (currentBranch.IsRoot)
				{
					return currentBranch;
				}
				else
				{
					throw new ArgumentException("Cannot get the SourceJob if the Job's ParentJobId is null.");
				}
			}

			if (parentJobId == currentBranch.Job.Id)
			{
				result = currentBranch;
			}
			else
			{
				if (TryFindJobTreeItemById(parentJobId.Value, currentBranch, includeCanvasSizeUpdateJobs: false, out var parentPath))
				{
					result = parentPath;
				}
				else
				{
					throw new InvalidOperationException($"Cannot find the parent JobTreeItem for job: {job.Id}, that has ParentJob: {parentJobId.Value}.");
				}
			}
			return result;
		}

		private bool TryFindCanvasSizeUpdateTreeItem(Job job, JobTreePath currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Attempting to Find a CanvasSizeUpdate JobTreeItem using a Job that has a TransformType other than " +
					$"{nameof(TransformType.CanvasSizeUpdate)} is not supported. The TransformType of this job is {job.TransformType}.");
			}

			var parentJobId = job.ParentJobId;

			if (parentJobId == null)
			{
				throw new InvalidOperationException("When finding a CanvasSizeUpdate, the job must have a parent.");
			}

			if (TryFindJobTreeItemById(parentJobId.Value, currentBranch, includeCanvasSizeUpdateJobs: false, out var parentPath))
			{
				var parentJobTreeItem = parentPath.GetItemUnsafe();
				var canvasSizeUpdateTreeItem = parentJobTreeItem.AlternateDispSizes?.FirstOrDefault(x => x.Job.Id == job.Id);

				if (canvasSizeUpdateTreeItem != null)
				{
					path = parentPath.Combine(canvasSizeUpdateTreeItem); 
					return true;
				}
				else
				{
					path = null;
					return false;
				}
			}
			else
			{
				path = null;
				return false;
			}
		}

		private bool TryFindJobTreeParent(Job job, JobTreePath currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job.ParentJobId == null)
			{
				path = null;
				return false;
			}
			else
			{
				return TryFindJobTreeItemById(job.ParentJobId.Value, currentBranch, includeCanvasSizeUpdateJobs: false, out path);
			}
		}

		#endregion

		#region Private Collection Methods, No Support for CanvasSizeUpdates

		private JobTreePath AddJob(Job job, JobTreePath parentPath)
		{
			var parentNode = parentPath.IsEmpty ? parentPath.Tree : parentPath.GetItemUnsafe();
			var newNode = parentNode.AddJob(job);
			if (parentNode.IsActiveAlternate)
			{
				newNode.IsParkedAlternate = true;
			}

			var result = parentPath.Combine(newNode);

			return result;
		}

		private JobTreePath AddJobTreeItem(JobTreeItem newNode, JobTreePath parentPath)
		{
			var parentNode = parentPath.GetItemOrRoot();
			parentNode.AddNode(newNode);
			var result = parentPath.Combine(newNode);

			return result;
		}

		private bool TryFindJob(ObjectId jobId, JobTreePath currentBranch, [MaybeNullWhen(false)] out Job job)
		{
			var parentNode = currentBranch.GetParentItemOrRoot();

			var foundNode = parentNode.Children.FirstOrDefault(x => x.JobId == jobId);

			if (foundNode != null)
			{
				job = foundNode.Job;
				return true;
			}
			else
			{
				foreach (var child in parentNode.Children)
				{
					if (TryFindJob(jobId, currentBranch.Combine(child), out job))
					{
						return true;
					}
				}

				job = null;
				return false;
			}
		}

		private void UpdateIsSelectedForRelatives(JobTreeItem? jobTreeItem, JobTreePath currentBranch, bool isSelected)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.IsSelected = isSelected;

				if (TryFindJobTreeParent(jobTreeItem.Job, currentBranch, out var realParentPath))
				{
					var realParentNode = realParentPath.GetItemUnsafe();

					// Set the parent node's IsParentOfSelected
					realParentNode.IsParentOfSelected = isSelected;

					// Use the grandparent node (or root) to start the search for each sibling
					var grandparentPath = realParentPath.GetParentPath() ?? currentBranch;

					// Set each sibling node's IsSiblingSelected
					foreach (var realSiblingJob in realParentNode.RealChildJobs.Values)
					{
						if (TryFindJobTreeItem(realSiblingJob, grandparentPath, out var siblingPath))
						{
							siblingPath.GetItemUnsafe().IsSiblingOfSelected = isSelected;
						}
					}
				}

				// Use the Logical ParentNode to start the search for each child.
				var logicalParentPath = jobTreeItem.ParentNode == null ? currentBranch : new JobTreePath(jobTreeItem.ParentNode);
				// Set each child node's IsChildOfSelected
				foreach (var realChildJob in jobTreeItem.RealChildJobs.Values)
				{
					if (TryFindJobTreeItem(realChildJob, logicalParentPath, out var childPath))
					{
						childPath.GetItemUnsafe().IsChildOfSelected = isSelected;
					}
				}
			}
		}

		#endregion

		#region Private Navigate Methods

		private JobTreePath? GetNextJobPath(JobTreePath path, bool skipPanJobs)
		{
			var currentItem = path.LastTerm;

			if (currentItem == null)
			{
				return null;
			}

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
					// The new job will be a sibling of the current job
					//result = CreateSiblingPath(path, nextJobTreeItem);
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
				nextJobTreeItem = jobTreeItems.Skip(currentPosition + 1).FirstOrDefault(x => x.Job.TransformType != TransformType.Pan);
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

			var newBasePath = path.Clone();

			var parentJobTreeItem = path.GetParentItemOrRoot();
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);
			var previousJobTreeItem = GetPreviousJobTreeItem(siblings, currentPosition, skipPanJobs);

			while (previousJobTreeItem == null && newBasePath.Count > 1)
			{
				newBasePath = newBasePath.GetParentPathUnsafe();
				currentItem = newBasePath.GetItemUnsafe();

				var grandparentNode = newBasePath.GetParentItemOrRoot();
				var ancestors = grandparentNode.Children;
				currentPosition = ancestors.IndexOf(currentItem);
				previousJobTreeItem = GetPreviousJobTreeItem(ancestors, currentPosition + 1, skipPanJobs);
			}

			if (previousJobTreeItem != null)
			{
				//var result = CreateSiblingPath(newBasePath, previousJobTreeItem);
				var result = newBasePath.Combine(previousJobTreeItem);

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
				result = jobTreeItems.SkipLast(jobTreeItems.Count - currentPosition).LastOrDefault(x => x.Job.TransformType != TransformType.Pan);
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

		private bool MoveCurrentTo(Job? job, JobTreePath currentBranch, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job == null)
			{
				path = null;
				return false;
			}

			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				var result = TryFindCanvasSizeUpdateTreeItem(job, currentBranch, out path);
				return result;
			}
			else
			{
				if (TryFindJobTreeItem(job, currentBranch, out path))
				{
					ExpandAndSetCurrent(path);
					return true;
				}
				else
				{
					path = null;
					return false;
				}
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

			var lastTerm = path.TransformType == TransformType.CanvasSizeUpdate ? path.GetParentItemUnsafe() : path.GetItemUnsafe();
			lastTerm.IsCurrent = true;
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

using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using JobBranchType = MSS.Types.ITreeBranch<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;
using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;
using JobNodeType = MSS.Types.ITreeNode<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;

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

	public class JobTree : Tree<JobTreeNode, Job>, IJobTree
	{
		private JobNodeType? _selectedItem;

		#region Constructor

		public JobTree(List<Job> jobs, bool checkHomeJob) : base(new JobTreeBranch())
		{
			var homeJob = GetHomeJob(jobs, checkHomeJob);
			CurrentPath = AddJob(homeJob, Root);

			if (CurrentPath == null)
			{
				throw new InvalidOperationException("The new JobTreeBranch has a null _currentPath.");
			}

			if (!CurrentPath.IsHome)
			{
				//throw new InvalidOperationException("The new JobTreeBranch's CurrentPath is not the HomeNode.");
				Debug.WriteLine("WARNING: The new JobTreeBranch's CurrentPath is not the HomeNode.");
			}

			jobs = jobs.OrderBy(x => x.Id.ToString()).ToList();
			ReportInput(jobs);

			Debug.WriteLine($"Loading {jobs.Count} jobs.");

			// Have BuildTree start with the homeJob, and not the root, so that it will not add the Home Job a second time.
			CurrentPath = PopulateTree(jobs, CurrentPath);

			ReportOutput(Root, CurrentPath);

			//Debug.Assert(_root.RootItem.Job.Id == ObjectId.Empty, "Creating a Root JobTreeItem that has a JobId != ObjectId.Empty.");
			Debug.Assert(!IsDirty, "IsDirty should be false as the constructor is exited.");
		}

		#endregion

		#region Public Properties

		public Job CurrentJob
		{
			get => CurrentItem;
			set => CurrentItem = value;
		}

		public bool AnyJobIsDirty => AnyItemIsDirty;

		public JobNodeType? SelectedNode
		{
			get => _selectedItem;
			set
			{
				if (value != _selectedItem)
				{
					UpdateIsSelected(_selectedItem, false, UseRealRelationShipsToUpdateSelected, Root);
					_selectedItem = value;
					UpdateIsSelected(_selectedItem, true, UseRealRelationShipsToUpdateSelected, Root);
				}
			}
		}

		public bool UseRealRelationShipsToUpdateSelected { get; set; }

		#endregion

		#region Public Methods

		public bool RestoreBranch(ObjectId jobId)
		{
			Debug.WriteLine($"Restoring Branch: {jobId}.");

			// TODO: RestoreBranch does not support CanvasSizeUpdateJobs
			if (!TryFindPathById(jobId, Root, out var path))
			{
				throw new InvalidOperationException($"Cannot find job: {jobId} that is being restored.");
			}

			while(path != null && !(path.Count > 1))
			{
				path = path.GetParentPath();
			}

			if (path == null || !path.NodeSafe.IsParkedAlternate)
			{
				throw new InvalidOperationException("Cannot restore this branch, it is not a \"parked\" alternate.");
			}

			var result = RestoreBranch(path);

			return result;
		}

		public bool RestoreBranch(JobPathType path)
		{
			JobPathType newPath;

			if (path.ItemSafe.TransformType == TransformType.CanvasSizeUpdate)
			{
				var parentPath = path.GetParentPath()!;
				newPath = MakeBranchActive(parentPath).Combine(path.NodeSafe);
			}
			else
			{
				newPath = MakeBranchActive(path);
			}

			ExpandAndSetCurrent(newPath);
			CurrentPath = newPath;
			IsDirty = true;
			return true;
		}

		public bool TryGetPreviousJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			if (CurrentPath == null)
			{
				job = null;
				return false;
			}

			var backPath = GetPreviousItemPath(CurrentPath, GetPredicate(skipPanJobs));
			job = backPath?.Item;

			return job != null;
		}

		public bool MoveBack(bool skipPanJobs)
		{
			if (CurrentPath == null)
			{
				return false;
			}

			var backPath = GetPreviousItemPath(CurrentPath, GetPredicate(skipPanJobs));

			if (backPath != null)
			{
				CurrentPath = backPath;
				ExpandAndSetCurrent(backPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetNextJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			if (CurrentPath == null)
			{
				job = null;
				return false;
			}

			var forwardPath = GetNextItemPath(CurrentPath, GetPredicate(skipPanJobs));
			job = forwardPath?.Item;

			return job != null;
		}

		public bool MoveForward(bool skipPanJobs)
		{
			if (CurrentPath == null)
			{
				return false;
			}

			var forwardPath = GetNextItemPath(CurrentPath, GetPredicate(skipPanJobs));

			if (forwardPath != null)
			{
				CurrentPath = forwardPath;
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

			TreeLock.EnterUpgradeableReadLock();

			try
			{
				if (TryFindParentPath(job, Root, out var parentPath))
				{
					JobTreeNode parentNode;

					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						// The parentPath points to the "original job" for which the CanvasSizeUpdate job was created.
						// We need to get its parentPath to continue.
						parentNode = parentPath.GetParentPath()!.NodeSafe;
					}
					else
					{
						parentNode = parentPath.NodeSafe;
					}

					var proxyJobTreeItem = parentNode.AlternateDispSizes?.FirstOrDefault(x => x.Item.CanvasSizeInBlocks == canvasSizeInBlocks);
					proxy = proxyJobTreeItem?.Item;
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
				TreeLock.ExitUpgradeableReadLock();
			}
		}

		public override JobPathType Add(Job item, bool selectTheAddedItem)
		{
			JobPathType newPath;
			TreeLock.EnterWriteLock();

			try
			{
				newPath = AddInternal(item, currentBranch: Root);
				IsDirty = true;
			}
			finally
			{
				TreeLock.ExitWriteLock();
			}

			if (selectTheAddedItem)
			{
				ExpandAndSetCurrent(newPath);
				CurrentPath = newPath;
			}

			return newPath;
		}

		#endregion

		#region Public Methods - Collection

		public Job? GetParent(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindItem(job.ParentJobId.Value, Root, out var result);
				return result;
			}
		}

		public JobPathType? GetParentPath(Job job, JobBranchType currentBranch)
		{
			return TryFindParentPath(job, currentBranch, out var path) ? path : null;
		}

		public bool TryFindParentPath(Job job, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		{
			path = currentBranch.GetCurrentPath();

			var parentId = job.ParentJobId;
			if (parentId == null)
			{
				return false;
			}

			if (TryFindPathById(parentId.Value, currentBranch, out var path1))
			{
				path = path1;
				return true;
			}
			else
			{
				return false;
			}
		}

		public Job? GetParentItem(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindItem(job.ParentJobId.Value, Root, out var result);
				return result;
			}
		}

		#endregion

		#region Private Add Methods

		private JobPathType AddInternal(Job job, JobBranchType currentBranch)
		{
			JobPathType newPath;
			
			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				newPath = AddCanvasSizeUpdateJob(job, currentBranch);
			}
			else
			{
				if (TryFindParentPath(job, currentBranch, out var parentPath))
				{
					newPath = AddAtParentPath(job, parentPath);
				}
				else
				{
					throw new InvalidOperationException($"Cannot find ... FIX ME FIX ME.");
				}
			}

			return newPath;
		}

		private JobPathType AddCanvasSizeUpdateJob(Job job, JobBranchType currentBranch)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's TransformType is {job.TransformType}.");
			}

			if (job.ParentJobId == null)
			{
				throw new InvalidOperationException($"Attempting to add a CanvasSizeUpdate job, but the job's parentJobId is null.");
			}

			if (TryFindParentPath(job, currentBranch, out var parentPath))
			{
				var parentNode = parentPath.NodeSafe;

				var canvasSizeUpdateNode = parentNode.AddCanvasSizeUpdateJob(job);
				var newPath = parentPath.Combine(canvasSizeUpdateNode);

				return newPath;
			}
			else
			{
				throw new InvalidOperationException($"Cannot find ... FIX ME FIX ME.");
			}

		}

		private JobPathType AddAtParentPath(Job job, JobPathType parentPath)
		{
			JobPathType newPath;

			// This is the JobTreeItem for the new Job's real parent.
			var parentNode = parentPath.NodeSafe;

			// Add the new Job to the list of it's parent's "real" children.
			// The index is the position of the new job among its siblings which are sorted by the CreatedDate, ascending.
			var index = parentNode.AddRealChild(job);

			// Get the job already in the tree for which the job being added will directly follow.
			JobPathType preceedingPath;

			if (index == 0)
			{
				// Find the sibling of the parent, that comes just before the parent.
				if (parentPath.TryGetParentPath(out var grandparentPath))
				{
					var grandparentId = grandparentPath.Item!.Id;
					var grandparentBranch = grandparentPath.GetParentBranch();

					if (TryFindPathById(grandparentId, grandparentBranch, out var realGrandparentPath))
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
				preceedingPath = GetPath(preceedingJob1, parentPath.GetParentBranch());
			}

			var preceedingJob = preceedingPath.Item;

			// Does the preceeding sibling job (in date order) move the map to a different Zoom level.
			var addingJobAsAnAlt = preceedingJob!.TransformType is TransformType.ZoomIn or TransformType.ZoomOut;

			if (addingJobAsAnAlt)
			{
				// Add the new node as a Parked ALT.
				newPath = AddAsParkedAlt(job, preceedingPath!);
			}
			else
			{
				// Add the new node in-line after the preceeding ALT Job
				newPath = AddAfter(job, preceedingPath, parentPath);
			}

			return newPath;
		}

		private JobPathType AddAsParkedAlt(Job job, JobPathType preceedingPath)
		{
			JobPathType activeAltPath;

			if (preceedingPath.NodeSafe.IsActiveAlternate)
			{
				// The preceeding node is the Active ALT.
				// Add the new job as a sibling to the branch on which my real parent's JobTreeItem was found (in sort order by date created.)
				activeAltPath = preceedingPath;
			}
			else if (preceedingPath.NodeSafe.IsParkedAlternate)
			{
				// The parent of the preceeding node is the Active ALT
				var parkedParentPath = preceedingPath.GetParentPath()!;
				activeAltPath = parkedParentPath;
			}
			else
			{
				// The preceeding node has not yet been made an Alternate.
				Debug.WriteLine($"Found a Job that is a new Alternate. Marking existing node: {preceedingPath.LastTerm} as the Active ALT.");
				preceedingPath.NodeSafe.IsActiveAlternate = true;
				activeAltPath = preceedingPath;
			}

			Debug.WriteLine($"Adding job: {job.Id}, as a Parked ALT to Active ALT: {activeAltPath.NodeSafe.Item.Id}.");

			var parkedAltPath = AddJob(job, activeAltPath);

			// TODO: See if we can avoid making the just added job be on the 'Main' branch.
			var newPath = MakeBranchActive(parkedAltPath);

			return newPath;
		}

		private JobPathType AddAfter(Job job, JobPathType preceedingPath, JobPathType parentPath)
		{
			JobPathType newPath;

			if (preceedingPath.NodeSafe.IsParkedAlternate)
			{
				if (job.ParentJobId == preceedingPath.NodeSafe.Id) // preceedingPath.LastTerm == parentPath.LastTerm)
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

		private JobPathType AddInLine(Job job, JobPathType parentPath)
		{
			Debug.WriteLine($"Adding job: {job.Id}, in-line after: {parentPath.NodeSafe.Item.Id}.");

			var grandparentBranch = GetGrandParentBranch(parentPath);

			Debug.Assert(grandparentBranch.IsEmpty || !grandparentBranch.LastTerm?.IsActiveAlternate == true, "AddJobInLine is adding a job to an Active Alt node.");
			var result = AddJob(job, grandparentBranch);

			return result;
		}

		private JobBranchType GetGrandParentBranch(JobPathType parentPath)
		{
			var grandparentPath = parentPath.GetParentBranch();

			if (parentPath.NodeSafe.IsActiveAlternate)
			{
				// The real grandparent branch is the Active Alternate's Parent
				Debug.WriteLine("The grandparentBranch is being set to the great-grandparent of the parentPath -- the parentPath points to an Active Alternate.");
				return grandparentPath.GetParentBranch();
			}
			else
			{
				return grandparentPath;
			}
		}

		private JobPathType AddJob(Job job, JobBranchType parentBranch)
		{
			var parentNode = parentBranch.GetNodeOrRoot();
			var newNode = parentNode.AddItem(job);

			if (parentNode.IsActiveAlternate)
			{
				newNode.IsParkedAlternate = true;
			}

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
		private JobPathType MakeBranchActive(JobPathType path)
		{
			if (path.IsEmpty)
			{
				throw new ArgumentException("Path cannot be empty.");
			}

			var parkedAltNode = path.NodeSafe;

			if (parkedAltNode.Item.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException("MakeActiveBranch does not support CanvasSizeUpdates.");
			}

			var activeAltNode = parkedAltNode.ParentNode!;

			if (activeAltNode is null)
			{
				throw new InvalidOperationException("Call to MakeBranchActive found no Active Alt parent of the Parked Alt being made active.");
			}

			// The parked ALT's children contains all of the job following the parked ALT.
			var ourChildern = new List<JobNodeType>(parkedAltNode.Children);

			SwitchAltBranches(parkedAltNode, activeAltNode.Node);

			var parentPath = path.GetParentPath()!;
			var grandparentItem = parentPath.GetParentNodeOrRoot();
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
		private void SwitchAltBranches(JobTreeNode parkedAlt, JobTreeNode activeAlt)
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

			Debug.WriteLine($"Switching Branches. CurrentAlt: {activeAlt.Item.Id}, CurrentAlt ParentNode: {parentNode.Item.Id}. Current Pos: {currentPosition}, NewNode: {parkedAlt.IdAndParentId}.");

			// Get a list of the items after the current position of the parent. We will use this later to add them to the node being parked.
			var jobsFollowingActiveAlt = siblings.Skip(currentPosition + 1).ToList();

			// Move the Active Alt node from the grandparent node to be a child of the node becoming the Active Alt node.
			// The new Active Alt node stores all of the parked ALTs in its list of Children.
			_ = activeAlt.Move(parkedAlt);

			// Move all children of the Active Alt node to be children of the node becoming the Active Alt node. Don't move the node becoming the Active Alt node.
			var otherAlts = new List<JobNodeType>(activeAlt.Children);
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

		private JobPathType SelectMostRecentAlternate(JobPathType currentAltPath)
		{
			var parkedAlts = currentAltPath.NodeSafe.Children;
			var mostRecentParkedAlt = parkedAlts.Aggregate((i1, i2) => (i1.Item.CompareTo(i2.Item) > 0) ? i1 : i2);

			//var result = currentAltPath.Combine((JobTreeNode)mostRecentParkedAlt);
			var result = currentAltPath.Combine(mostRecentParkedAlt.Node);

			return result;
		}

		#endregion

		#region Private Populate Methods

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

		private void ReportOutput(JobBranchType root, JobPathType? currentPath)
		{
			Debug.WriteLine($"OUTPUT Report for currentPath: {currentPath}");
			Debug.WriteLine("Id\t\t\t\t\t\t\tParentId\t\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var jwps = GetJobsWithParentage(root);

			foreach (var jwp in jwps)
			{
				var j = jwp.Item1;
				var p = jwp.Item2;
				Debug.WriteLine($"{j.Id}\t{p?.Id.ToString() ?? "null\t\t\t\t\t"}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		private JobPathType? PopulateTree(IList<Job> jobs, JobBranchType currentBranch)
		{
			var visited = 1;
			LoadChildItems(jobs, currentBranch, ref visited);

			if (visited != jobs.Count)
			{
				Debug.WriteLine($"WARNING: Only {visited} jobs out of {jobs.Count} were included during build.");
			}

			// Use the very top of the tree. The value of current branch given to this method may be pointing to the HomeNode instead of to the root.
			var tree = currentBranch.GetRoot();

			_ = MoveCurrentTo(jobs[0], tree, out var path);

			return path;
		}

		private static Job GetHomeJob(IList<Job> jobs, bool checkHomeJob)
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

			Job? homeJob;

			if (!checkHomeJob)
			{
				// Use the first job, unconditionally
				homeJob = jobs.Take(1).First();
			}
			else
			{
				// Make it an error for their not be one and only one Job of type Home.
				homeJob = jobs.FirstOrDefault(x => x.TransformType == TransformType.Home);

				if (homeJob == null)
				{
					throw new InvalidOperationException("There is no Job with TransformType = Home.");
				}
			}

			if (homeJob.ParentJobId != null)
			{
				Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has a non-null ParentJobId. Setting the ParentJobId to null.");
				homeJob.ParentJobId = null;
			}

			if (homeJob.TransformType != TransformType.Home)
			{
				Debug.WriteLine($"WARNING: The job: {homeJob.Id} used to create the JobTree(path) has TransformType of {homeJob.TransformType}. Expecting the TransformType to be {nameof(TransformType.Home)}.");
			}

			return homeJob;
		}

		private void LoadChildItems(IList<Job> jobs, JobBranchType currentBranch, ref int visited)
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
					var path = AddInternal(job, currentBranch);
					ValidateAddInternal(job, currentBranch);
					LoadChildItems(jobs, path, ref visited);
				}
			}
		}

		[Conditional("DEBUG")]
		private void ValidateAddInternal(Job job, JobBranchType currentBranch)
		{
			if (!TryFindPath(job, currentBranch.GetRoot(), out _))
			{
				throw new InvalidOperationException("Cannot find job just loaded.");
			}
		}

		private IList<Job> GetChildren(JobPathType? currentPath, IList<Job> jobs)
		{
			var parentJobId = currentPath == null ? (ObjectId?)null : currentPath.Item!.Id;
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

		private List<Tuple<Job, Job?>> GetJobsWithParentage(JobBranchType currentBranch)
		{
			var result = new List<Tuple<Job, Job?>>();

			foreach (var child in currentBranch.GetNodeOrRoot().Children)
			{
				result.Add(new Tuple<Job, Job?>(child.Item, currentBranch.Item));
				if (child.Node .AlternateDispSizes != null)
				{
					result.AddRange
						(
							child.Node.AlternateDispSizes.Select(x => new Tuple<Job, Job?>(x.Item, child.Item))
						);
				}

				var jobList = GetJobsWithParentage(currentBranch.Combine(child.Node));
				result.AddRange(jobList);
			}

			return result;
		}

		#endregion

		#region Private Navigate Methods

		private void UpdateIsSelected(JobNodeType? jobTreeItem, bool isSelected, bool useRealRelationships, JobBranchType startPos)
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

		private void UpdateIsSelectedLogical(JobNodeType? jobTreeItem, bool isSelected, JobBranchType startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.Node.IsSelected = isSelected;

				if (!TryFindPath(jobTreeItem.Item, startPos, out var path))
				{
					return;
				}

				//var ancestors = jobTreeItem.GetAncestors();
				//var path2 = _xroot.Combine(ancestors);
				//var strPath1 = string.Join("; ", path.Terms.Select(x => x.JobId.ToString()));
				//var strPath2 = string.Join("; ", path2.Terms.Select(x => x.JobId.ToString()));
				//Debug.WriteLine($"Path: {strPath1}\nPath2: {strPath2}.");

				var strPath1 = string.Join("\n\t", path.Terms.Select(x => $"Id:{x.Item.Id}, ParentId:{x.Item.ParentJobId}, Alt:{x.IsActiveAlternate}, Prk:{x.IsParkedAlternate}"));
				Debug.WriteLine($"Path: {strPath1}.");

				var backPath = GetPreviousItemPath(path, GetPredicate(skipPanJobs: true));

				if (backPath == null)
				{
					return;
				}

				// The backPath points to the first job previous to the give job that has a TransformType of Zoom-In or Zoom-Out or Home.

				// Set the parent node's IsParentOfSelected
				backPath.NodeSafe.IsParentOfSelected = isSelected;

				//// Set each sibling node's IsSiblingSelected
				//foreach (var siblingItem in parentNode.Children)
				//{
				//	siblingItem.IsSiblingOfSelected = isSelected;
				//}

				if (jobTreeItem.Node.RealChildJobs.Any())
				{
					// Use the prior job's parent path to start the search for each child.
					var parentBranch = backPath.GetParentBranch();

					// Set each child node's IsChildOfSelected
					foreach (var realChildJob in jobTreeItem.Node.RealChildJobs.Values)
					{
						if (TryFindPath(realChildJob, parentBranch, out var childPath))
						{
							childPath.NodeSafe.IsChildOfSelected = isSelected;
						}
					}
				}
				else
				{
					var forwardPath = GetNextItemPath(path, GetPredicate(skipPanJobs: true));

					if (forwardPath != null)
					{
						forwardPath.NodeSafe.IsChildOfSelected = true;
					}
				}

			}
		}

		private void UpdateIsSelectedReal(JobNodeType? jobTreeItem, bool isSelected, JobBranchType startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.Node.IsSelected = isSelected;

				if (TryFindParentPath(jobTreeItem.Item, startPos, out var realParentPath))
				{
					var realParentNode = realParentPath.NodeSafe;

					// Set the parent node's IsParentOfSelected
					realParentNode.IsParentOfSelected = isSelected;

					// Use the logical grandparent path (or root) to start the search for each sibling
					var grandparentBranch = realParentPath.GetParentBranch();

					// Set each sibling node's IsSiblingSelected
					foreach (var realSiblingJob in realParentNode.RealChildJobs.Values)
					{
						if (TryFindPath(realSiblingJob, grandparentBranch, out var siblingPath))
						{
							siblingPath.NodeSafe.IsSiblingOfSelected = isSelected;
						}
					}

					// Use the real parent path to start the search for each child.
					var logicalParentBranch = realParentPath;

					// Set each child node's IsChildOfSelected
					foreach (var realChildJob in jobTreeItem.Node.RealChildJobs.Values)
					{
						if (TryFindPath(realChildJob, logicalParentBranch, out var childPath))
						{
							childPath.NodeSafe.IsChildOfSelected = isSelected;
						}
					}

				}
			}
		}

		private Func<JobNodeType, bool>? GetPredicate(bool skipPanJobs)
		{
			Func<JobNodeType, bool>? result = skipPanJobs
				? x => x.Item.TransformType is TransformType.ZoomIn or TransformType.ZoomOut
				: null;
			return result;
		}

		#endregion
	}
}

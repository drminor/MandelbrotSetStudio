using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

//using JobBranchType = MSS.Types.ITreeBranch<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;
//using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;
//using JobNodeType = MSS.Types.ITreeNode<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;

namespace MSS.Common
{
	using JobBranchType = ITreeBranch<JobTreeNode, Job>;
	using JobPathType = ITreePath<JobTreeNode, Job>;
	using JobNodeType = ITreeNode<JobTreeNode, Job>;

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

	public abstract class JobTreeBase : Tree<JobTreeNode, Job>, IJobTree, ITree<JobTreeNode, Job>
	{
		#region Constructor

		public JobTreeBase(List<Job> jobs, bool checkHomeJob) : base(new JobTreeBranch())
		{
			var homeJob = GetHomeJob(jobs, checkHomeJob);
			CurrentPath = AddItem(homeJob, Root);
			var parentNode = CurrentPath.GetParentNodeOrRoot();
			parentNode.RealChildJobs.Add(homeJob.Id, homeJob);

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

		public override JobTreeNode? SelectedNode
		{
			get => base.SelectedNode;
			set
			{
				if (value != base.SelectedNode)
				{
					UpdateIsSelected(base.SelectedNode, false, SelectionMode, Root);
					base.SelectedNode = value;
					UpdateIsSelected(base.SelectedNode, true, SelectionMode, Root);
				}
			}
		}

		public JobTreeSelectionMode SelectionMode { get; set; }

		#endregion

		#region Public Methods

		public override bool RemoveBranch(JobPathType path)
		{
			// TODO: RemoveBranch does not support removing CanvasSizeUpdate nodes.

			var result = base.RemoveBranch(path);
			return result;
		}

		public virtual bool MakePreferred(ObjectId jobId)
		{
			Debug.WriteLine($"Marking Branch for {jobId}, the preferred branch.");

			// TODO: MakePreferred does not support CanvasSizeUpdateJobs
			if (!TryFindPathById(jobId, Root, out var path))
			{
				throw new InvalidOperationException($"Cannot find job: {jobId} for which to mark the branch as preferred.");
			}

			var result = MakePreferred(path);

			return result;
		}

		public abstract bool MakePreferred(JobPathType? path);
		//{
		//	if (path == null)
		//	{
		//		Root.GetNodeOrRoot().PreferredChild = null;
		//	}
		//	else
		//	{
		//		// TODO: Get a path to the node identifed by path, based the node's ParentId.
		//		// Then set the preferred child top, down.
		//		var originalPathTerms = path.ToString();
		//		//var parentPath = path.GetParentPath();

		//		//while (parentPath != null)
		//		//{
		//		//	try
		//		//	{
		//		//		parentPath.Node.PreferredChild = path.Node;
		//		//	} 
		//		//	catch (InvalidOperationException ioe)
		//		//	{
		//		//		Debug.WriteLine($"Error1 while setting the parentNode: {parentPath.Node.Id}'s PreferredChild to {path.Node.Id}. The original path has terms: {originalPathTerms}. \nThe error is {ioe}");
		//		//	}

		//		//	path = parentPath;
		//		//	parentPath = path.GetParentPath();
		//		//}

		//		//var parentNode = path.GetParentNodeOrRoot();

		//		//try
		//		//{
		//		//	parentNode.PreferredChild = path.Node;
		//		//}
		//		//catch (InvalidOperationException ioe)
		//		//{
		//		//	Debug.WriteLine($"Error2 while setting the parentNode: {parentNode.Id}'s PreferredChild to {path.Node.Id}. The original path has terms: {originalPathTerms}. \nThe error is {ioe}");
		//		//}

		//		var parentNode = path.GetParentNodeOrRoot();

		//		try
		//		{
		//			parentNode.PreferredChild = path.Node;
		//		}
		//		catch (InvalidOperationException ioe)
		//		{
		//			Debug.WriteLine($"Error2 while setting the parentNode: {parentNode.Id}'s PreferredChild to {path.Node.Id}. The original path has terms: {originalPathTerms}. \nThe error is {ioe}");
		//		}


		//	}

		//	return true;
		//}

		public virtual bool TryGetPreviousJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			var result = TryGetPreviousItemPath(out var backPath, GetPredicate(skipPanJobs));
			job = backPath?.Item;
			return result;
		}

		public virtual bool MoveBack(bool skipPanJobs)
		{
			var result = MoveBack(GetPredicate(skipPanJobs));
			return result;
		}

		public virtual bool TryGetNextJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			var result = TryGetNextItemPath(out var forwardPath, GetPredicate(skipPanJobs));
			job = forwardPath?.Item;
			return result;
		}

		public virtual bool MoveForward(bool skipPanJobs)
		{
			var result = MoveForward(GetPredicate(skipPanJobs));
			return result;
		}

		public virtual bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy)
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
						parentNode = parentPath.GetParentPath()!.Node;
					}
					else
					{
						parentNode = parentPath.Node;
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

		#endregion

		#region Public Methods - Collection

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

		public JobPathType? GetParentPath(Job job, JobBranchType currentBranch)
		{
			return TryFindParentPath(job, currentBranch, out var path) ? path : null;
		}

		public bool TryFindParentPath(Job job, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		{
			path = currentBranch.GetCurrentPath();
			var parentId = job.ParentJobId;
			var result = parentId == null ? false : TryFindPathById(parentId.Value, currentBranch, out path);

			return result;
		}

		#endregion

		#region Private Add Methods

		protected override JobPathType AddInternal(Job job, JobBranchType currentBranch)
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

		protected JobPathType AddCanvasSizeUpdateJob(Job job, JobBranchType currentBranch)
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
				var parentNode = parentPath.Node;

				var canvasSizeUpdateNode = parentNode.AddCanvasSizeUpdateJob(job);
				var newPath = parentPath.Combine(canvasSizeUpdateNode);

				return newPath;
			}
			else
			{
				throw new InvalidOperationException($"Cannot find ... FIX ME FIX ME.");
			}
		}

		protected abstract JobPathType AddAtParentPath(Job job, JobPathType parentPath);

		#endregion

		#region Private Populate Methods

		protected virtual void ReportInput(IList<Job> jobs)
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

		protected virtual void ReportOutput(JobBranchType root, JobPathType? currentPath)
		{
			Debug.WriteLine($"OUTPUT Report for currentPath: {currentPath}");
			Debug.WriteLine("Id\t\t\t\t\t\t\tParentId\t\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var nodeAndParents = GetNodesWithParentage(root);

			foreach (var nodeAndParent in nodeAndParents)
			{
				var job = nodeAndParent.Item1.Item;
				var parentJob = nodeAndParent.Item2?.Item;
				Debug.WriteLine($"{job.Id}\t{parentJob?.Id.ToString() ?? "null\t\t\t\t\t"}\t{job.DateCreated}\t{job.TransformType}\t{job.Id.Timestamp}");
			}
		}

		protected virtual JobPathType? PopulateTree(IList<Job> jobs, JobBranchType currentBranch)
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

		protected virtual Job GetHomeJob(IList<Job> jobs, bool checkHomeJob)
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

		protected virtual void LoadChildItems(IList<Job> jobs, JobBranchType currentBranch, ref int visited)
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

		protected virtual IList<Job> GetChildren(JobPathType? currentPath, IList<Job> jobs)
		{
			var parentJobId = currentPath == null ? (ObjectId?)null : currentPath.Item!.Id;
			parentJobId = parentJobId == ObjectId.Empty ? null : parentJobId;
			var result = jobs.Where(x => x.ParentJobId == parentJobId).OrderBy(x => x.Id.Timestamp).ToList();

			return result;
		}

		[Conditional("DEBUG")]
		private void ValidateAddInternal(Job job, JobBranchType currentBranch)
		{
			if (!TryFindPath(job, currentBranch.GetRoot(), out _))
			{
				throw new InvalidOperationException("Cannot find job just loaded.");
			}
		}

		#endregion

		#region Private Navigate Methods

		protected virtual void UpdateIsSelected(JobTreeNode? jobTreeNode, bool isSelected, JobTreeSelectionMode jobTreeSelectionMode, JobBranchType startPos)
		{
			if (jobTreeNode == null)
			{
				return;
			}

			var node = jobTreeNode;

			if (jobTreeSelectionMode == JobTreeSelectionMode.Real)
			{
				UpdateIsSelectedReal(node, isSelected, startPos);
			}
			else
			{
				UpdateIsSelectedLogical(node, isSelected, startPos);
			}
		}

		protected virtual void UpdateIsSelectedLogical(JobTreeNode node,  bool isSelected, JobBranchType startPos)
		{
			node.IsSelected = isSelected;

			var path = startPos.CreatePath(node);

			var backPath = GetPreviousItemPath(path, GetPredicate(skipPanJobs: true));

			if (backPath == null)
			{
				return;
			}

			// The backPath points to the first job previous to the give job that has a TransformType of Zoom-In or Zoom-Out or Home.

			// Set the parent node's IsParentOfSelected
			backPath.Node.IsParentOfSelected = isSelected;

			//// Set each sibling node's IsSiblingSelected
			//foreach (var siblingItem in parentNode.Children)
			//{
			//	siblingItem.IsSiblingOfSelected = isSelected;
			//}

			//if (jobTreeItem.Node.RealChildJobs.Any())
			//{
			//	// Use the prior job's parent path to start the search for each child.
			//	var parentBranch = backPath.GetParentBranch();

			//	// Set each child node's IsChildOfSelected
			//	foreach (var realChildJob in jobTreeItem.Node.RealChildJobs.Values)
			//	{
			//		if (TryFindPath(realChildJob, parentBranch, out var childPath))
			//		{
			//			childPath.Node.IsChildOfSelected = isSelected;
			//		}
			//	}
			//}
			//else
			//{
			//	var forwardPath = GetNextItemPath(path, GetPredicate(skipPanJobs: true));

			//	if (forwardPath != null)
			//	{
			//		forwardPath.Node.IsChildOfSelected = true;
			//	}
			//}

		}

		protected virtual void UpdateIsSelectedReal(JobTreeNode node, bool isSelected, JobBranchType startPos)
		{
			node.IsSelected = isSelected;

			var path = startPos.CreatePath(node);

			// TODO: Get the real parent
			var parentPath = path.GetParentPath();

			if (parentPath != null)
			{
				// Set the parent node's IsParentOfSelected
				parentPath.Node.IsParentOfSelected = isSelected;

				// Use the logical grandparent path (or root) to start the search for each sibling
				var grandparentBranch = parentPath.GetParentBranch();

				// Set each sibling node's IsSiblingSelected -- Using RealChildJobs
				foreach (var realSiblingJob in parentPath.Node.RealChildJobs.Values)
				{
					if (TryFindPath(realSiblingJob, grandparentBranch, out var siblingPath))
					{
						siblingPath.Node.IsSiblingOfSelected = isSelected;
					}
				}

				// Set each sibling node's IsSiblingSelected -- Using Logical Children
				foreach (var siblingNode in parentPath.Node.Children)
				{
					siblingNode.IsSiblingOfSelected = isSelected;
				}
			}

			// Use the real parent path to start the search for each child.
			var logicalParentBranch = path.GetParentBranch();

			// Set each child node's IsChildOfSelected
			foreach (var realChildJob in node.RealChildJobs.Values)
			{
				if (TryFindPath(realChildJob, logicalParentBranch, out var childPath))
				{
					childPath.Node.IsChildOfSelected = isSelected;
				}
			}
		}

		protected override JobPathType? GetNextItemPath(JobPathType path, Func<JobTreeNode, bool>? predicate)
		{
			var currentPosition = GetPosition(path, out var siblings);
			var nextNode = GetNextNode(siblings, currentPosition, predicate);

			if (nextNode != null)
			{
				//The new item will be a sibling of the current item
				var result = path.Combine(nextNode);
				return result;
			}
			else
			{
				return null;
			}
		}

		private IEnumerable<JobTreeNode>? GetNextNode(IList<JobTreeNode> nodes, int currentPosition, Func<JobTreeNode, bool>? predicate)
		{
			IEnumerable<JobTreeNode>? result = null;

			if (predicate != null)
			{
				for (var i = currentPosition; i < nodes.Count; i++)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null && predicate(preferredNode))
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i > currentPosition && predicate(node))
						{
							result = new[] { node };
							break;
						}
					}
				}
			}
			else
			{
				for (var i = currentPosition; i < nodes.Count; i++)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null)
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i > currentPosition)
						{
							result = new[] { node };
							break;
						}
					}
				}
			}

			return result;
		}

		protected override JobPathType? GetPreviousItemPath(JobPathType path, Func<JobTreeNode, bool>? predicate)
		{
			var currentPosition = GetPosition(path, out var siblings);
			var previousNewPathTerms = GetPreviousNode(siblings, currentPosition, predicate);

			var previousPath = path;
			var curPath = path.GetParentPath();

			while (previousNewPathTerms == null && curPath != null)
			{
				currentPosition = GetPosition(curPath, out siblings);
				previousNewPathTerms = GetPreviousNode(siblings, currentPosition, predicate);

				previousPath = curPath;
				curPath = curPath.GetParentPath();
			}

			if (previousNewPathTerms != null)
			{
				var result = previousPath.Combine(previousNewPathTerms);
				return result;
			}
			else
			{
				return null;
			}
		}

		private IEnumerable<JobTreeNode>? GetPreviousNode(IList<JobTreeNode> nodes, int currentPosition, Func<JobTreeNode, bool>? predicate)
		{
			IEnumerable<JobTreeNode>? result = null;

			if (predicate != null)
			{
				for (var i = currentPosition; i >= 0; i--)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null && predicate(preferredNode))
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i < currentPosition && predicate(node))
						{
							result = new[] { node };
							break;
						}
					}
				}
			}
			else
			{
				for (var i = currentPosition; i >= 0; i--)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null)
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i < currentPosition)
						{
							result = new[] { node };
							break;
						}
					}
				}
			}

			return result;
		}

		protected Func<JobTreeNode, bool>? GetPredicate(bool skipPanJobs)
		{
			Func<JobTreeNode, bool>? result = skipPanJobs
				? x => DoesNodeChangeZoom(x)
				: null;
			return result;
		}

		protected virtual bool DoesNodeChangeZoom(JobTreeNode node)
		{
			var result = node.TransformType is TransformType.ZoomIn or TransformType.ZoomOut or TransformType.Home;
			return result;
		}

		#endregion
	}
}

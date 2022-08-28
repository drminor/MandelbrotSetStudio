using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
					if (value != null && value.TransformType == TransformType.CanvasSizeUpdate)
					{
						throw new InvalidOperationException("Cannot set the Selected Node to a CanvasSizeUpdate job.");
					}

					UpdateIsSelected(base.SelectedNode, false, Root);
					base.SelectedNode = value;
					UpdateIsSelected(base.SelectedNode, true, Root);
				}
			}
		}

		#endregion

		#region Public Methods

		public override bool RemoveBranch(JobPathType path)
		{
			// TODO: RemoveBranch does not support removing CanvasSizeUpdate nodes.

			var result = base.RemoveBranch(path);
			return result;
		}

		public IList<JobPathType> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			var result = new List<JobPathType>();

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

		public bool TryGetPreviousJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			var result = TryGetPreviousItemPath(out var backPath, GetPredicate(skipPanJobs));
			job = backPath?.Item;
			return result;
		}

		public bool MoveBack(bool skipPanJobs)
		{
			var result = MoveBack(GetPredicate(skipPanJobs));
			return result;
		}

		public bool TryGetNextJob([MaybeNullWhen(false)] out Job job, bool skipPanJobs)
		{
			var result = TryGetNextItemPath(out var forwardPath, GetPredicate(skipPanJobs));
			job = forwardPath?.Item;
			return result;
		}

		public bool MoveForward(bool skipPanJobs)
		{
			var result = MoveForward(GetPredicate(skipPanJobs));
			return result;
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
				if (TryFindPath(job, Root, out var path))
				{
					JobTreeNode? parentNode;

					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						// The path points to a CanvasSizeUpdate job that has a different size that what is sought.
						// We need to get its parent to continue.
						parentNode = path.GetParentPath()?.Node;
					}
					else
					{
						parentNode = path.Node;
					}

					var proxyJobTreeItem = parentNode?.AlternateDispSizes?.FirstOrDefault(x => x.Item.CanvasSizeInBlocks == canvasSizeInBlocks);
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
				var csuPath = AddCanvasSizeUpdateJob(job, currentBranch);
				newPath = csuPath.GetParentPath()!;
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

			var nodeAndParents = GetNodesWithParentage(root);

			foreach (var nodeAndParent in nodeAndParents)
			{
				var job = nodeAndParent.Item1.Item;
				var parentJob = nodeAndParent.Item2?.Item;
				Debug.WriteLine($"{job.Id}\t{parentJob?.Id.ToString() ?? "null\t\t\t\t\t"}\t{job.DateCreated}\t{job.TransformType}\t{job.Id.Timestamp}");
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

		private Job GetHomeJob(IList<Job> jobs, bool checkHomeJob)
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

		private IList<Job> GetChildren(JobPathType? currentPath, IList<Job> jobs)
		{
			var parentJobId = currentPath == null ? (ObjectId?)null : currentPath.Item.Id;
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

		#region Private Collection Methods

		private bool TryFindCanvasSizeUpdateJob(Job job, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		{
			if (job.ParentJobId == null)
			{
				throw new ArgumentException("The job must have a non-null ParentJobId when finding a CanvasSizeUpdate job.", nameof(job));
			}

			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new ArgumentException("The job must have a TransformType = CanvasSizeUpdate when finding a CanvasSizeUpdate job.", nameof(job));
			}

			if (TryFindParentPath(job, currentBranch, out var parentPath))
			{
				JobTreeNode parentNode = parentPath.Node;

				var canvasSizeUpdateNode = parentNode.AlternateDispSizes?.FirstOrDefault(x => x.Item == job);

				if (canvasSizeUpdateNode != null)
				{
					path = parentPath.Combine(canvasSizeUpdateNode);
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

		#endregion

		#region Private Navigate Methods

		protected virtual void UpdateIsSelected(JobTreeNode? node, bool isSelected, JobBranchType startPos)
		{
			if (node == null)
			{
				return;
			}

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
			JobPathType? result;

			if (predicate != null)
			{
				result = GetNextPath(path);

				while (result != null && !predicate(result.Node))
				{
					result = GetNextPath(result);
				}
			}
			else
			{
				result = GetNextPath(path);
			}

			return result;
		}

		private JobPathType? GetNextPath(JobPathType path)
		{
			JobPathType? result;

			var currentPosition = GetPosition(path, out var siblings);
			var currentNode = siblings[currentPosition];
			var preferredNode = currentNode.PreferredChild;

			if (preferredNode != null && preferredNode.ParentId == currentNode.Id)
			{
				result = path.Combine(preferredNode);
				//Debug.WriteLine($"GetNextPath, using the node's preferred child.\n{result}");
			}
			else if (currentPosition < siblings.Count - 1 && siblings[currentPosition + 1].ParentId == currentNode.Id)
			{
				var nextNode = siblings[currentPosition + 1];
				result = path.CreateSiblingPath(nextNode);
				//Debug.WriteLine($"GetNextPath, using the next node.\n{result}");
			}
			else
			{
				// TODO: Optimize calls to GetPath by providing a closer starting point.
				//var realChildPaths = currentNode.RealChildJobs.Select(x => GetPath(x.Value, Root));
				//result = realChildPaths.FirstOrDefault(x => x.Node.IsOnPreferredPath) ?? realChildPaths.LastOrDefault();

				// Note: This assumes that every node has its "IsOnPreferredPath" value assigned.
				// TODO: Optimize calls to GetPath by providing a closer starting point.
				var preferredRealChildJob = currentNode.RealChildJobs.FirstOrDefault(x => x.Value.IsAlternatePathHead).Value ?? currentNode.RealChildJobs.LastOrDefault().Value;
				result = preferredRealChildJob != null ? GetPath(preferredRealChildJob, Root) : null;

				//Debug.WriteLine($"GetNextPath, initial result is null, using RealChildJobs.\n{result}.");
			}

			return result;
		}

		protected override JobPathType? GetPreviousItemPath(JobPathType path, Func<JobTreeNode, bool>? predicate)
		{
			JobPathType? result;

			if (predicate != null)
			{
				result = GetPreviousPath(path);

				while (result != null && !predicate(result.Node))
				{
					result = GetPreviousPath(result);
				}
			}
			else
			{
				result = GetPreviousPath(path);
			}

			return result;
		}

		private JobPathType? GetPreviousPath(JobPathType path)
		{
			JobPathType? result;

			var currentPosition = GetPosition(path, out var siblings);
			var currentNode = siblings[currentPosition];

			if (currentPosition > 0 && siblings[currentPosition - 1].ParentId == currentNode.Id)
			{
				var previousNode = siblings[currentPosition - 1];

				if (previousNode.Children.Count > 0)
				{
					var preferredNode = previousNode.PreferredChild;

					if (preferredNode?.ParentId == currentNode.Id)
					{
						result = path.CreateSiblingPath(previousNode).Combine(preferredNode);
						//Debug.WriteLine($"GetPreviousPath, using the node's preferred child.\n{result}");
					}
					else
					{
						//Debug.WriteLine($"GetPreviousPath, node has children, the preferredNode has ParentId: {preferredNode?.ParentId}, the currentNode has id: {currentNode.Id}.\n{result}");
						result = null;
					}
				}
				else
				{
					result = path.CreateSiblingPath(previousNode);
					//Debug.WriteLine($"GetPreviousPath, using the previous node.\n{result}");
				}
			}
			else
			{
				result = null;
			}

			if (result == null)
			{
				// TODO: Optimize call to GetParentPath by providing a closer starting point.
				result = GetParentPath(currentNode.Item, Root);
				//Debug.WriteLine($"GetPreviousPath, initial result is null, using GetParentPath.\n{result}.");
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

		protected override bool MoveCurrentTo(Job item, JobBranchType currentBranch, [MaybeNullWhen(false)] out JobPathType path)
		{
			if (item.TransformType == TransformType.CanvasSizeUpdate)
			{
				if (TryFindCanvasSizeUpdateJob(item, currentBranch, out var csuPath))
				{
					path = csuPath.GetParentPath()!;
					ExpandAndSetCurrent(path);
					return true;
				}
			}
			else
			{
				if (TryFindPath(item, currentBranch, out path))
				{
					ExpandAndSetCurrent(path);
					return true;
				}
			}

			path = null;
			return false;
		}

		#endregion
	}
}

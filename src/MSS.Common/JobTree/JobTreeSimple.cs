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

	public class JobTreeSimple : Tree<JobTreeNode, Job>, IJobTree
	{
		#region Constructor

		public JobTreeSimple(List<Job> jobs, bool checkHomeJob) : base(new JobTreeBranch())
		{
			var homeJob = GetHomeJob(jobs, checkHomeJob);
			CurrentPath = AddJob(homeJob, Root);
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

		public override JobNodeType? SelectedNode
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

		public bool RestoreBranch(ObjectId jobId)
		{
			throw new NotImplementedException("JobTreeSimple does not support Alternate Branches.");
		}

		public bool RestoreBranch(JobPathType path)
		{
			throw new NotImplementedException("JobTreeSimple does not support Alternate Branches.");
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

		private JobPathType AddAtParentPath(Job job, JobPathType parentPath)
		{
			var parentNode = parentPath.Node;
			var grandparentNode = parentPath.GetParentNodeOrRoot();

			JobPathType newPath;

			if (parentNode.RealChildJobs.Count > 0)
			{
				var existingJob = parentNode.RealChildJobs.Values[0];

				if (TryFindPath(existingJob, Root, out var path))
				{
					Debug.WriteLine($"Moving job: {existingJob.Id}, to be a child of {parentNode.Id}.");
					_ = path.Node.Move(parentNode);
				}

				Debug.WriteLine($"Adding job: {job.Id}, as a child of {parentPath.Node.Id}.");
				newPath = AddJob(job, parentPath);
			}
			else
			{
				Debug.WriteLine($"Adding job: {job.Id}, in-line after {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
				newPath = AddInLine(job, parentPath);
			}

			_ = parentNode.AddRealChild(job);

			return newPath;
		}

		private JobPathType AddInLine(Job job, JobPathType parentPath)
		{
			JobPathType newPath;

			var grandparentPath = parentPath.GetParentPath();

			if (grandparentPath != null && grandparentPath.Node.Id == parentPath.Node.ParentJobId)
			{
				// The parentPath is a child of a set of Alternate Paths, we want the new node to be a sibling of the parent's parent.
				var greatGrandparentBranch = grandparentPath.GetParentBranch();
				newPath = AddJob(job, greatGrandparentBranch);
			}
			else
			{
				var grandparentBranch = parentPath.GetParentBranch();
				newPath = AddJob(job, grandparentBranch);
			}

			return newPath;
		}

		private JobPathType AddJob(Job job, JobBranchType parentBranch)
		{
			var parentNode = parentBranch.GetNodeOrRoot();
			var newNode = parentNode.AddItem(job);

			var result = parentBranch.Combine(newNode);

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

		private List<Tuple<Job, Job?>> GetJobsWithParentage(JobBranchType currentBranch)
		{
			var result = new List<Tuple<Job, Job?>>();

			foreach (var child in currentBranch.GetNodeOrRoot().Children)
			{
				result.Add(new Tuple<Job, Job?>(child.Item, currentBranch.LastTerm?.Item));
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

		private void UpdateIsSelected(JobNodeType? jobTreeItem, bool isSelected, JobTreeSelectionMode jobTreeSelectionMode, JobBranchType startPos)
		{
			if (jobTreeSelectionMode == JobTreeSelectionMode.Real)
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

				//var strPath1 = string.Join("\n\t", path.Terms.Select(x => $"Id:{x.Item.Id}, ParentId:{x.Item.ParentJobId}, Alt:{x.IsActiveAlternate}, Prk:{x.IsParkedAlternate}"));
				//Debug.WriteLine($"Path: {strPath1}.");

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
		}

		private void UpdateIsSelectedReal(JobNodeType? jobTreeItem, bool isSelected, JobBranchType startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.Node.IsSelected = isSelected;

				if (TryFindParentPath(jobTreeItem.Item, startPos, out var realParentPath))
				{
					var realParentNode = realParentPath.Node;

					// Set the parent node's IsParentOfSelected
					realParentNode.IsParentOfSelected = isSelected;

					// Use the logical grandparent path (or root) to start the search for each sibling
					var grandparentBranch = realParentPath.GetParentBranch();

					//// Set each sibling node's IsSiblingSelected
					//foreach (var realSiblingJob in realParentNode.RealChildJobs.Values)
					//{
					//	if (TryFindPath(realSiblingJob, grandparentBranch, out var siblingPath))
					//	{
					//		siblingPath.Node.IsSiblingOfSelected = isSelected;
					//	}
					//}

					//// Use the real parent path to start the search for each child.
					//var logicalParentBranch = realParentPath;

					//// Set each child node's IsChildOfSelected
					//foreach (var realChildJob in jobTreeItem.Node.RealChildJobs.Values)
					//{
					//	if (TryFindPath(realChildJob, logicalParentBranch, out var childPath))
					//	{
					//		childPath.Node.IsChildOfSelected = isSelected;
					//	}
					//}
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

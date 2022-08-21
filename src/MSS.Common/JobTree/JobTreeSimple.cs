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

	public class JobTreeSimple : JobTreeBase
	{
		#region Constructor

		public JobTreeSimple(List<Job> jobs, bool checkHomeJob) : base(jobs, checkHomeJob)
		{ }

		#endregion

		#region Private Add Methods

		protected override JobPathType AddAtParentPath(Job job, JobPathType parentPath)
		{
			var parentNode = parentPath.Node;

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
				//Debug.WriteLine($"Adding job: {job.Id}, in-line after {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
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
				//// The parentPath is a child of a set of Alternate Paths, we want the new node to be a sibling of the parent's parent.
				//var greatGrandparentBranch = grandparentPath.GetParentBranch();
				//var greatGrandparentNode = grandparentPath.GetParentNodeOrRoot();

				//Debug.WriteLine($"Adding job: {job.Id}, as a sibling to its grandparent: {grandparentPath.Node.Id} as a child of {greatGrandparentNode.Id}.");
				//newPath = AddJob(job, greatGrandparentBranch);

				var grandparentBranch = parentPath.GetParentBranch();
				var grandparentNode = parentPath.GetParentNodeOrRoot();
				Debug.WriteLine($"Adding job: {job.Id}, asx sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
				newPath = AddJob(job, grandparentBranch);
			}
			else
			{
				if (parentPath.Node.TransformType is TransformType.ZoomIn or TransformType.ZoomOut or TransformType.Home)
				{
					// The parent changes the Zoom level (and is not one of several alternates) -- and so starts a branch
					Debug.WriteLine($"Adding job: {job.Id}, as a child of {parentPath.Node.Id}. The parent's TransformType is {parentPath.Node.TransformType}.");
					newPath = AddJob(job, parentPath);
				}
				else
				{
					var grandparentBranch = parentPath.GetParentBranch();
					var grandparentNode = parentPath.GetParentNodeOrRoot();
					Debug.WriteLine($"Adding job: {job.Id}, a sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
					newPath = AddJob(job, grandparentBranch);
				}
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

		#region Private Navigate Methods

		protected override void UpdateIsSelectedLogical(JobNodeType? jobTreeItem, bool isSelected, JobBranchType startPos)
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

		protected override void UpdateIsSelectedReal(JobNodeType? jobTreeItem, bool isSelected, JobBranchType startPos)
		{
			if (jobTreeItem != null)
			{
				jobTreeItem.Node.IsSelected = isSelected;

				if (TryFindParentPath(jobTreeItem.Item, startPos, out var realParentPath))
				{
					var realParentNode = realParentPath.Node;

					// Set the parent node's IsParentOfSelected
					realParentNode.IsParentOfSelected = isSelected;

					//// Use the logical grandparent path (or root) to start the search for each sibling
					//var grandparentBranch = realParentPath.GetParentBranch();

					//// Set each sibling node's IsSiblingSelected
					//foreach (var realSiblingJob in realParentNode.RealChildJobs.Values)
					//{
					//	if (TryFindPath(realSiblingJob, grandparentBranch, out var siblingPath))
					//	{
					//		siblingPath.Node.IsSiblingOfSelected = isSelected;
					//	}
					//}

					// Use the real parent path to start the search for each child.
					var logicalParentBranch = realParentPath;

					// Set each child node's IsChildOfSelected
					foreach (var realChildJob in jobTreeItem.Node.RealChildJobs.Values)
					{
						if (TryFindPath(realChildJob, logicalParentBranch, out var childPath))
						{
							childPath.Node.IsChildOfSelected = isSelected;
						}
					}
				}
			}
		}

		#endregion
	}
}

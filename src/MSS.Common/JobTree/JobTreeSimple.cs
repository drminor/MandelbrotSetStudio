using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
				newPath = AddItem(job, parentPath);
			}
			else if (DoesNodeChangeZoom(parentNode))
			{
				Debug.WriteLine($"Adding job: {job.Id}, as a child of {parentPath.Node.Id}. The parent's TransformType is {parentPath.Node.TransformType}.");
				newPath = AddItem(job, parentPath);
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
				//newPath = AddItem(job, greatGrandparentBranch);

				var grandparentBranch = parentPath.GetParentBranch();
				var grandparentNode = parentPath.GetParentNodeOrRoot();
				Debug.WriteLine($"Adding job: {job.Id}, asx sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
				newPath = AddItem(job, grandparentBranch);
			}
			else
			{
				var grandparentBranch = parentPath.GetParentBranch();
				var grandparentNode = parentPath.GetParentNodeOrRoot();
				Debug.WriteLine($"Adding job: {job.Id}, a sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
				newPath = AddItem(job, grandparentBranch);
			}

			return newPath;
		}

		#endregion

		#region Private Navigate Methods

		protected override JobNodeType? GetNextNode(IList<JobNodeType> nodes, int currentPosition, Func<JobNodeType, bool>? predicate = null)
		{
			JobNodeType? result;

			if (predicate != null)
			{
				result = null;
				for(var i = currentPosition; i < nodes.Count; i++)
				{
					var node = nodes[i];

					if (node.Children.Count > 0)
					{
						var preferredNode = node.Node.PreferredChild;
						if (preferredNode != null)
						{
							if (predicate(preferredNode))
							{
								// TODO: Check the impact of returning a child of one of the items in the list,
								// as opposed to returning one of the items in the list.
								result = preferredNode; 
								break;
							}
						}
					}

					if (i > currentPosition && predicate(node))
					{
						result = node;
						break;
					}
				}
			}
			else
			{
				result = nodes.Skip(currentPosition).FirstOrDefault()?.Node.PreferredChild ?? nodes.Skip(currentPosition + 1).FirstOrDefault();
			}

			return result;
		}

		protected override JobNodeType? GetPreviousNode(IList<JobNodeType> nodes, int currentPosition, Func<JobNodeType, bool>? predicate = null)
		{
			var result = predicate != null
				? nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault(predicate)
				: nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault();
			return result;
		}

		protected override void UpdateIsSelectedLogical(JobTreeNode node, bool isSelected, JobBranchType startPos)
		{
				node.IsSelected = isSelected;

				if (!TryFindPath(node.Item, startPos, out var path))
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

		protected override void UpdateIsSelectedReal(JobTreeNode node, bool isSelected, JobBranchType startPos)
		{
			node.IsSelected = isSelected;

			if (!TryFindPath(node.Item, startPos, out var path))
			{
				return;
			}

			var parentPath = path.GetParentPath();

			if (parentPath != null)
			{
				// Set the parent node's IsParentOfSelected
				parentPath.Node.IsParentOfSelected = isSelected;

				// Use the logical grandparent path (or root) to start the search for each sibling
				var grandparentBranch = parentPath.GetParentBranch();

				// Set each sibling node's IsSiblingSelected -- Using RealChildJobs
				//foreach (var realSiblingJob in parentPath.Node.RealChildJobs.Values)
				//{
				//	if (TryFindPath(realSiblingJob, grandparentBranch, out var siblingPath))
				//	{
				//		siblingPath.Node.IsSiblingOfSelected = isSelected;
				//	}
				//}

				// Set each sibling node's IsSiblingSelected -- Using Logical Children
				foreach (var siblingNode in parentPath.Node.Children)
				{
					siblingNode.Node.IsSiblingOfSelected = isSelected;
				}
			}

			// Use the real parent path to start the search for each child.
			var logicalParentBranch = path.GetParentBranch();

			// Set each child node's IsChildOfSelected
			foreach (var realChildJob in node.Node.RealChildJobs.Values)
			{
				if (TryFindPath(realChildJob, logicalParentBranch, out var childPath))
				{
					childPath.Node.IsChildOfSelected = isSelected;
				}
			}

		}

		#endregion
	}
}

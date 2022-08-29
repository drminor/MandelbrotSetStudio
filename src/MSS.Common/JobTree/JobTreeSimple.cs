using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace MSS.Common
{
	using JobBranchType = ITreeBranch<JobTreeNode, Job>;
	using JobPathType = ITreePath<JobTreeNode, Job>;

	public class JobTreeSimple : JobTreeBase
	{
		#region Constructor

		public JobTreeSimple(List<Job> jobs, bool checkHomeJob) : base(jobs, checkHomeJob)
		{ }

		#endregion

		#region Public Methods

		public override bool MakePreferred(JobPathType? path)
		{
			var numberSet = 0;
			var numberReset = 0;

			//var sb = new StringBuilder();
			//_ = sb.AppendLine("*****");

			if (path == null)
			{
				Debug.WriteLine("MakePreferred is clearing all node.");
				Root.GetNodeOrRoot().PreferredChild = null;
			}
			else
			{
				// Assemble a list of parents and their preferred child for each term
				// of a path constructed using the specified path's "real" parent.

				var parentNode = path.GetParentNodeOrRoot();
				var parentChildPairs = new[] { (parent: parentNode, child: path.Node) }.ToList();

				var previousPath = path;
				var nextPath = GetParentPath(previousPath.Item, Root); // Using the real parent Id here.

				while (nextPath != null)
				{
					parentNode = nextPath.GetParentNodeOrRoot();
					parentChildPairs.Add((parentNode, nextPath.Node));

					previousPath = nextPath;
					nextPath = GetParentPath(previousPath.Item, Root);
				}

				// Set the preferred child of the root node using the very first term of the given path.
				parentNode = previousPath.GetParentNodeOrRoot();
				parentChildPairs.Add((parentNode, previousPath.Node));

				// Using the assembled list, set the preferred child, using its logical parent, starting from the top.
				parentChildPairs.Reverse();

				var vistedNodeIds = new List<ObjectId>();
				foreach (var (parent, child) in parentChildPairs)
				{
					var alreadyVisited = vistedNodeIds.Contains(parent.Id);
					vistedNodeIds.Add(parent.Id);
					if (parent.SetPreferredChild(child, !alreadyVisited, ref numberReset))
					{
						var strResetingChildNodes = !alreadyVisited ? ", and reseting child nodes." : ".";
						//_ = sb.AppendLine($"Setting node: {child.Id} to be the preferred child of parent: {parent.Id}{strResetingChildNodes}");
						numberSet++;
					}
					else
					{
						//_ = sb.AppendLine($"Node: {child.Id} is already the preferred child of parent: {parent.Id}.");
					}
				}

				//_ = sb.AppendLine("Now selecting the child's last child as the preferred child.");

				//	For each child of the last term, set the last child to be the preferred child.
				var nextChildJob = path.Node.RealChildJobs.LastOrDefault().Value;

				while (nextChildJob != null)
				{
					var nextChildPath = GetPath(nextChildJob, Root);
					var child = nextChildPath.Node; 
					parentNode = nextChildPath.GetParentNodeOrRoot();

					var alreadyVisited = vistedNodeIds.Contains(parentNode.Id);
					vistedNodeIds.Add(parentNode.Id);

					if (parentNode.SetPreferredChild(child, !alreadyVisited, ref numberReset))
					{
						//var strResetingChildNodes = !alreadyVisited ? ", and reseting child nodes." : ".";
						//_ = sb.AppendLine($"Setting node: {child.Id} to be the preferred child of parent: {parentNode.Id}{strResetingChildNodes}");
						numberSet++;
					}
					else
					{
						//_ = sb.AppendLine($"Node: {nextChildPath.Node.Id} is already the preferred child of parent: {parentNode.Id}.");
					}

					nextChildJob = nextChildPath.Node.RealChildJobs.LastOrDefault().Value;
				}
			}

			//_ = sb.AppendLine($"Setting path: {path?.Node.Id} to be the preferred path. Set {numberSet} nodes, reset {numberReset}.");
			//_ = sb.AppendLine("*****");

			//Debug.WriteLine(sb.ToString());

			return true;
		}

		public override bool RemoveNode(JobPathType path)
		{
			var removedJobs = RemoveJobs(path, NodeSelectionType.SingleNode);
			return removedJobs.Count > 0;
		}

		public override IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			var result = new List<JobTreeNode>();

			switch (nodeSelectionType)
			{
				case NodeSelectionType.SingleNode:
					break;
				case NodeSelectionType.Preceeding:
					break;
				case NodeSelectionType.Children:
					break;
				case NodeSelectionType.Siblings:
					break;
				case NodeSelectionType.Branch:
					_ = RemoveNode(path);
					break;
				case NodeSelectionType.ContainingBranch:
					throw new NotImplementedException();
				default:
					break;
			}

			return result;
		}

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
			var grandparentBranch = parentPath.GetParentBranch();
			var grandparentNode = parentPath.GetParentNodeOrRoot();
			Debug.WriteLine($"Adding job: {job.Id}, a sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
			var newPath = AddItem(job, grandparentBranch);

			return newPath;
		}

		#endregion

		#region Private Navigate Methods

		protected override void UpdateIsSelected(JobTreeNode? node, bool isSelected, JobBranchType startPos)
		{
			if (node == null)
			{
				return;
			}

			node.IsSelected = isSelected;

			var path = startPos.CreatePath(node);
			var backPath = GetPreviousItemPath(path, GetPredicate(skipPanJobs: true));

			if (backPath != null)
			{
				backPath.Node.IsParentOfSelected = isSelected;
			}

			var prevPath = GetPreviousItemPath(path, null);

			while (prevPath != null && !DoesNodeChangeZoom(prevPath.Node))
			{
				_ = UpdateIsSiblingAndIsChildOfSelected(prevPath.Node, isSelected);
				prevPath = GetPreviousItemPath(prevPath, null);
			}

			var nextPath = GetNextItemPath(path, null);

			while (nextPath != null && !DoesNodeChangeZoom(nextPath.Node))
			{
				_ = UpdateIsSiblingAndIsChildOfSelected(nextPath.Node, isSelected);
				nextPath = GetNextItemPath(nextPath, null);
			}

			if (nextPath != null)
			{
				nextPath.Node.IsChildOfSelected = isSelected;
			}
		}

		private bool UpdateIsSiblingAndIsChildOfSelected(JobTreeNode node, bool isSelected)
		{
			node.IsSiblingOfSelected = isSelected;

			var result = DoesNodeChangeZoom(node);

			if (!DoesNodeChangeZoom(node))
			{
				foreach (var c in node.Children.Where(x => DoesNodeChangeZoom(x)))
				{
					c.IsChildOfSelected = isSelected;
				}
			}

			return result;
		}

		#endregion
	}
}

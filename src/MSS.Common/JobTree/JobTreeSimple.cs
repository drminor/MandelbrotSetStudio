using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
				Debug.WriteLine("MakePreferred is clearing all nodes.");
				Root.GetNodeOrRoot().PreferredChild = null;
			}
			else
			{
				var visitedNodeIds = new List<ObjectId>();
				var parentChildPairs = GetAncestorParentChildPairs(path);
				SetPreferredChildNodes(parentChildPairs, visitedNodeIds, ref numberSet, ref numberReset);

				//_ = sb.AppendLine("Now selecting the child's last child as the preferred child.");

				parentChildPairs = GetDescendantPCPairsLast(path);
				SetPreferredChildNodes(parentChildPairs, visitedNodeIds, ref numberSet, ref numberReset);
			}

			//_ = sb.AppendLine($"Setting path: {path?.Node.Id} to be the preferred path. Set {numberSet} nodes, reset {numberReset}.");
			//_ = sb.AppendLine("*****");

			//Debug.WriteLine(sb.ToString());

			return true;
		}

		public override IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			IList<JobTreeNode> result;

			switch (nodeSelectionType)
			{
				case NodeSelectionType.SingleNode:
					result = new List<JobTreeNode>(); break;

				case NodeSelectionType.Preceeding:
					result = new List<JobTreeNode>(); break;

				case NodeSelectionType.Children:
					result = new List<JobTreeNode>(); break;

				case NodeSelectionType.Siblings:
					result = new List<JobTreeNode>(); break;

				case NodeSelectionType.Branch:
					result = RemoveJobsBranch(path); break;

				case NodeSelectionType.ContainingBranch:
					throw new NotImplementedException();

				default:
					result = new List<JobTreeNode>(); break;
			}

			return result;
		}

		private IList<JobTreeNode> RemoveJobsBranch(JobPathType path)
		{
			var startingNode = path.Node;
			var parentPath = path.GetParentPath();

			if (parentPath == null || parentPath.Node.RealChildJobs.Count < 1)
			{
				throw new InvalidOperationException($"Expecting path to have siblings.");
			}

			SelectedNode = GetNewSelectedNode(path);

			var parentNode = parentPath.Node;
			var siblings = parentNode.RealChildJobs;
			if (siblings.Count == 2 && !DoesNodeChangeZoom(parentNode))
			{
				// Once the node at path is removed, the parent will have only a single child, 
				// move this sole, remaining child so that it is a child of the path's grandparent.
				var grandparentNode = parentPath.GetParentNodeOrRoot();
				_ = SelectedNode.Move(grandparentNode);
			}

			if (startingNode.IsOnPreferredPath)
			{
				_ = MakePreferred(SelectedNode!.Id);
			}

			// Get a list of parentChildPairs for each job that will be removed.
			var parentChildPairs = GetDescendantPCPairsAll(path);

			if (startingNode.IsCurrent || parentChildPairs.Any(x => x.child.IsCurrent))
			{
				CurrentItem = SelectedNode.Item; // _ = MoveCurrentTo(SelectedNode.Item, Root, out var _);
			}

			// Remove each child, unless its parent is in the list of nodes to be removed.
			var childIds = parentChildPairs.Select(x => x.child.Id).ToList();

			foreach (var (parent, child) in parentChildPairs)
			{
				if (parent.Id != startingNode.Id && !childIds.Contains(parent.Id))
				{
					_ = parent.Remove(child);
					_ = parent.RemoveRealChild(child.Item);
				}
			}

			Debug.Assert(!parentChildPairs.Any(x => x.child.Id == startingNode.Id), "Node already deleted.");

			_ = parentNode.Remove(startingNode);
			_ = parentNode.RemoveRealChild(startingNode.Item);

			var result = parentChildPairs.Select(x => x.child).ToList();
			return result;
		}

		// Find the node that will receive the selection once the node at path is removed.
		private JobTreeNode GetNewSelectedNode(JobPathType path)
		{
			JobTreeNode result;

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.RealChildJobs;

			if (siblings.Count == 1)
			{
				var backPath = GetPreviousItemPath(path, predicate: null);
				result = backPath != null ? backPath.Node : throw new InvalidOperationException("Cannot find a previous item.");
			}
			else
			{
				var jobToReceiveSelection = siblings.LastOrDefault(x => x.Key != path.Node.Id).Value;
				result = parentNode.Children.First(x => x.Id == jobToReceiveSelection.Id);
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
				UpdateIsSiblingAndIsChildOfSelected(prevPath.Node, isSelected);
				prevPath = GetPreviousItemPath(prevPath, null);
			}

			var nextPath = GetNextItemPath(path, null);

			while (nextPath != null && !DoesNodeChangeZoom(nextPath.Node))
			{
				UpdateIsSiblingAndIsChildOfSelected(nextPath.Node, isSelected);
				nextPath = GetNextItemPath(nextPath, null);
			}

			if (nextPath != null)
			{
				nextPath.Node.IsChildOfSelected = isSelected;
			}
		}

		private void UpdateIsSiblingAndIsChildOfSelected(JobTreeNode node, bool isSelected)
		{
			node.IsSiblingOfSelected = isSelected;

			if (!DoesNodeChangeZoom(node))
			{
				var childNodesThatChangeZoom = node.Children.Where(x => DoesNodeChangeZoom(x));

				foreach (var c in childNodesThatChangeZoom)
				{
					c.IsChildOfSelected = isSelected;
				}
			}
		}

		#endregion

		#region Protected GetNode Methods

		private List<(JobTreeNode parent, JobTreeNode child)> GetAncestorParentChildPairs(JobPathType path)
		{
			// Assemble a list of parents and their preferred child for each term
			// of a path constructed using the specified path's "real" parent.

			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			var parentNode = path.GetParentNodeOrRoot();
			parentChildPairs.Add((parentNode, path.Node));

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

			parentChildPairs.Reverse();

			return parentChildPairs;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetDescendantPCPairsLast(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			//	For each child of the last term, set the last child to be the preferred child.
			var nextChildJob = path.Node.RealChildJobs.LastOrDefault().Value;

			while (nextChildJob != null)
			{
				var nextChildPath = GetPath(nextChildJob, Root);
				var child = nextChildPath.Node;
				var parentNode = nextChildPath.GetParentNodeOrRoot();
				parentChildPairs.Add((parentNode, child));

				nextChildJob = nextChildPath.Node.RealChildJobs.LastOrDefault().Value;
			}

			return parentChildPairs;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetDescendantPCPairsAll(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			foreach (var nextChildJob in path.Node.RealChildJobs.Values)
			{
				var nextChildPath = GetPath(nextChildJob, Root);
				parentChildPairs.Add((nextChildPath.GetParentNodeOrRoot(), nextChildPath.Node));

				parentChildPairs.AddRange(GetDescendantPCPairsAll(nextChildPath));
			}

			return parentChildPairs;
		}

		private void SetPreferredChildNodes(List<(JobTreeNode parent, JobTreeNode child)> parentChildPairs, List<ObjectId> vistedNodeIds, ref int numberSet, ref int numberReset)
		{
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
		}

		//protected override IList<Job> GetItems(JobBranchType currentBranch)
		//{
		//	var result = GetNodes(currentBranch).Select(x => x.Item).ToList();
		//	return result;
		//}

		//protected override IList<JobTreeNode> GetNodes(JobBranchType currentBranch)
		//{
		//	var result = new List<JobTreeNode>();
		//	var misses = 0;

		//	var currentPath = currentBranch.GetCurrentPath();

		//	if (currentPath == null)
		//	{
		//		foreach(var child in currentBranch.Children)
		//		{
		//			result.Add(child);
		//			var childPath = currentBranch.CreatePath(child);
		//			result.AddRange(GetNodes(childPath, ref misses));
		//		}
		//	}
		//	else
		//	{
		//		result = GetNodes(currentPath, ref misses);
		//	}

		//	var hitRate = (result.Count - misses) / (double)result.Count;

		//	Debug.WriteLine($"GetNodes for branch: {currentBranch} found {result.Count} nodes with a hit rate of {hitRate}.");

		//	return result;
		//}

		//private List<JobTreeNode> GetNodes(JobPathType path, ref int misses)
		//{
		//	var result = new List<JobTreeNode>();

		//	var hasSiblings = path.GetParentNodeOrRoot().RealChildJobs.Count > 0;
		//	var startNode = path.Node;

		//	//var parentBranch = (JobBranchType) path;
		//	var parentBranch = path.GetParentBranch();
		//	//if (hasSiblings || DoesNodeChangeZoom(startNode))
		//	//{
		//	//	//parentBranch = path.GetParentBranch();
		//	//	parentBranch = parentBranch.GetParentBranch();
		//	//}

		//	var children = parentBranch.Children;
		//	var childPtr = 0;
		//	var node = children[childPtr];

		//	var rcjs = path.Node.RealChildJobs;
		//	foreach (var job in rcjs)
		//	{
		//		if (node.Id == job.Key)
		//		{
		//			result.Add(node);
		//			node = children[++childPtr];

		//			var nodeList = GetNodes(path.Combine(node), ref misses);
		//			result.AddRange(nodeList);
		//		}
		//		else
		//		{
		//			misses++;
		//			var childPath = GetPath(job.Value, path);
		//			result.Add(childPath.Node);

		//			var nodeList = GetNodes(childPath, ref misses);
		//			result.AddRange(nodeList);
		//		}
		//	}

		//	return result;
		//}

		//protected override List<Tuple<JobTreeNode, JobTreeNode?>> GetNodesWithParentage(JobBranchType currentBranch)
		//{
		//	var result = GetNodes(currentBranch).Select(x => new Tuple<JobTreeNode, JobTreeNode?>(x, x.ParentNode)).ToList();
		//	return result;
		//}

		#endregion
	}
}

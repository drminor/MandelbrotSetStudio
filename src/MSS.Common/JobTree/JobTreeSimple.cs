using MongoDB.Bson;
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
				var ancendantPairs = GetNodeAndAncestorPairsAll(path);
				SetPreferredChildNodes(ancendantPairs, visitedNodeIds, ref numberSet, ref numberReset);

				//_ = sb.AppendLine("Now selecting the child's last child as the preferred child.");

				var descendantPairs = GetNodeAndDescendantPairsLast(path).Skip(1).ToList();
				SetPreferredChildNodes(descendantPairs, visitedNodeIds, ref numberSet, ref numberReset);
			}

			//_ = sb.AppendLine($"Setting path: {path?.Node.Id} to be the preferred path. Set {numberSet} nodes, reset {numberReset}.");
			//_ = sb.AppendLine("*****");

			//Debug.WriteLine(sb.ToString());

			return true;
		}

		public override IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			Debug.WriteLine($"Remove Jobs: Starting for path: {path.Node.Id}. SelectionType: {nodeSelectionType}.");
			var jobsToRemove = GetJobsToRemove(path, nodeSelectionType);

			if (jobsToRemove.Count == 0)
			{
				Debug.WriteLine("Remove Jobs: found no jobs to remove.");
				return new List<JobTreeNode>();
			}

			// Update the selected node, if any being removed in selected.
			//var selectedNode = SelectedNode;

			//if (jobsToRemove.Any(x => x.child.IsSelected) || selectedNode == null)
			//{
			//	selectedNode = GetNewSelectedNode(path);
			//	SelectedNode = selectedNode;
			//}

			var saveSelectedNode = SelectedNode;
			Debug.WriteLine("Remove Jobs: is setting the Selected Node to null.");
			SelectedNode = null;

			//if (jobsToRemove.Any(x => x.child.IsCurrent))
			//{
			//	var newCurrentNode = GetNewCurrentNode(path);
			//	Debug.WriteLine($"Remove Jobs is updating the Current Item using node: {SelectedNode}.");
			//	CurrentItem = newCurrentNode.Item;
			//}

			var saveCurrentItem = CurrentItem;
			var newCurrentNode = GetNewCurrentNode(path);
			Debug.WriteLine($"Remove Jobs is updating the Current Item to {newCurrentNode.Item.Id}.");
			CurrentItem = newCurrentNode.Item;

			var topLevelPairs = RemoveJobs(path, jobsToRemove);

			if (jobsToRemove.Any(x => x.child.IsOnPreferredPath))
			{
				Debug.WriteLine($"Remove Jobs is updating the preferred path using node: {newCurrentNode.Item.Id}.");
				_ = MakePreferred(newCurrentNode.Item.Id);
			}

			if (TryFindPath(saveCurrentItem, Root, out var currentPath))
			{
				CurrentItem = currentPath.Node.Item;
			}

			if (saveSelectedNode == null)
			{
				saveSelectedNode = newCurrentNode;
			}

			if (TryFindPath(saveSelectedNode.Item, Root, out var selectedPath))
			{
				SelectedNode = selectedPath.Node;
			}
			else
			{
				SelectedNode = newCurrentNode;
			}

			//Debug.WriteLine($"Remove Jobs: is moving the Selected Node to {SelectedNode}.");

			ReportTopLevelJobsRemoved(topLevelPairs);

			var result = jobsToRemove.Select(x => x.child).ToList();
			IsDirty = true;
			return result;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetJobsToRemove(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			var result = nodeSelectionType switch
			{
				NodeSelectionType.SingleNode => GetSingleParentChildPair(path),                                         // 1

				NodeSelectionType.Preceeding => GetNodeAndAncestorPairsRun(path).SkipLast(1).ToList(),                  // 2

				NodeSelectionType.SinglePlusPreceeding => GetNodeAndAncestorPairsRun(path),                             // 1 | 2 (3)

				NodeSelectionType.Following => GetNodeAndDescendantPairsRun(path).Skip(1).ToList(),                     // 4

				NodeSelectionType.SinglePlusFollowing => GetNodeAndDescendantPairsRun(path),                            // 1 | 4 (5)

				NodeSelectionType.Run => GetNodeAndDescendantPairsRun(GetBranchHead(path)),                             // 1 | 2 | 4 (7)

				NodeSelectionType.Children => GetDescendantPairsAll(GetBranchHead(path)),                               // 8

				NodeSelectionType.Branch => GetNodeAndDescendantPairsAll(GetBranchHead(path)),                          // 1 | 8 (9)

				NodeSelectionType.SiblingBranches => GetDescendantPairsAllForOtherSiblingBranches(GetBranchHead(path)), // 16

				_ => throw new NotImplementedException(),
			};

			return result;
		}

		#endregion

		#region Private Add Item Methods

		protected override JobPathType AddAtParentPath(Job job, JobPathType parentPath)
		{
			var parentNode = parentPath.Node;

			JobPathType newPath;

			if (parentNode.RealChildJobs.Count > 0)
			{
				var existingJob = parentNode.RealChildJobs.Values[0];

				if (!parentNode.Children.Any(x => x.Id == existingJob.Id))
				{
					if (TryFindPath(existingJob, Root, out var existingJobpath))
					{
						Debug.WriteLine($"Moving job: {existingJob.Id}, to be a child of {parentNode.Id}.");
						_ = existingJobpath.Node.Move(parentNode);
					}
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

		#region Private Add Node Methods

		private JobPathType AddNodeAtParentPath(JobTreeNode node, JobTreeNode parentNode, JobBranchType parentBranch)
		{
			//var parentNode = parentBranch.Node;

			JobPathType newPath;

			if (parentNode.RealChildJobs.Count > 0)
			{
				var existingJob = parentNode.RealChildJobs.Values[0];

				if (!parentNode.Children.Any(x => x.Id == existingJob.Id))
				{
					if (TryFindPath(existingJob, Root, out var existingJobpath))
					{
						Debug.WriteLine($"Moving job: {existingJob.Id}, to be a child of {parentNode.Id}.");
						_ = existingJobpath.Node.Move(parentNode);
					}
				}

				//var parentNode = parentBranch.GetNodeOrRoot();
				Debug.WriteLine($"Adding JobTreeNode: {node.Id}, as a child of {parentNode.Id}.");
				newPath = AddNode(node, parentNode, parentBranch);
			}
			else if (DoesNodeChangeZoom(parentNode))
			{
				Debug.WriteLine($"Adding JobTreeNode: {node.Id}, as a child of {parentNode.Id}. The parent's TransformType is {parentNode.TransformType}.");
				newPath = AddNode(node, parentNode, parentBranch);
			}
			else
			{
				//Debug.WriteLine($"Adding job: {job.Id}, in-line after {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
				var parentPath = parentBranch.GetCurrentPath();

				if (parentPath == null)
				{
					throw new InvalidOperationException("Wnen adding in-line, the parentBranch should have a current path.");
				}

				newPath = AddNodeInLine(node, parentPath);
			}

			_ = parentNode.AddRealChild(node.Item);

			return newPath;
		}

		private JobPathType AddNodeInLine(JobTreeNode node, JobPathType parentPath)
		{
			var grandparentBranch = parentPath.GetParentBranch();
			var grandparentNode = parentPath.GetParentNodeOrRoot();
			Debug.WriteLine($"Adding JobTreeNode: {node.Id}, a sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");

			var newPath = AddNode(node, grandparentNode, grandparentBranch);
			return newPath;
		}

		private JobPathType AddNode(JobTreeNode node, JobTreeNode parentNode, JobBranchType parentBranch)
		{
			parentNode.AddNode(node);
			var newPath = parentBranch.Combine(node);

			return newPath;
		}

		#endregion

		#region Private Navigate Methods

		protected override void UpdateIsSelected(JobTreeNode? node, bool isSelected, JobBranchType startPos)
		{
			if (node == null)
			{
				Debug.WriteLine($"UpdateIsSelected, value = null, no action taken.");
				return;
			}

			try
			{
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
			catch (Exception e)
			{
				Debug.WriteLine($"UpdateIsSelected received exception: {e} while attempting to set IsSelected to {isSelected} for node: {node}");
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

		private List<(JobTreeNode parent, JobTreeNode child)> GetSingleParentChildPair(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>
			{
				(path.GetParentNodeOrRoot(), path.Node)
			};

			return parentChildPairs;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetNodeAndAncestorPairsAll(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>
			{
				(path.GetParentNodeOrRoot(), path.Node)
			};

			var previousPath = path;
			var nextPath = GetParentPath(previousPath.Item, Root); // Using the real parent Id here. // TODO: Tighten up the Call to GetParentPath

			while (nextPath != null)
			{
				parentChildPairs.Add((nextPath.GetParentNodeOrRoot(), nextPath.Node));

				previousPath = nextPath;
				nextPath = GetParentPath(previousPath.Item, Root); // TODO: Tighten up the Call to GetParentPath
			}

			// Set the preferred child of the root node using the very first term of the given path.
			parentChildPairs.Add((previousPath.GetParentNodeOrRoot(), previousPath.Node));

			parentChildPairs.Reverse();

			return parentChildPairs;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetNodeAndAncestorPairsRun(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>
			{
				(path.GetParentNodeOrRoot(), path.Node)
			};

			var previousPath = path;
			var nextPath = GetParentPath(previousPath.Item, Root); // Using the real parent Id here.

			while (nextPath != null && !IsBranchHead(nextPath.Node))
			{
				parentChildPairs.Add((nextPath.GetParentNodeOrRoot(), nextPath.Node));

				previousPath = nextPath;
				nextPath = GetParentPath(previousPath.Item, Root);
			}

			if (nextPath == null && !IsBranchHead(previousPath.Node))
			{
				// Set the preferred child of the root node using the very first term of the given path.
				parentChildPairs.Add((previousPath.GetParentNodeOrRoot(), previousPath.Node));
			}

			parentChildPairs.Reverse();

			return parentChildPairs;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetNodeAndDescendantPairsLast(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			//	For each child of the last term, set the last child to be the preferred child.
			var nextChildJob = path.Node.RealChildJobs.LastOrDefault().Value;

			while (nextChildJob != null)
			{
				var nextChildPath = GetPath(nextChildJob, Root);
				parentChildPairs.Add((nextChildPath.GetParentNodeOrRoot(), nextChildPath.Node));

				nextChildJob = nextChildPath.Node.RealChildJobs.LastOrDefault().Value;
			}

			var result = new List<(JobTreeNode parent, JobTreeNode child)>
			{
				(path.GetParentNodeOrRoot(), path.Node)
			};

			result.AddRange(parentChildPairs);

			return result;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetNodeAndDescendantPairsAll(JobPathType path)
		{
			var result = GetSingleParentChildPair(path);
			result.AddRange(GetDescendantPairsAll(path));
			return result;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetDescendantPairsAll(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			foreach (var nextChildJob in path.Node.RealChildJobs.Values)
			{
				var nextChildPath = GetPath(nextChildJob, Root);
				parentChildPairs.Add((nextChildPath.GetParentNodeOrRoot(), nextChildPath.Node));

				parentChildPairs.AddRange(GetDescendantPairsAll(nextChildPath));
			}

			return parentChildPairs;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetNodeAndDescendantPairsRun(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>
			{
				(path.GetParentNodeOrRoot(), path.Node)
			};

			var previousPath = path;
			var nextPath = GetNextItemPath(previousPath, predicate: x => !IsBranchHead(x)); 

			while (nextPath != null)
			{
				parentChildPairs.Add((nextPath.GetParentNodeOrRoot(), nextPath.Node));

				previousPath = nextPath;
				nextPath = GetNextItemPath(previousPath, predicate: x => !IsBranchHead(x));
			}

			return parentChildPairs;
		}

		private List<(JobTreeNode parent, JobTreeNode child)> GetDescendantPairsAllForOtherSiblingBranches(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			var node = path.Node;
			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.RealChildJobs;

			foreach(var siblingJob in siblings)
			{
				if (siblingJob.Value.Id == node.Id)
				{
					continue;
				}

				var siblingPath = GetPath(siblingJob.Value.Id);
				if (siblingPath != null)
				{
					parentChildPairs.AddRange(GetNodeAndDescendantPairsAll(siblingPath));
				}
				else
				{
					throw new InvalidOperationException($"Cannot get path of RealChildJob: {siblingJob.Value.Id}.");
				}
			}

			return parentChildPairs;
		}

		//protected override IList<Job> GetItems(JobBranchType currentBranch)
		//{
		//	var result = GetNodes(currentBranch).Select(x => x.Item).ToList();
		//	return result;
		//}

		//protected override IList<JobTreeNode> GetNodes(JobBranchType currentBranch)
		//{
		//	var result = GetNodesWithParentage(currentBranch).Select(x => x.child).ToList();
		//	return result;
		//}

		//protected override List<(JobTreeNode? parent, JobTreeNode child)> GetNodesWithParentage(JobBranchType branch)
		//{
		//	List<(JobTreeNode? parent, JobTreeNode child)> result;

		//	var path = branch.GetCurrentPath();

		//	if (path != null)
		//	{
		//		result = GetDescendantPairsAll(path).Select(x => ((JobTreeNode?) x.parent, x.child)).ToList();
		//	}
		//	else
		//	{
		//		result = new List<(JobTreeNode? parent, JobTreeNode child)>();
		//		foreach(var child in branch.Children)
		//		{
		//			var childPath = branch.CreatePath(new JobTreeNode[] { child });
		//			result.AddRange(GetDescendantPairsAll(childPath).Select(x => ((JobTreeNode?)x.parent, x.child)).ToList());
		//		}
		//	}

		//	return result;
		//}

		#endregion

		#region Private Branch Methods

		private JobPathType GetBranchHead(JobPathType path)
		{
			var resultPath = path;
			while (!IsBranchHead(resultPath.Node))
			{
				resultPath = GetParentPath(resultPath.Item, Root); // Using the real parent Id here.
				if (resultPath == null)
				{
					throw new InvalidOperationException();
				}
			}

			return resultPath;
		}

		private bool IsBranchHead(JobTreeNode node)
		{
			var realChildCount = node.RealChildJobs.Count;
			var changesZoom = DoesNodeChangeZoom(node);
			var isHome = node.IsHome;

			var result = isHome || realChildCount > 1 || changesZoom;
			return result;
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

		private List<(JobTreeNode parent, JobTreeNode child)> RemoveJobs(JobPathType path, List<(JobTreeNode parent, JobTreeNode child)> parentChildPairs)
		{
			var topLevelPairs = new List<(JobTreeNode parent, JobTreeNode child)>();
			var pairsToBeOrphaned = new List<(JobTreeNode parent, JobTreeNode child)>();

			var childIds = parentChildPairs.Select(x => x.child.Id).ToList();
			foreach (var (parent, child) in parentChildPairs)
			{
				// Find all of the parents of nodes being removed
				// that are not also being removed.
				if (!childIds.Contains(parent.Id))
				{
					topLevelPairs.Add((parent, child));
				}

				// Find all of the childern of the children being removed
				// that are not also being removed.
				foreach(var grandChild in child.Children)
				{
					if (!childIds.Contains(grandChild.Id))
					{
						pairsToBeOrphaned.Add((child, grandChild));
					}
				}
			}

			// Remove the nodes that will become orphaned.
			foreach (var (child, grandchild) in pairsToBeOrphaned)
			{
				Debug.WriteLine($"Remove Jobs: is removing node to be orphaned: {grandchild.Id} from it's parent: {child.Id}.");
				_ = RemoveNode(grandchild, child);
			}

			foreach (var (parent, child) in topLevelPairs)
			{
				Debug.WriteLine($"Remove Jobs: removing child: {child.Id} from parent: {parent.Id}.");
				_ = RemoveNode(child, parent);

				var siblings = parent.RealChildJobs;
				if (child.ParentJobId == parent.Id && siblings.Count > 0)
				{
					var soleChildJob = siblings.Values[0];

					if (siblings.Count == 1 && !DoesItemChangeZoom(soleChildJob))
					{
						// The parent no longer has child "choices" or "exits"; 
						// move this sole, remaining child so that it is a child of the parent's parent.

						var parentPath = path.CreatePath(parent);
						var grandparentNode = parentPath.GetParentNodeOrRoot();

						Debug.WriteLine($"Remove Jobs: having removed child: {child.Id}, it's real parent: {child.ParentId} now has but one real child: {soleChildJob.Id}.");
						Debug.WriteLine($"Remove Jobs: moving the logical children of parent: {parent.Id}.");
						
						var nodesToMove = new List<JobTreeNode>(parent.Children);
						foreach(var logicalChildNode in nodesToMove)
						{
							Debug.WriteLine($"Remove Jobs: moving logical child: {logicalChildNode.Id} to the grandparent node: {grandparentNode.Id}.");
							_ = logicalChildNode.Move(grandparentNode);
						}

						//_ = firstChildNode.Move(grandparentNode);
					}
				}
			}

			// Re-add the orphaned nodes.
			var firstParentNode = parentChildPairs[0].parent;
			var firstParentPath = path.CreatePath(firstParentNode);

			foreach (var (child, grandchild) in pairsToBeOrphaned)
			{
				Debug.WriteLine($"Remove Jobs: is adding the orphaned node: {grandchild.Id} to {firstParentNode.Id}.");
				grandchild.Item.ParentJobId = firstParentNode.Id;
				_ = AddNodeAtParentPath(grandchild, firstParentNode, firstParentPath);
			}

			return topLevelPairs;
		}

		private bool RemoveNode(JobTreeNode child, JobTreeNode parent)
		{
			if (child.ParentJobId != parent.Id)
			{
				var realPath = GetPath(child.Item, Root);
				var realParentNode = realPath.GetParentNodeOrRoot();
				if (!realParentNode.RemoveRealChild(child.Item))
				{
					Debug.WriteLine($"WARNING: could not remove RealChildJob: {child.Id} from real parent: {realParentNode.Id}.");
				}
			}
			else
			{
				if (!parent.RemoveRealChild(child.Item))
				{
					Debug.WriteLine($"WARNING: could not remove RealChildJob: {child.Id} from parent: {parent.Id}.");
				}
			}

			var result = parent.Remove(child);
			return result;
		}

		// Find the node that will receive the selection once the node at path is removed.
		private JobTreeNode GetNewCurrentNode(JobPathType path)
		{
			JobTreeNode result;

			//var parentNode = path.GetParentNodeOrRoot();
			//var siblings = parentNode.RealChildJobs;

			//if (siblings.Count < 2)
			//{
			//	var backPath = GetPreviousItemPath(path, predicate: null);
			//	result = backPath != null ? backPath.Node : throw new InvalidOperationException("Cannot find a previous item.");
			//}
			//else
			//{
			//	var jobToReceiveSelection = siblings.LastOrDefault(x => x.Key != path.Node.Id).Value;
			//	result = parentNode.Children.First(x => x.Id == jobToReceiveSelection.Id);
			//}

			var backPath = GetPreviousItemPath(path, predicate: null);
			result = backPath != null ? backPath.Node : throw new InvalidOperationException("Cannot find a previous item.");

			return result;
		}

		[Conditional("DEBUG")]
		private void ReportTopLevelJobsRemoved(List<(JobTreeNode parent, JobTreeNode child)> topLevelPairs)
		{
			Debug.WriteLine($"Remove Jobs: removed these top-level pairs:");
			foreach (var (parent, child) in topLevelPairs)
			{
				Debug.WriteLine($"\tChild: {child.Id}, Parent: {parent.Id}");
			}
		}

		#endregion
	}
}

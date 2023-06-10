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

		private bool _useDetailedDebug = false;

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
				Debug.WriteLineIf(_useDetailedDebug, "MakePreferred is clearing all nodes.");
				Root.GetNodeOrRoot().PreferredChild = null;
			}
			else
			{
				var visitedNodeIds = new List<ObjectId>();
				var ancendantPairs = GetNodeAndAncestorPairsAll(path);
				SetPreferredChildNodes(ancendantPairs, visitedNodeIds, ref numberSet, ref numberReset);

				//_ = sb.AppendLine("Now selecting the child's last child as the preferred child.");

				var descendantPairs = GetDescendantPairsLast(path);
				SetPreferredChildNodes(descendantPairs, visitedNodeIds, ref numberSet, ref numberReset);
			}

			//_ = sb.AppendLine($"Setting path: {path?.Node.Id} to be the preferred path. Set {numberSet} nodes, reset {numberReset}.");
			//_ = sb.AppendLine("*****");

			//Debug.WriteLine(sb.ToString());

			return true;
		}

		public override IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: Starting for path: {path.Node.Id}. SelectionType: {nodeSelectionType}.");
			var jobsToRemove = GetJobsToRemove(path, nodeSelectionType);

			if (jobsToRemove.Count == 0)
			{
				Debug.WriteLineIf(_useDetailedDebug, "Remove Jobs: found no jobs to remove.");
				return new List<JobTreeNode>();
			}

			var saveSelectedNode = SelectedNode;
			Debug.WriteLineIf(_useDetailedDebug, "Remove Jobs: is setting the Selected Node to null.");
			SelectedNode = null;

			//var newCurrentNode = GetNewCurrentNode(path);
			var newPath = GetParentPath(jobsToRemove[0].child.Item, Root);
			var saveCurrentItem = CurrentItem;

			//if (newCurrentNode != null)
			//{
			//	CurrentItem = newCurrentNode.Item;
			//}

			if (newPath != null)
			{
				CurrentItem = newPath.Item;
			}

			var topLevelPairs = RemoveJobs(path, jobsToRemove);

			// Reset the Current Job
			//var newCurrentItem = TryFindPath(saveCurrentItem, Root, out var newPath) ? newPath.Item : newCurrentNode?.Item ?? Root.Children[0].Item;
			var newCurrentItem = TryFindPath(saveCurrentItem, Root, out var savedItemPath) ? savedItemPath.Item : newPath?.Item ?? Root.Children[0].Item;
			Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs is updating the Current Item to {newCurrentItem.Id}.");
			CurrentItem = newCurrentItem;

			// Reset the Selected Job
			if (saveSelectedNode != null)
			{
				SelectedNode = TryFindPath(saveSelectedNode.Item, Root, out var selectedPath) ? selectedPath.Node : newPath?.Node;
				Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: moved the Selected Node to {SelectedNode}.");
			}

			// Restore or set the Preferred Path
			if (jobsToRemove.Any(x => x.child.IsOnPreferredPath))
			{
				//if (nodeSelectionType is not (NodeSelectionType.Children or NodeSelectionType.Branch or NodeSelectionType.SiblingBranches))
				//{
				//	// TODO: Find the last (deepest) descendant of the newCurrentNode whose IsOnPreferredPath property = true.
				//}
				//else
				//{
				//	var newPreferredItemId = newCurrentNode?.Item.Id ?? Root.Children[0].Item.Id;
				//	Debug.WriteLine($"Remove Jobs is updating the preferred path using node: {newPreferredItemId}.");
				//	_ = MakePreferred(newPreferredItemId);
				//}

				var newPreferredItemId = newPath?.Item.Id ?? Root.Children[0].Item.Id;
				Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs is updating the preferred path using node: {newPreferredItemId}.");
				_ = MakePreferred(newPreferredItemId);
			}

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

				NodeSelectionType.Ancestors => GetNodeAndAncestorPairsAll(path),										// 16

				NodeSelectionType.SiblingBranches => GetDescendantPairsAllForOtherSiblingBranches(GetBranchHead(path)), // 32

				NodeSelectionType.AllButPreferred => GetAllNonPreferredNodes(),											// 64

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
						Debug.WriteLineIf(_useDetailedDebug, $"Moving job: {existingJob.Id}, to be a child of {parentNode.Id}.");
						_ = existingJobpath.Node.Move(parentNode);
					}
				}

				Debug.WriteLineIf(_useDetailedDebug, $"Adding job: {job.Id}, as a child of {parentPath.Node.Id}.");
				newPath = AddItem(job, parentPath);
			}
			else if (DoesNodeChangeZoom(parentNode))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Adding job: {job.Id}, as a child of {parentPath.Node.Id}. The parent's TransformType is {parentPath.Node.TransformType}.");
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
			Debug.WriteLineIf(_useDetailedDebug, $"Adding job: {job.Id}, a sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");
			var newPath = AddItem(job, grandparentBranch);

			return newPath;
		}

		#endregion

		#region Private Add Node Methods

		private JobPathType AddNodeAtParentPath(JobTreeNode node, JobTreeNode parentNode, JobBranchType parentBranch)
		{
			JobPathType newPath;

			if (parentNode.RealChildJobs.Count > 0)
			{
				var existingJob = parentNode.RealChildJobs.Values[0];

				if (!parentNode.Children.Any(x => x.Id == existingJob.Id))
				{
					if (TryFindPath(existingJob, Root, out var existingJobpath))
					{
						Debug.WriteLineIf(_useDetailedDebug, $"Moving job: {existingJob.Id}, to be a child of {parentNode.Id}.");
						_ = existingJobpath.Node.Move(parentNode);
					}
				}

				//var parentNode = parentBranch.GetNodeOrRoot();
				Debug.WriteLineIf(_useDetailedDebug, $"Adding JobTreeNode: {node.Id}, as a child of {parentNode.Id}.");
				newPath = AddNode(node, parentNode, parentBranch);
			}
			else if (DoesNodeChangeZoom(parentNode))
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Adding JobTreeNode: {node.Id}, as a child of {parentNode.Id}. The parent's TransformType is {parentNode.TransformType}.");
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
			Debug.WriteLineIf(_useDetailedDebug, $"Adding JobTreeNode: {node.Id}, a sibling to its parent: {parentPath.Node.Id} as a child of {grandparentNode.Id}.");

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
				Debug.WriteLineIf(_useDetailedDebug, $"UpdateIsSelected, value = null, no action taken.");
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
				Debug.WriteLineIf(_useDetailedDebug, $"UpdateIsSelected received exception: {e} while attempting to set IsSelected to {isSelected} for node: {node}");
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

			//if (nextPath == null && !IsBranchHead(previousPath.Node))
			//{
			//	// Set the preferred child of the root node using the very first term of the given path.
			//	parentChildPairs.Add((previousPath.GetParentNodeOrRoot(), previousPath.Node));
			//}

			parentChildPairs.Reverse();

			return parentChildPairs;
		}

		//private List<(JobTreeNode parent, JobTreeNode child)> GetNodeAndDescendantPairsLast(JobPathType path)
		//{
		//	var result = new List<(JobTreeNode parent, JobTreeNode child)>
		//	{
		//		(path.GetParentNodeOrRoot(), path.Node)
		//	};

		//	result.AddRange(GetDescendantPairsLast(path));

		//	return result;
		//}

		private List<(JobTreeNode parent, JobTreeNode child)> GetDescendantPairsLast(JobPathType path)
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

			return parentChildPairs;
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

		private List<(JobTreeNode parent, JobTreeNode child)> GetAllNonPreferredNodes() 
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			var nodes = GetNodes(Root);
			var preferredNodes = nodes.Where(x => x.IsOnPreferredPath).ToList();

			foreach(var node in preferredNodes)
			{
				parentChildPairs.Add(new(node.ParentNode ?? Root.GetNodeOrRoot(), node));
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
					//var strResetingChildNodes = !alreadyVisited ? ", and reseting child nodes." : ".";
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
				Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: is removing node to be orphaned: {grandchild.Id} from it's parent: {child.Id}.");
				_ = RemoveNode(grandchild, child);
			}

			foreach (var (parent, child) in topLevelPairs)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: removing child: {child.Id} from parent: {parent.Id}.");
				_ = RemoveNode(child, parent);

				var siblings = parent.RealChildJobs;
				if (child.ParentJobId == parent.Id && siblings.Count == 1)
				{
					var soleChildJob = siblings.Values[0];

					if (!DoesItemChangeZoom(soleChildJob))
					{
						// The parent no longer has child "choices" or "exits"; 
						// move this sole, remaining child so that it is a child of the parent's parent.

						var parentPath = path.CreatePath(parent);
						var grandparentNode = parentPath.GetParentNodeOrRoot();

						Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: having removed child: {child.Id}, it's real parent: {child.ParentId} now has but one real child: {soleChildJob.Id}.");
						Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: moving the logical children of parent: {parent.Id}.");
						
						var nodesToMove = new List<JobTreeNode>(parent.Children);
						foreach(var logicalChildNode in nodesToMove)
						{
							Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: moving logical child: {logicalChildNode.Id} to the grandparent node: {grandparentNode.Id}.");
							_ = logicalChildNode.Move(grandparentNode);
						}
					}
				}
			}

			// Re-add the orphaned nodes.
			var firstParentNode = parentChildPairs[0].parent;
			var parentBranch = firstParentNode.IsRoot ? path.GetRoot() : path.CreatePath(firstParentNode);
			ObjectId? parentId = firstParentNode.IsRoot ? null : firstParentNode.Id;

			if (firstParentNode.IsRoot && pairsToBeOrphaned.Count > 1)
			{
				Debug.WriteLineIf(_useDetailedDebug, "WARNING: The Tree will have multiple Home Jobs.");
			}

			foreach (var (child, grandchild) in pairsToBeOrphaned)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: is adding the orphaned node: {grandchild.Id} to {firstParentNode.Id}.");
				grandchild.Item.ParentJobId = parentId;
				_ = AddNodeAtParentPath(grandchild, firstParentNode, parentBranch);
			}

			return topLevelPairs;
		}

		private bool RemoveNode(JobTreeNode child, JobTreeNode parent)
		{
			if (child.ParentJobId != null && child.ParentJobId != parent.Id)
			{
				var realParentPath = GetPathById(child.ParentJobId.Value, Root);
				if (realParentPath != null)
				{
					var realParentNode = realParentPath.Node;
					if (!realParentNode.RemoveRealChild(child.Item))
					{
						Debug.WriteLineIf(_useDetailedDebug, $"WARNING: could not remove RealChildJob: {child.Id} from real parent: {realParentNode.Id}.");
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"WARNING: could not remove RealChildJob: {child.Id}, could not find any node with Id = {child.ParentJobId.Value}.");
				}
			}
			else
			{
				if (!parent.RemoveRealChild(child.Item))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"WARNING: could not remove RealChildJob: {child.Id} from parent: {parent.Id}.");
				}
			}

			var result = parent.Remove(child);
			return result;
		}

		// Find the node that will receive the selection once the node at path is removed.
		private JobTreeNode? GetNewCurrentNode(JobPathType path)
		{
			var result = GetPreviousItemPath(path, predicate: null)?.Node;
			return result;
		}

		[Conditional("DEBUG2")]
		private void ReportTopLevelJobsRemoved(List<(JobTreeNode parent, JobTreeNode child)> topLevelPairs)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"Remove Jobs: removed these top-level pairs:");
			foreach (var (parent, child) in topLevelPairs)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\tChild: {child.Id}, Parent: {parent.Id}");
			}
		}

		#endregion
	}
}

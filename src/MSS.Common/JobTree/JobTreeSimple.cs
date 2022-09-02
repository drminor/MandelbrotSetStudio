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
			var jobsToRemove = nodeSelectionType switch
			{
				NodeSelectionType.SingleNode => GetSingleParentChildPair(path),										// 1

				NodeSelectionType.Preceeding => GetNodeAndAncestorPairsRun(path).SkipLast(1).ToList(),				// 2

				NodeSelectionType.SinglePlusPreceeding => GetNodeAndAncestorPairsRun(path),							// 1 | 2 (3)

				NodeSelectionType.Following => GetNodeAndDescendantPairsRun(path).Skip(1).ToList(),					// 4

				NodeSelectionType.SinglePlusFollowing => GetNodeAndDescendantPairsRun(path),                        // 1 | 4 (5)

				NodeSelectionType.Run => GetNodeAndDescendantPairsRun(GetBranchHead(path)),                         // 1 | 2 | 4 (7)

				NodeSelectionType.Children => GetDescendantPairsAll(GetBranchHead(path)),                           // 8

				NodeSelectionType.Branch => GetNodeAndDescendantPairsAll(GetBranchHead(path)),                      // 1 | 8 (9)

				NodeSelectionType.SiblingBranches => GetDescendantPairsAllForSiblingBranches(GetBranchHead(path)),	// 16

				_ => throw new NotImplementedException(),
			};

			var selectedNode = SelectedNode;

			if (jobsToRemove.Any(x => x.child.IsSelected) || selectedNode == null)
			{
				selectedNode = GetNewSelectedNode(path);
				SelectedNode = selectedNode;
			}

			if (jobsToRemove.Any(x => x.child.IsOnPreferredPath))
			{
				_ = MakePreferred(selectedNode.Id);
			}

			if (jobsToRemove.Any(x => x.child.IsCurrent))
			{
				CurrentItem = selectedNode.Item;
			}

			var topLevelPairs = RemoveJobs(path, jobsToRemove);

			ReportTopLevelJobsRemoved(topLevelPairs);

			var result = jobsToRemove.Select(x => x.child).ToList();
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
			var nextPath = GetParentPath(previousPath.Item, Root); // Using the real parent Id here.

			while (nextPath != null)
			{
				parentChildPairs.Add((nextPath.GetParentNodeOrRoot(), nextPath.Node));

				previousPath = nextPath;
				nextPath = GetParentPath(previousPath.Item, Root);
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

		private List<(JobTreeNode parent, JobTreeNode child)> GetDescendantPairsAllForSiblingBranches(JobPathType path)
		{
			var parentChildPairs = new List<(JobTreeNode parent, JobTreeNode child)>();

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.RealChildJobs;

			foreach(var siblingJob in siblings)
			{
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

		protected override IList<Job> GetItems(JobBranchType currentBranch)
		{
			var result = GetNodes(currentBranch).Select(x => x.Item).ToList();
			return result;
		}

		protected override IList<JobTreeNode> GetNodes(JobBranchType currentBranch)
		{
			var result = GetNodesWithParentage(currentBranch).Select(x => x.child).ToList();
			return result;
		}

		protected override List<(JobTreeNode? parent, JobTreeNode child)> GetNodesWithParentage(JobBranchType branch)
		{
			List<(JobTreeNode? parent, JobTreeNode child)> result;

			var path = branch.GetCurrentPath();

			if (path != null)
			{
				result = GetDescendantPairsAll(path).Select(x => ((JobTreeNode?) x.parent, x.child)).ToList();
			}
			else
			{
				result = new List<(JobTreeNode? parent, JobTreeNode child)>();
				foreach(var child in branch.Children)
				{
					var childPath = branch.CreatePath(new JobTreeNode[] { child });
					result.AddRange(GetDescendantPairsAll(childPath).Select(x => ((JobTreeNode?)x.parent, x.child)).ToList());
				}
			}

			return result;
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
			var result = node.IsHome || DoesNodeChangeZoom(node) || node.HasRealSiblings;
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

			var childIds = parentChildPairs.Select(x => x.child.Id).ToList();
			foreach (var (parent, child) in parentChildPairs)
			{
				if (!childIds.Contains(parent.Id))
				{
					topLevelPairs.Add((parent, child));
				}
			}

			foreach (var (parent, child) in topLevelPairs)
			{
				_ = parent.Remove(child);
				_ = parent.RemoveRealChild(child.Item);

				var siblings = parent.RealChildJobs;
				if (child.ParentJobId == parent.Id && siblings.Count > 0)
				{
					var firstChildNode = parent.Children[0];
					if (siblings.Count == 1 && !DoesNodeChangeZoom(firstChildNode))
					{
						// The parent no longer has child "choices" or "exits"; 
						// move this sole, remaining child so that it is a child of the parent's parent.

						var parentPath = path.CreatePath(parent);
						var grandparentNode = parentPath.GetParentNodeOrRoot();

						Debug.WriteLine($"Moving a sole remaining child: {child.Id} to the grandparent node: {grandparentNode.Id}.");
						_ = firstChildNode.Move(grandparentNode);
					}
				}
			}

			return topLevelPairs;
		}

		// Find the node that will receive the selection once the node at path is removed.
		private JobTreeNode GetNewSelectedNode(JobPathType path)
		{
			JobTreeNode result;

			var parentNode = path.GetParentNodeOrRoot();
			var siblings = parentNode.RealChildJobs;

			if (siblings.Count < 2)
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

		[Conditional("Debug")]
		private void ReportTopLevelJobsRemoved(List<(JobTreeNode parent, JobTreeNode child)> topLevelPairs)
		{
			Debug.WriteLine($"Removed these top-level pairs:");
			foreach (var (parent, child) in topLevelPairs)
			{
				Debug.WriteLine($"\tParent: {parent.Id}, Child: {child.Id}");
			}
		}

		#endregion
	}
}

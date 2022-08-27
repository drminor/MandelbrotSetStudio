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

			var sb = new StringBuilder();
			sb.AppendLine("*****");

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
						sb.AppendLine($"Setting node: {child.Id} to be the preferred child of parent: {parent.Id}{strResetingChildNodes}");
						numberSet++;
					}
					else
					{
						sb.AppendLine($"Node: {child.Id} is already the preferred child of parent: {parent.Id}.");
					}
				}

				sb.AppendLine("Now selecting the child's last child as the preferred child.");

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
						var strResetingChildNodes = !alreadyVisited ? ", and reseting child nodes." : ".";
						sb.AppendLine($"Setting node: {child.Id} to be the preferred child of parent: {parentNode.Id}{strResetingChildNodes}");
						numberSet++;
					}
					else
					{
						sb.AppendLine($"Node: {nextChildPath.Node.Id} is already the preferred child of parent: {parentNode.Id}.");
					}

					nextChildJob = nextChildPath.Node.RealChildJobs.LastOrDefault().Value;
				}
			}

			sb.AppendLine($"Setting path: {path?.Node.Id} to be the preferred path. Set {numberSet} nodes, reset {numberReset}.");
			sb.AppendLine("*****");

			Debug.WriteLine(sb.ToString());

			return true;
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

		protected override JobPathType? GetNextItemPath(JobPathType path, Func<JobTreeNode, bool>? predicate)
		{
			JobPathType? result;

			if (predicate != null)
			{
				result = GetNextPath(path);

				while(result != null && !predicate(result.Node))
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
				Debug.WriteLine($"GetNextPath, using the node's preferred child.\n{result}");
			}
			else if (currentPosition < siblings.Count - 1 && siblings[currentPosition + 1].ParentId == currentNode.Id)
			{
				var nextNode = siblings[currentPosition + 1];
				result = path.CreateSiblingPath(nextNode);
				Debug.WriteLine($"GetNextPath, using the next node.\n{result}");
			}
			else
			{
				// TODO: Optimize calls to GetPath by providing a closer starting point.
				var realChildPaths = currentNode.RealChildJobs.Select(x => GetPath(x.Value, Root));
				result = realChildPaths.FirstOrDefault(x => x.Node.IsOnPreferredPath) ?? realChildPaths.LastOrDefault();

				// TODO: Make sure every node has its "IsOnPreferredPath" value set.
				//var preferredRealChildJob = currentNode.RealChildJobs.FirstOrDefault(x => x.Value.IsAlternatePathHead).Value ?? currentNode.RealChildJobs.LastOrDefault().Value;
				//result = preferredRealChildJob != null ? GetPath(preferredRealChildJob, Root) : null;

				Debug.WriteLine($"GetNextPath, initial result is null, using RealChildJobs.\n{result}.");
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
			JobPathType? result = null;

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
						Debug.WriteLine($"GetPreviousPath, using the node's preferred child.\n{result}");
					}
					else
					{
						Debug.WriteLine($"GetPreviousPath, node has children, the preferredNode has ParentId: {preferredNode?.ParentId}, the currentNode has id: {currentNode.Id}.\n{result}");
					}
				}
				else
				{
					result = path.CreateSiblingPath(previousNode);
					Debug.WriteLine($"GetPreviousPath, using the previous node.\n{result}");
				}
			}

			if (result == null)
			{
				// TODO: Optimize call to GetParentPath by providing a closer starting point.
				result = GetParentPath(currentNode.Item, Root);
				Debug.WriteLine($"GetPreviousPath, initial result is null, using GetParentPath.\n{result}.");
			}

			return result;
		}

		#endregion
	}
}

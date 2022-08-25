using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
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
			if (path == null)
			{
				Root.GetNodeOrRoot().PreferredChild = null;
			}
			else
			{
				// TODO: Get a path to the node identifed by path, based the node's ParentId.
				// Then set the preferred child top, down.

 				var pathsAndParents = new List<Tuple<JobTreeNode, JobTreeNode>>();

				var parentNode = path.GetParentNodeOrRoot();
				pathsAndParents.Add(new Tuple<JobTreeNode, JobTreeNode>(parentNode, path.Node));

				// Get the parent path using the current path's Job's ParentJobId
				var previousPath = path;
				path = GetParentPath(path.Item, Root);

				while (path != null)
				{
					parentNode = path.GetParentNodeOrRoot();
					pathsAndParents.Add(new Tuple<JobTreeNode, JobTreeNode>(parentNode, path.Node));

					previousPath = path;
					path = GetParentPath(path.Item, Root);
				}

				parentNode = previousPath.GetParentNodeOrRoot();
				pathsAndParents.Add(new Tuple<JobTreeNode, JobTreeNode>(parentNode, previousPath.Node));

				pathsAndParents.Reverse();

				foreach(var pAndP in pathsAndParents)
				{
					parentNode = pAndP.Item1;
					var node = pAndP.Item2;
					parentNode.PreferredChild = node;
				}
			}

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
				result = path.Combine(new[] { currentNode, preferredNode });
			}
			else if (currentPosition < siblings.Count && siblings[currentPosition + 1].ParentId == currentNode.Id)
			{
				result = path.Combine(siblings[currentPosition + 1]);
			}
			else
			{
				// TODO: Optimize calls to GetPath by providing a closer starting point.
				var realChildPaths = currentNode.RealChildJobs.Select(x => GetPath(x.Value, Root));
				result = realChildPaths.FirstOrDefault(x => x.Node.IsOnPreferredPath) ?? realChildPaths.LastOrDefault();
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
						result = path.Combine(new[] { currentNode, preferredNode });
					}
				}
				else
				{
					result = path.Combine(previousNode);
				}
			}

			if (result == null)
			{
				// TODO: Optimize call to GetParentPath by providing a closer starting point.
				result = GetParentPath(currentNode.Item, Root);
			}

			return result;
		}

		#endregion

		#region OLD - DELETE

		protected JobPathType? GetPreviousItemPathOLD(JobPathType path, Func<JobTreeNode, bool>? predicate)
		{
			//var previousNode = GetPreviousPath(path, predicate);

			//var previousPath = path;
			//var curPath = path.GetParentPath();

			//while (previousNode == null && curPath != null)
			//{
			//	previousNode = GetPreviousPath(path, predicate);

			//	previousPath = curPath;
			//	curPath = curPath.GetParentPath();
			//}

			//if (previousNode != null)
			//{
			//	var result = previousPath.Combine(previousNode);
			//	return result;
			//}
			//else
			//{
			//	return null;
			//}

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

		private JobPathType? GetPreviousPath(JobPathType path, Func<JobTreeNode, bool>? predicate)
		{
			JobPathType? result;

			var currentPosition = GetPosition(path, out var siblings);
			//var currentNode = siblings[currentPosition];
			//var preferredNode = currentNode.PreferredChild;

			if (predicate != null)
			{
				result = null;
				for (var i = currentPosition; i >= 0; i--)
				{
					var node = siblings[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null && predicate(preferredNode))
					{
						var newTerms = new[] { node, preferredNode };
						result = path.Combine(newTerms);
						break;
					}
					else
					{
						if (i < currentPosition && predicate(node))
						{
							var newTerms = new[] { node };
							result = path.Combine(newTerms);
							break;
						}
					}
				}
			}
			else
			{
				result = GetPreviousPath(path);
			}

			return result;
		}

		#endregion
	}
}

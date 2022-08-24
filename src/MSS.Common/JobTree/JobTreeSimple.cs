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

		protected override IEnumerable<JobTreeNode>? GetNextNode(IList<JobTreeNode> nodes, int currentPosition, Func<JobTreeNode, bool>? predicate = null)
		{
			IEnumerable<JobTreeNode>? result;

			if (predicate != null)
			{
				result = null;
				for(var i = currentPosition; i < nodes.Count; i++)
				{
					var node = nodes[i];

					if (node.Children.Count > 0)
					{
						var preferredNode = node.PreferredChild;
						if (preferredNode != null)
						{
							if (predicate(preferredNode))
							{
								// TODO: Check the impact of returning a child of one of the items in the list,
								// as opposed to returning one of the items in the list.
								result = new[] { preferredNode }; 
								break;
							}
						}
					}

					if (i > currentPosition && predicate(node))
					{
						result = new[] { node };
						break;
					}
				}
			}
			else
			{
				var node = nodes.Skip(currentPosition).FirstOrDefault()?.PreferredChild ?? nodes.Skip(currentPosition + 1).FirstOrDefault();
				result = node == null ? null : new[] { node };
			}

			return result;
		}

		protected override IEnumerable<JobTreeNode>? GetPreviousNode(IList<JobTreeNode> nodes, int currentPosition, Func<JobTreeNode, bool>? predicate = null)
		{
			var node = predicate != null
				? nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault(predicate)
				: nodes.SkipLast(nodes.Count - currentPosition).LastOrDefault();

			return node == null ? null : new[] { node };
		}

		#endregion
	}
}

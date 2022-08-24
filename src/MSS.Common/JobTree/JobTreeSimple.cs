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
				var originalPathTerms = path.ToString();
				var parentPath = path.GetParentPath();

				while (parentPath != null)
				{
					try
					{
						parentPath.Node.PreferredChild = path.Node;
					}
					catch (InvalidOperationException ioe)
					{
						Debug.WriteLine($"Error1 while setting the parentNode: {parentPath.Node.Id}'s PreferredChild to {path.Node.Id}. The original path has terms: {originalPathTerms}. \nThe error is {ioe}");
					}

					path = parentPath;
					parentPath = path.GetParentPath();
				}

				var parentNode = path.GetParentNodeOrRoot();

				try
				{
					parentNode.PreferredChild = path.Node;
				}
				catch (InvalidOperationException ioe)
				{
					Debug.WriteLine($"Error2 while setting the parentNode: {parentNode.Id}'s PreferredChild to {path.Node.Id}. The original path has terms: {originalPathTerms}. \nThe error is {ioe}");
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

		protected override IEnumerable<JobTreeNode>? GetNextNode(IList<JobTreeNode> nodes, int currentPosition, Func<JobTreeNode, bool>? predicate = null)
		{
			IEnumerable<JobTreeNode>? result = null;

			if (predicate != null)
			{
				for(var i = currentPosition; i < nodes.Count; i++)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null && predicate(preferredNode))
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i > currentPosition && predicate(node))
						{
							result = new[] { node };
							break;
						}
					}
				}
			}
			else
			{
				for (var i = currentPosition; i < nodes.Count; i++)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null)
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i > currentPosition)
						{
							result = new[] { node };
							break;
						}
					}
				}
			}

			return result;
		}

		protected override IEnumerable<JobTreeNode>? GetPreviousNode(IList<JobTreeNode> nodes, int currentPosition, Func<JobTreeNode, bool>? predicate = null)
		{
			IEnumerable<JobTreeNode>? result = null;

			if (predicate != null)
			{
				for (var i = currentPosition; i >= 0; i--)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null && predicate(preferredNode))
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i < currentPosition && predicate(node))
						{
							result = new[] { node };
							break;
						}
					}
				}
			}
			else
			{
				for (var i = currentPosition; i >= 0; i--)
				{
					var node = nodes[i];
					var preferredNode = node.PreferredChild;

					if (preferredNode != null)
					{
						result = new[] { node, preferredNode };
						break;
					}
					else
					{
						if (i < currentPosition)
						{
							result = new[] { node };
							break;
						}
					}
				}
			}

			return result;
		}

		#endregion
	}
}

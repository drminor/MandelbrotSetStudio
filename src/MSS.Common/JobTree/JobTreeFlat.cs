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
	using JobNodeType = ITreeNode<JobTreeNode, Job>;

	public class JobTreeFlat : JobTreeBase
	{
		#region Constructor

		public JobTreeFlat(List<Job> jobs, bool checkHomeJob) : base(jobs, checkHomeJob)
		{ }

		#endregion

		#region Public Methods

		public override bool RemoveBranch(JobPathType path)
		{
			// TODO: RemoveBranch does not support removing CanvasSizeUpdate nodes.

			var node = path.Node;

			JobPathType newPath;

			if (path.Node.IsActiveAlternate)
			{
				// Restore the most recent alternate branches before removing.
				Debug.WriteLine($"Making the branch being removed a parked alternate.");

				var alternateToMakeActive = SelectMostRecentAlternate(path);
				newPath = MakeBranchActive(alternateToMakeActive);
			}
			else
			{
				newPath = path;
			}

			var parentNode = path.GetParentNodeOrRoot();
			var idx = parentNode.Children.IndexOf(node);

			if (node.TransformType == TransformType.ZoomIn)
			{
				// TODO: Determine if this is the last
			}

			if (parentNode.IsActiveAlternate || idx == 0)
			{
				if (parentNode.IsHome)
				{
					throw new InvalidOperationException("Removing the Home node is not yet supported.");
				}

				CurrentPath = newPath.GetParentPath();
			}
			else
			{
				CurrentPath = newPath.CreateSiblingPath(parentNode.Children[idx - 1]);
			}

			var result = parentNode.Children.Remove(node);

			if (parentNode.IsActiveAlternate && !parentNode.Children.Any())
			{
				parentNode.IsActiveAlternate = false;
			}

			ExpandAndSetCurrent(CurrentPath);

			return result;
		}

		public override IList<JobTreeNode> RemoveJobs(JobPathType path, NodeSelectionType nodeSelectionType)
		{
			throw new NotImplementedException();
		}

		public override bool MakePreferred(ObjectId jobId)
		{
			Debug.WriteLine($"Marking Branch for {jobId}, the preferred branch.");

			// TODO: MakePreferred does not support CanvasSizeUpdateJobs
			if (!TryFindPathById(jobId, Root, out var path))
			{
				throw new InvalidOperationException($"Cannot find job: {jobId} for which to mark the branch as preferred.");
			}

			while (path != null && !(path.Count > 1))
			{
				path = path.GetParentPath();
			}

			if (path == null || !path.Node.IsParkedAlternate)
			{
				throw new InvalidOperationException("Cannot mark this branch as preferred, it is not a \"parked\" alternate.");
			}

			var result = MakePreferred(path);

			return result;
		}

		public override bool MakePreferred(JobPathType? path)
		{
			if (path == null)
			{
				//return base.MakePreferred(path);
				Root.GetNodeOrRoot().PreferredChild = null;
			}
			else
			{
				JobPathType newPath;

				if (path.Item.TransformType == TransformType.CanvasSizeUpdate)
				{
					var parentPath = path.GetParentPath()!;
					newPath = MakeBranchActive(parentPath).Combine(path.Node);
				}
				else
				{
					newPath = MakeBranchActive(path);
				}

				ExpandAndSetCurrent(newPath);
				CurrentPath = newPath;
				IsDirty = true;
			}

			return true;
		}

		#endregion

		#region Protected Add Methods

		protected override JobPathType AddAtParentPath(Job job, JobPathType parentPath)
		{
			JobPathType newPath;

			// This is the JobTreeItem for the new Job's real parent.
			var parentNode = parentPath.Node;

			// Add the new Job to the list of it's parent's "real" children.
			// The index is the position of the new job among its siblings which are sorted by the CreatedDate, ascending.
			var index = parentNode.AddRealChild(job);

			// Get the job already in the tree for which the job being added will directly follow.
			JobPathType preceedingPath;

			if (index == 0)
			{
				// Find the sibling of the parent, that comes just before the parent.
				if (parentPath.TryGetParentPath(out var grandparentPath))
				{
					var grandparentId = grandparentPath.Item.Id;
					var grandparentBranch = grandparentPath.GetParentBranch();

					if (TryFindPathById(grandparentId, grandparentBranch, out var realGrandparentPath))
					{
						preceedingPath = realGrandparentPath;
					}
					else
					{
						throw new InvalidOperationException("Cant find a child of a known parent.");
					}
				}
				else
				{
					preceedingPath = parentPath;
				}
			}
			else
			{
				var preceedingJob1 = parentNode.RealChildJobs.Values[index - 1];
				preceedingPath = GetPath(preceedingJob1, parentPath.GetParentBranch());
			}

			// Does the preceeding sibling job (in date order) move the map to a different Zoom level.
			var addingJobAsAnAlt = DoesNodeChangeZoom(preceedingPath.Node);

			if (addingJobAsAnAlt)
			{
				// Add the new node as a Parked ALT.
				newPath = AddAsParkedAlt(job, preceedingPath!);
			}
			else
			{
				// Add the new node in-line after the preceeding ALT Job
				newPath = AddAfter(job, preceedingPath, parentPath);
			}

			return newPath;
		}

		private JobPathType AddAsParkedAlt(Job job, JobPathType preceedingPath)
		{
			JobPathType activeAltPath;

			if (preceedingPath.Node.IsActiveAlternate)
			{
				// The preceeding node is the Active ALT.
				// Add the new job as a sibling to the branch on which my real parent's JobTreeItem was found (in sort order by date created.)
				activeAltPath = preceedingPath;
			}
			else if (preceedingPath.Node.IsParkedAlternate)
			{
				// The parent of the preceeding node is the Active ALT
				var parkedParentPath = preceedingPath.GetParentPath()!;
				activeAltPath = parkedParentPath;
			}
			else
			{
				// The preceeding node has not yet been made an Alternate.
				Debug.WriteLine($"Found a Job that is a new Alternate. Marking existing node: {preceedingPath.LastTerm} as the Active ALT.");
				preceedingPath.Node.IsActiveAlternate = true;
				activeAltPath = preceedingPath;
			}

			Debug.WriteLine($"Adding job: {job.Id}, as a Parked ALT to Active ALT: {activeAltPath.Node.Item.Id}.");

			var parkedAltPath = AddItem(job, activeAltPath);

			// TODO: See if we can avoid making the just added job be on the 'Main' branch.
			var newPath = MakeBranchActive(parkedAltPath);

			return newPath;
		}

		private JobPathType AddAfter(Job job, JobPathType preceedingPath, JobPathType parentPath)
		{
			JobPathType newPath;

			if (preceedingPath.Node.IsParkedAlternate)
			{
				if (job.ParentJobId == preceedingPath.Node.Id) // preceedingPath.LastTerm == parentPath.LastTerm)
				{
					// Add the job as a child of the Parked Alt
					newPath = AddItem(job, preceedingPath);
				}
				else
				{
					newPath = AddInLine(job, preceedingPath.GetParentPath()!);
				}
			}
			else
			{
				// Add the job as a sibling of its parent 
				newPath = AddInLine(job, preceedingPath);
			}

			return newPath;
		}

		private JobPathType AddInLine(Job job, JobPathType parentPath)
		{
			Debug.WriteLine($"Adding job: {job.Id}, in-line after: {parentPath.Node.Item.Id}.");

			var grandparentBranch = GetGrandParentBranch(parentPath);

			Debug.Assert(grandparentBranch.IsEmpty || !grandparentBranch.LastTerm?.IsActiveAlternate == true, "AddJobInLine is adding a job to an Active Alt node.");
			var result = AddItem(job, grandparentBranch);

			return result;
		}

		private JobBranchType GetGrandParentBranch(JobPathType parentPath)
		{
			var grandparentPath = parentPath.GetParentBranch();

			if (parentPath.Node.IsActiveAlternate)
			{
				// The real grandparent branch is the Active Alternate's Parent
				Debug.WriteLine("The grandparentBranch is being set to the great-grandparent of the parentPath -- the parentPath points to an Active Alternate.");
				return grandparentPath.GetParentBranch();
			}
			else
			{
				return grandparentPath;
			}
		}

		protected override JobPathType AddItem(Job job, JobBranchType parentBranch)
		{
			var parentNode = parentBranch.GetNodeOrRoot();
			var newNode = parentNode.AddItem(job);

			if (parentNode.IsActiveAlternate)
			{
				newNode.IsParkedAlternate = true;
			}

			var result = parentBranch.Combine(newNode);

			return result;
		}

		#endregion

		#region Protected GetNode Methods

		protected override IList<Job> GetItems(JobBranchType currentBranch)
		{
			var result = GetNodes(currentBranch).Select(x => x.Item).ToList();
			return result;
		}

		protected override IList<JobTreeNode> GetNodes(JobBranchType currentBranch)
		{
			throw new NotImplementedException();
		}

		protected override List<Tuple<JobTreeNode, JobTreeNode?>> GetNodesWithParentage(JobBranchType currentBranch)
		{
			var result = GetNodes(currentBranch).Select(x => new Tuple<JobTreeNode, JobTreeNode?>(x, x.ParentNode)).ToList();
			return result;
		}

		#endregion

		#region Private Branch Methods

		/// <summary>
		///	1.  Get the jobTreeItem
		///	2.  Find its Parent(The active alternate that will be parked)
		///	3.  Remove the jobTreeItem from its Parent
		///	4.  Get a list of our children and remove them.
		///	
		///			-- SwitchAltBranches --
		///	5.  Get a list of all items from the main branch starting with the item just after the Active Alt, aka Parent
		///	6.  The current Alt becomes our child -- [The Alternate currently in the main trunk]
		///	7.  Move all children of this first child[The other alternates] and make them our children
		///	8.  The items that were identified in step 5 are then added as children once current, now parked Alt
		///			-- SwitchAltBranches
		///	
		///	9.  Move us from being a child of the(once Active) now Parked Alt to be a child of the Grandparent
		///	10. Move our children to the Grandparent(following us.)
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private JobPathType MakeBranchActive(JobPathType path)
		{
			if (path.IsEmpty)
			{
				throw new ArgumentException("Path cannot be empty.");
			}

			var parkedAltNode = path.Node;

			if (parkedAltNode.Item.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException("MakeActiveBranch does not support CanvasSizeUpdates.");
			}

			var activeAltNode = parkedAltNode.ParentNode!;

			if (activeAltNode is null)
			{
				throw new InvalidOperationException("Call to MakeBranchActive found no Active Alt parent of the Parked Alt being made active.");
			}

			// The parked ALT's children contains all of the job following the parked ALT.
			var ourChildern = new List<JobNodeType>(parkedAltNode.Children);

			SwitchAltBranches(parkedAltNode, activeAltNode);

			var parentPath = path.GetParentPath()!;
			var grandparentItem = parentPath.GetParentNodeOrRoot();
			var grandparentBranch = parentPath.GetParentBranch();

			// Move the once Parked, now Active Alt node to the grandparent
			_ = parkedAltNode.Move(grandparentItem);
			var newPath = grandparentBranch.Combine(parkedAltNode);

			// Move each job following the once Parked, now Active Alt node to the grandparent
			for (var i = 0; i < ourChildern.Count; i++)
			{
				var child = ourChildern[i];
				_ = child.Move(grandparentItem);
			}

			return newPath;
		}

		/// <summary>
		/// 1. Get a list of the Jobs following the Active Alt
		/// 2. Move the Active Alt from its parent(main trunk) to become a child of the node becoming Active, i.e., the Parked Alt node.
		/// 3. Move each child of the Active Alt to become a child of the node being parked -- excluding the node being parked.
		/// 4. Move each of the nodes following the (once) ActiveAlt to become a child of the node being parked; i.e., the (once) Active Alt node.
		/// </summary>
		/// <param name="parkedAlt"></param>
		/// <param name="activeAlt"></param>
		private void SwitchAltBranches(JobTreeNode parkedAlt, JobTreeNode activeAlt)
		{
			/* 	
				Nodes have children in three cases:
				1. The root node's children contain the list of jobs currently in play. Some of these may be active alternates.
				2. An active alternate node's children contains all of the other alternates currently not in play, aka the "parked alternates.
				3. A parked alternate node's children contains the jobs that follow this alternate that would also be made active.
			
				An active alternate may be "parked" if its part of a trunk that is parked, if that trunk is made active, this will be the active node.
			*/

			if (!parkedAlt.IsParkedAlternate)
			{
				throw new InvalidOperationException("The newNode being Switched to become the Active ALT is not a Parked ALT.");
			}

			if (!activeAlt.IsActiveAlternate)
			{
				throw new InvalidOperationException("The oldNode being Switched out to become a Parked ALT is not the Active ALT.");
			}

			var parentNode = activeAlt.ParentNode;

			if (parentNode is null)
			{
				throw new InvalidOperationException("Call to SwitchAltBranch found the Active ALT to have no parent.");
			}

			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(activeAlt);

			//var strSiblings = string.Join("; ", siblings.Select(x => x.IdAndParentId));
			//var strNewNodeChildren = string.Join("; ", newNode.Children.Select(x => x.IdAndParentId));
			//Debug.WriteLine($"CurrentAlt: {currentAlt.Job.Id}, CurrentAlt ParentNode: {parentNode.Job.Id}, Siblings = {strSiblings}. current Pos: {currentPosition}, NewNode: {newNode.IdAndParentId}, NewNode Children = {strSiblings}.");

			Debug.WriteLine($"Switching Branches. CurrentAlt: {activeAlt.Item.Id}, CurrentAlt ParentNode: {parentNode.Item.Id}. Current Pos: {currentPosition}, NewNode: {parkedAlt.IdAndParentId}.");

			// Get a list of the items after the current position of the parent. We will use this later to add them to the node being parked.
			var jobsFollowingActiveAlt = siblings.Skip(currentPosition + 1).ToList();

			// Move the Active Alt node from the grandparent node to be a child of the node becoming the Active Alt node.
			// The new Active Alt node stores all of the parked ALTs in its list of Children.
			_ = activeAlt.Move(parkedAlt);

			// Move all children of the Active Alt node to be children of the node becoming the Active Alt node. Don't move the node becoming the Active Alt node.
			var otherAlts = new List<JobNodeType>(activeAlt.Children);
			for (var i = 0; i < otherAlts.Count; i++)
			{
				var otherAlt = otherAlts[i];
				if (otherAlt != parkedAlt)
				{
					_ = otherAlt.Move(parkedAlt);
				}
			}
			// The job being added is the active alternate among it's peer alternates..
			activeAlt.IsActiveAlternate = false;
			activeAlt.IsParkedAlternate = true;
			parkedAlt.IsActiveAlternate = true;
			parkedAlt.IsParkedAlternate = false;

			// All of the jobs that followed the Active Alt node are now added to the Parked Alt node.
			var newlyDesignatedParkedAlt = activeAlt;

			for (var i = 0; i < jobsFollowingActiveAlt.Count; i++)
			{
				var successor = jobsFollowingActiveAlt[i];
				_ = successor.Move(newlyDesignatedParkedAlt);
			}
		}

		private JobPathType SelectMostRecentAlternate(JobPathType currentAltPath)
		{
			var parkedAlts = currentAltPath.Node.Children;
			var mostRecentParkedAlt = parkedAlts.Aggregate((i1, i2) => (i1.Item.CompareTo(i2.Item) > 0) ? i1 : i2);

			//var result = currentAltPath.Combine((JobTreeNode)mostRecentParkedAlt);
			var result = currentAltPath.Combine(mostRecentParkedAlt);

			return result;
		}

		#endregion
	}
}

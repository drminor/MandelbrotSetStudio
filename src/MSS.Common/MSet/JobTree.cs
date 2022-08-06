using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSS.Common
{
	public class JobTree : IJobTree
	{
		private readonly ReaderWriterLockSlim _jobsLock;
		private readonly JobTreeItem _root;

		private JobTreePath? _currentPath;

		#region Constructor

		public JobTree(List<Job> jobs)
		{
			jobs = jobs.OrderBy(x => x.Id.ToString()).ToList();

			ReportInput(jobs);

			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			var numberOfJobsWithNullParentId = jobs.Count(x => !x.ParentJobId.HasValue && x.TransformType != TransformType.CanvasSizeUpdate);

			if (numberOfJobsWithNullParentId > 1)
			{
				Debug.WriteLine($"WARNING: More than one job is a child of the root.");
			}

			//if (numberOfJobsWithNullParentId > 1)
			//{
			//	Debug.WriteLine($"Loading {jobs.Count()} jobs.");
			//	_root = BuildTree(jobs, out _currentPath);
			//}
			//else
			//{
			//	Debug.WriteLine($"Loading {jobs.Count()} jobs from an older project.");
			//	_root = BuildTreeForOldPro(jobs, out _currentPath);
			//}

			Debug.WriteLine($"Loading {jobs.Count()} jobs.");

			_root = JobTreeItem.CreateRoot();
			_currentPath = BuildTree(jobs, _root);

			ReportOutput(_root);
		}

		private	void ReportInput(IList<Job> jobs)
		{
			Debug.WriteLine("INPUT Report");
			Debug.WriteLine("Id\t\t\t\tParentId\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var homeJob = jobs.FirstOrDefault(x => x.TransformType == TransformType.Home);

			if (homeJob != null)
			{
				var strParentJobId = homeJob.ParentJobId.HasValue ? homeJob.ParentJobId.ToString() : "null";
				Debug.WriteLine($"{homeJob.Id}\t{strParentJobId}\t{homeJob.DateCreated}\t{homeJob.TransformType}\t{homeJob.Id.Timestamp}");
			}
			else
			{
				Debug.WriteLine("No Home Node Found.");
			}

			var wlist = jobs.Where(x => x != homeJob).OrderBy(x => x.Id.ToString()).ToList();

			foreach (var j in wlist)
			{
				var strParentJobId = j.ParentJobId.HasValue ? j.ParentJobId.ToString() : "null";
				Debug.WriteLine($"{j.Id}\t{strParentJobId}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		private void ReportOutput(JobTreeItem start)
		{
			Debug.WriteLine("OUTPUT Report");
			Debug.WriteLine("Id\t\t\t\tParentId\t\t\t\tDate\t\t\tTransformType\t\t\tTimestamp");

			var jwps = GetJobsWithParentage(start);

			foreach (var jwp in jwps)
			{
				var j = jwp.Item1;
				var p = jwp.Item2;
				Debug.WriteLine($"{j.Id}\t{p?.Id.ToString() ?? "null"}\t{j.DateCreated}\t{j.TransformType}\t{j.Id.Timestamp}");
			}
		}

		#endregion

		#region Public Properties

		public ObservableCollection<JobTreeItem> JobItems => _root.Children;

		public Job CurrentJob
		{
			get => DoWithReadLock(() =>
			{
				var currentJob = _currentPath?.LastTerm.Job;
				if (currentJob == null)
				{
					currentJob = JobItems[0].Job;
				}
				return currentJob;
			});
			
			set => DoWithWriteLock(() =>
			{
				if (value != _currentPath?.LastTerm.Job)
				{
					var moved = MoveCurrentTo(value, _root, out _currentPath);
					if (!moved && value != null)
					{
						Debug.WriteLine($"WARNING: Could not MoveCurrent to job: {value.Id}.");
					}
				}
			});
		}

		// TODO: Consider having the Job Tree keep track of "CanGoBack" / "CanGoForward" as to make these real properties.
		public bool CanGoBack => DoWithReadLock(() => { return CanMoveBack(_currentPath); });

		public bool CanGoForward => DoWithReadLock(() => { return CanMoveForward(_currentPath); });

		public bool IsDirty { get; set; }

		public bool AnyJobIsDirty => DoWithReadLock(() => { return GetJobs(_root).Any(x => x.IsDirty); });

		#endregion

		#region Public Methods

		public JobTreePath AddHomeJob(Job job, JobTreeItem root)
		{
			_jobsLock.EnterWriteLock();

			try
			{
				if (job.ParentJobId != null)
				{
					throw new InvalidOperationException($"Attempting to add a home job, but the job's parentJobId is {job.ParentJobId}.");
				}

				if (job.TransformType != TransformType.Home)
				{
					throw new InvalidOperationException($"Attempting to add a home job, but the job's TransformType is {job.TransformType}.");
				}

				if (root.Children.Any())
				{
					throw new InvalidOperationException("Attempting to add a job with TransformType = Home to a non-empty tree.");
				}

				var newPath = AddJob(job, null);

				IsDirty = true;

				return newPath;
			}
			finally
			{
				_jobsLock.ExitWriteLock();
			}
		}

		/* Notes for Method: Add
			New jobs are inserted into order by the date the job was created.
			The new job's Parent identifies the job from which this job was created.

			New jobs are added as a sibling to it "parent" if the new Job
				is being added as the last job
				or the job is not a Zoom-In or Zoom-Out and the job that is currently the last is not a Zoom-In or Zoom-Out
			Otherwise new jobs are added as a "Parked Alteranate" to the Job just before the point of insertion.	

			Here are the steps:
			1. Determine the point of insertion
			2. If the job will be added to the end or if it is not a Zoom-In or Zoom-Out and the job currently in the last position is not a Zoom-In or Zoom-Out type job
				then insert the new job just before the job at the insertion point.

			3. Otherwise
				a. Add the new Job as child of job currently at the point of insertion (if the job is a Zoom-In or Zoom-Out type job
			or	b. Add the job as a child of the job currently in the last position (if the job currently in the last position is Zoom-In or Zoom-Out type job.
				c. Make the new newly added node, active by calling MakeBranchActive
		*/

		public JobTreePath Add(Job job, bool selectTheAddedJob)
		{
			_jobsLock.EnterWriteLock();

			try
			{
				JobTreePath newPath;

				if (job.TransformType == TransformType.Home)
				{
					newPath = AddHomeJob(job, _root);
				}
				else
				{
					var parentPath = GetSourceJob(job, nodeToBeginSearch: _root);
					var parentJobTreeItem = parentPath.LastTerm;

					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						var canvasSizeUpdateNode = parentJobTreeItem.AddCanvasSizeUpdateJob(job);
						newPath = CreatePath(parentPath, canvasSizeUpdateNode);
					}
					else
					{
						newPath = AddJobAtParentPath(job, parentPath);
					}
				}

				if (selectTheAddedJob)
				{
					ExpandAndSelect(newPath);
					_currentPath = newPath;
				}

				IsDirty = true;

				return newPath;
			}
			finally
			{
				_jobsLock.ExitWriteLock();
			}
		}

		private JobTreePath AddJobAtParentPath(Job job, JobTreePath parentPath)
		{
			var parentJobTreeItem = parentPath.LastTerm;

			var grandParentPath = GetParentPath(parentPath);
			parentJobTreeItem.RealChildJobs.Add(job);
			var realChildrenOfTypeZoomCnt = parentJobTreeItem.RealChildJobs.Count(x => x.TransformType == TransformType.ZoomIn || x.TransformType == TransformType.ZoomOut);

			JobTreePath newPath;

			if (job.TransformType == TransformType.ZoomIn || job.TransformType == TransformType.ZoomOut)
			{
				if (realChildrenOfTypeZoomCnt == 1)
				{
					// We are the first real child with TransformType = Zoom-In or Zoom-Out
					// Add the new job to the main trunk in sort order by date created.
					newPath = AddJob(job, grandParentPath);
				}
				else if (realChildrenOfTypeZoomCnt == 2)
				{
					// We are now one of two real children to be added so far -- the parent is neither an Active or Parked ALT.
					// The logical parent of this node (that is being becoming an Active ALT)
					// is the parent of the SourceJob, i.e., the Grandparent of the job being added.
					var currentAltJob = parentJobTreeItem.RealChildJobs[0];

					if (TryFindJobTreeItem(currentAltJob, grandParentPath, out var currentAltNodePath))
					{
						var newParkedAltNodePath = AddJob(job, currentAltNodePath);
						Debug.Assert(currentAltNodePath.LastTerm.IsActiveAlternateBranchHead, "During Add, the currentAltNode is not the Active ALT.");
						Debug.Assert(newParkedAltNodePath.LastTerm.IsParkedAlternateBranchHead, "During Add, the newParkedAltNode is not a Parked ALT.");
						newPath = MakeBranchActive(newParkedAltNodePath);
					}
					else
					{
						throw new InvalidOperationException($"Cannot find an existing JobTreeItem for Job: {currentAltJob.Id}.");
					}
				}
				else
				{
					// We are now one of three or more real child to be added so far -- the parent is either the Active ALT or a Parked ALT.
					// The logical parent of this node (that is being becoming an Active ALT)
					// is the parent of the SourceJob, i.e., the Grandparent of the job being added.

					// If the ParentTreeItem is the Active ALT, use its path
					// otherwise the ParentTreeItem is a child of the Active ALT. (The Parent of any Parked ALT is the Active ALT)

					//var currentAltNodePath = parentJobTreeItem.IsActiveAlternateBranchHead
					//	? parentPath
					//	: GetParentPathNonNull(parentPath);

					//var newParkedAltNodePath = AddJob(job, currentAltNodePath);
					//Debug.Assert(currentAltNodePath[^1].IsActiveAlternateBranchHead, "During Add, the currentAltNode is not the Active ALT.");
					//Debug.Assert(newParkedAltNodePath[^1].IsParkedAlternateBranchHead, "During Add, the newParkedAltNode is not a Parked ALT.");
					//newPath = MakeBranchActive(newParkedAltNodePath);

					var someAltJob = parentJobTreeItem.RealChildJobs[0];

					if (TryFindJobTreeItem(someAltJob, grandParentPath, out var someAltNodePath))
					{
						var someAltNode = someAltNodePath.LastTerm;

						JobTreePath? currentAltNodePath;

						if (someAltNode.IsActiveAlternateBranchHead)
						{
							currentAltNodePath = someAltNodePath;
						}
						else
						{
							currentAltNodePath = GetParentPath(someAltNodePath);
							if (currentAltNodePath == null)
							{
								throw new InvalidOperationException($"Found a parked ALT: {someAltNode.Job.Id} whose parent is null.");
							}
						}

						var newParkedAltNodePath = AddJob(job, currentAltNodePath);
						Debug.Assert(currentAltNodePath.LastTerm.IsActiveAlternateBranchHead, "During Add, the currentAltNode is not the Active ALT.");
						Debug.Assert(newParkedAltNodePath.LastTerm.IsParkedAlternateBranchHead, "During Add, the newParkedAltNode is not a Parked ALT.");
						newPath = MakeBranchActive(newParkedAltNodePath);
					}
					else
					{
						throw new InvalidOperationException($"Cannot find an existing JobTreeItem for Job: {someAltJob.Id}.");
					}


				}
			}
			else
			{
				// TODO: Fix JobTree::Add when adding a job with TranformType other than Zoom-In or Zoom-Out
				// Insert this job into the branch to which this Job's predecessor belongs
				newPath = AddJob(job, grandParentPath);
			}

			return newPath;
		}

		public bool RestoreBranch(ObjectId jobId)
		{
			Debug.WriteLine($"Restoring Branch: {jobId}.");

			if (!TryFindJobTreeItemById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
			{
				throw new InvalidOperationException($"Cannot find job: {jobId} that is being restored.");
			}

			var result = RestoreBranch(path);

			return result;
		}

		private bool RestoreBranch(JobTreePath path)
		{
			var newPath = MakeBranchActive(path);

			ExpandAndSelect(newPath);
			_currentPath = (JobTreePath?)newPath;
			IsDirty = true;
			return true;
		}

		/* Notes for Method: MakeBranchActive
		  
		1. Get the jobTreeItem
		2. Find its Parent (The active alternate that will be parked)
		3. Remove the jobTreeItem from its Parent
		4. Get a list of our children and remove them.

				-- SwitchAltBranches --
		5.  Get a list of all items starting with the Parent and remove them
		6. The first becomes our child -- [The Alternate currently in the main trunk]
		7. Move all children of this first child [The other alternates] and make them our children
		8. The items that used to follow the first child are then added as children of that first child
				-- SwitchAltBranches

		9. Add us to the Grandparent (following the parent)
		10. Add our former childern to the Grandparent (following us.)
		*/
		
		private JobTreePath MakeBranchActive(JobTreePath path)
		{
			var parkedAltNode = path.LastTerm;

			var activeAltNode = parkedAltNode.ParentNode;

			if (activeAltNode is null)
			{
				throw new InvalidOperationException("Call to MakeBranchActive found no Active Alt parent of the Parked Alt being made active.");
			}

			// Remove the alt node being restored from the list of parked alternates.
			_ = activeAltNode.Remove(parkedAltNode);

			// The parked ALT's children contains all of the job following the parked ALT.
			
			var ourChildern = new List<JobTreeItem>(parkedAltNode.Children);
			parkedAltNode.Children.Clear();

			SwitchAltBranches(parkedAltNode, activeAltNode);

			// Add the new job to the end of the main trunk.
			var grandParentPath = GetGrandParentPath(path);
			var newPath = AddJobTreeItem(parkedAltNode, grandParentPath);

			for (var i = 0; i < ourChildern.Count; i++)
			{
				var child = ourChildern[i];
				AddJobTreeItem(child, grandParentPath);
			}

			return newPath;
		}

		private void SwitchAltBranches(JobTreeItem newNode, JobTreeItem currentAlt)
		{
			/* 	
				Nodes have children in three cases:
				1. The root node's children contain the list of jobs currently in play. Some of these may be active alternates.
				2. An active alternate node's children contains all of the other alternates currently not in play, aka the "parked alternates.
				3. A parked alternate node's children contains the jobs that follow this alternate that would also be made active.
			
				An active alternate may be "parked" if its part of a trunk that is parked, if that trunk is made active, this will be the active node.
			*/

			if (!newNode.IsParkedAlternateBranchHead)
			{
				throw new InvalidOperationException("The newNode being Switched to become the Active ALT is not a Parked ALT.");
			}

			if (!currentAlt.IsActiveAlternateBranchHead)
			{
				throw new InvalidOperationException("The oldNode being Switched out to become a Parked ALT is not the Active ALT.");
			}

			var parentNode = currentAlt.ParentNode;

			if (parentNode is null)
			{
				throw new InvalidOperationException("Call to SwitchAltBranch found the Active ALT to have no parent.");
			}

			var siblings = parentNode.Children;
			var currentPosition = siblings.IndexOf(currentAlt);

			var strSiblings = string.Join("; ", siblings.Select(x => x.IdAndParentId));
			var strNewNodeChildren = string.Join("; ", newNode.Children.Select(x => x.IdAndParentId));
			Debug.WriteLine($"CurrentAlt: {currentAlt.Job.Id}, CurrentAlt ParentNode: {parentNode.Job.Id}, Siblings = {strSiblings}. current Pos: {currentPosition}, NewNode: {newNode.IdAndParentId}, NewNode Children = {strSiblings}.");

			// Take all items after the current position of the grandparent and add them to an alternate path
			var jobsFollowingCurrentAlt = siblings.Skip(currentPosition + 1).ToList();

			// The new active ALT stores all of the parked ALTs in its list of Children.
			_ = currentAlt.Move(newNode);

			// Move all children of this first child[The other alternates] and make them our children
			var otherAlts = new List<JobTreeItem>(currentAlt.Children);
			for (var i = 0; i < otherAlts.Count; i++)
			{
				var otherAlt = otherAlts[i];
				_ = otherAlt.Move(newNode);
			}
			// The job being added is the active alternate among it's peer alternates..
			currentAlt.IsActiveAlternateBranchHead = false;
			currentAlt.IsParkedAlternateBranchHead = true;
			newNode.IsActiveAlternateBranchHead = true;
			newNode.IsParkedAlternateBranchHead = false;

			// All of the jobs that followed the first orphan are now added as children to the orphan
			var parent = currentAlt;

			for (var i = 0; i < jobsFollowingCurrentAlt.Count; i++)
			{
				var successor = jobsFollowingCurrentAlt[i];
				_ = successor.Move(parent);
			}
		}

		public bool RemoveBranch(ObjectId jobId)
		{
			// TODO: RemoveBranch does not support removing CanvasSizeUpdate nodes.
			if (!TryFindJobTreeItemById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
			{
				return false;
			}

			var jobTreeItem = path.LastTerm;

			if (jobTreeItem.IsActiveAlternateBranchHead)
			{
				// Restore the most recent alternate branches before removing.
				Debug.WriteLine($"Making the branch being removed a parked alternate.");

				//var alternateToMakeActive = SelectMostRecentAlternate(jobTreeItem.Children);
				var alternateToMakeActive = jobTreeItem.Children[0];
				var selectedAltPath = CreatePath(path, alternateToMakeActive);

				var newPath = MakeBranchActive(selectedAltPath);

				// Update the path to reflect that the item being removed is now a child of the newly activated alternate.
				path = CreatePath(newPath, jobTreeItem);
			}

			var parent = GetParentComponent(path);
			var parentPath = GetParentPath(path);
			var idx = parent.Children.IndexOf(jobTreeItem);

			if (jobTreeItem.TransformType == TransformType.ZoomIn)
			{
				// TODO: Determine if this JobTreeItem with TransformType = ZoomIn is the last node of the current branch as we execute RemoveBranch
			}

			if (parent.IsActiveAlternateBranchHead || idx == 0)
			{
				if (parentPath == null)
				{
					throw new InvalidOperationException("Removing the Home node is not yet supported.");
				}

				_currentPath = (JobTreePath?)parentPath;
			}
			else
			{
				_currentPath = (JobTreePath?)CreatePath(parentPath, parent.Children[idx - 1]);
			}

			// TODO: Examine how we "tear" down a set of JobTreeItems.
			jobTreeItem.RealChildJobs.Clear();

			var result = parent.Remove(jobTreeItem);

			if (parent.IsActiveAlternateBranchHead && !parent.Children.Any())
			{
				parent.IsActiveAlternateBranchHead = false;
			}

			ExpandAndSelect(_currentPath);

			return result;
		}

		//private JobTreeItem SelectMostRecentAlternate(IList<JobTreeItem> siblings)
		//{
		//	var result = siblings.Aggregate((i1, i2) => i1.Created > i2.Created ? i1 : i2);
		//	return result;
		//}

		public bool TryGetPreviousJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job)
		{
			if (_currentPath == null)
			{
				job = null;
				return false;
			}

			var backPath = GetPreviousJobPath(_currentPath, skipPanJobs);
			job = GetJobFromPath(backPath);

			return job != null;
		}

		public bool MoveBack(bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var backPath = GetPreviousJobPath(_currentPath, skipPanJobs);

			if (backPath != null)
			{
				_currentPath = backPath;
				ExpandAndSelect(backPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetNextJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job)
		{
			if (_currentPath == null)
			{
				job = null;
				return false;
			}

			var forwardPath = GetNextJobPath(_currentPath, skipPanJobs);
			job = GetJobFromPath(forwardPath);

			return job != null;
		}

		public bool MoveForward(bool skipPanJobs)
		{
			if (_currentPath == null)
			{
				return false;
			}

			var forwardPath = GetNextJobPath(_currentPath, skipPanJobs);

			if (forwardPath != null)
			{
				_currentPath = forwardPath;
				ExpandAndSelect(forwardPath);
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy)
		{
			_jobsLock.EnterUpgradeableReadLock();

			try
			{
				if (TryFindJobTreeParent(job, out var parentPath))
				{
					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						// The parentPath points to the "original job" for which the CanvasSizeUpdate job was created.
						// We need to get its parentPath to continue.

						var grandparentNode = GetParentPath(parentPath)?.LastTerm;
						if (grandparentNode != null)
						{
							var proxyJobTreeItem = grandparentNode.AlternateDispSizes?.FirstOrDefault(x => x.Job.CanvasSizeInBlocks == canvasSizeInBlocks);
							proxy = proxyJobTreeItem?.Job;
							return proxy != null;
						}
						else
						{
							proxy = null;
							return false;
						}
					}
					else
					{
						var parentNode = parentPath.LastTerm;
						var proxyJobTreeItem = parentNode.AlternateDispSizes?.FirstOrDefault(x => x.Job.CanvasSizeInBlocks == canvasSizeInBlocks);
						proxy = proxyJobTreeItem?.Job;
						return proxy != null;
					}
				}
				else
				{
					proxy = null;
					return false;
				}
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		#endregion

		#region Public Methods - Collection

		public JobTreePath? GetCurrentPath()
		{
			return DoWithReadLock(() =>
			{
				return _currentPath == null ? null : _currentPath;
			});
		}

		public JobTreePath? GetPath(ObjectId jobId)
		{
			// TODO: GetPath does not support getting CanvasSizeUpdate Jobs.
			var path = DoWithReadLock(() =>
			{
				return GetJobPath(jobId, includeCanvasSizeUpdateJobs: false);
			});

			return path;
		}

		public IEnumerable<Job> GetJobs()
		{
			_jobsLock.EnterReadLock();

			try
			{
				var result = GetJobs(_root);
				return result;
			}
			finally
			{
				_jobsLock.ExitReadLock();
			}
		}

		public Job? GetJob(ObjectId jobId)
		{
			_ = TryFindJob(jobId, _root, out var result);
			return result;
		}

		//TODO: Use ParentNode instead.
		public Job? GetParent(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}
			else
			{
				_ = TryFindJob(job.ParentJobId.Value, _root, out var result);
				return result;
			}
		}

		public List<Job>? GetJobAndDescendants(ObjectId jobId)
		{
			return DoWithReadLock(() =>
			{
				List<Job>? result;

				if (TryFindJobTreeItemById(jobId, _root, includeCanvasSizeUpdateJobs: false, out var path))
				{
					var jobTreeItem = path.LastTerm;

					result = new List<Job> { jobTreeItem.Job };
					result.AddRange(GetJobs(jobTreeItem));
				}
				else
				{
					result = null;
				}

				return result;
			});
		}

		#endregion

		#region Jobs List Methods

		private JobTreePath? BuildTree(IList<Job> jobs, JobTreeItem root)
		{
			if (!jobs.Any())
			{
				throw new ArgumentException("When building a JobTree the list of jobs cannot be empty", nameof(jobs));
			}

			var homeJob = jobs.FirstOrDefault(x => x.TransformType == TransformType.Home);

			if (homeJob == null)
			{
				throw new InvalidOperationException("There is no Job with TransformType = Home.");
			}

			var homeNodePath = AddHomeJob(homeJob, root);
			var homeNode = homeNodePath.LastTerm;
			//var remainingJobs = PrepareRemaingJobs(jobs, homeJob);

			var visited = 1;

			//LoadChildItems(root, remainingJobs, ref visited);
			LoadChildItems(jobs, homeNode, root, ref visited);

			if (visited != jobs.Count)
			{
				Debug.WriteLine("Not all jobs were included.");
			}

			_ = MoveCurrentTo(jobs[0], root, out var path);

			return path;
		}

		private void LoadChildItems(IList<Job> jobs, JobTreeItem parentJobTreeItem, JobTreeItem root, ref int visited)
		{
			var childJobs = GetChildren(parentJobTreeItem.JobId, jobs);
			foreach (var job in childJobs)
			{
				visited++;

				if (job.TransformType == TransformType.CanvasSizeUpdate)
				{
					Debug.Assert(!jobs.Any(x => x.ParentJobId == job.Id), "Found a CanvasSizeUpdateJob that has children.");
					_ = parentJobTreeItem.AddCanvasSizeUpdateJob(job);
					
				}
				else
				{
					var path = Add(job, selectTheAddedJob: false);
					var child = path.LastTerm;

					Debug.Assert(TryFindJobTreeItem(job, _root, out _), "Cannot find job just added.");

					LoadChildItems(jobs, child, root, ref visited);
				}
			}
		}

		private IList<Job> GetChildren(ObjectId parentJobId, IList<Job> jobs)
		{
			ObjectId? parentIdToFind = parentJobId == ObjectId.Empty ? null : parentJobId;
			var result = jobs.Where(x => x.ParentJobId == parentIdToFind).OrderBy(x => x.Id.Timestamp).ToList();

			return result;
		}

		//private List<Job> PrepareRemaingJobs(IList<Job> jobs, Job homeJob)
		//{
		//	//foreach (var job in jobs)
		//	//{
		//	//	if (job.ParentJobId == null)
		//	//	{
		//	//		job.ParentJobId = homeJob.Id;
		//	//	}

		//	//	//if (job.TransformType == TransformType.CanvasSizeUpdate)
		//	//	//{
		//	//	//	job.TransformType = TransformType.ZoomOut;
		//	//	//}
		//	//}

		//	var remainingJobs = jobs.Where(x => x != homeJob).OrderBy(x => x.Id.ToString()).ToList();

		//	return remainingJobs;
		//}


		private IList<Job> GetJobs(JobTreeItem jobTreeItem)
		{
			// TODO: Consider implementing an IEnumerator<JobTreeItem> for the JobTree class.
			var result = new List<Job>();

			foreach (var child in jobTreeItem.Children)
			{
				result.Add(child.Job);

				if (child.AlternateDispSizes != null)
				{
					result.AddRange(child.AlternateDispSizes.Select(x => x.Job));
				}

				var jobList = GetJobs(child);
				result.AddRange(jobList);
			}

			return result;
		}

		private List<Tuple<Job, Job?>> GetJobsWithParentage(JobTreeItem jobTreeItem)
		{
			var result = new List<Tuple<Job, Job?>>();

			var parentJob = jobTreeItem.Job;

			foreach (var child in jobTreeItem.Children)
			{
				result.Add(new Tuple<Job, Job?>(child.Job, parentJob));
				if (child.AlternateDispSizes != null)
				{
					result.AddRange
						(
							child.AlternateDispSizes.Select(x => new Tuple<Job, Job?>(x.Job, child.Job))
						);
				}

				var jobList = GetJobsWithParentage(child);
				result.AddRange(jobList);
			}

			return result;
		}

		#endregion

		#region Private Collection Methods, With Support for CanvasSizeUpdates

		private JobTreePath? GetJobPath(ObjectId jobId, bool includeCanvasSizeUpdateJobs)
		{
			return TryFindJobTreeItemById(jobId, _root, includeCanvasSizeUpdateJobs, out var path) ? path : null;
		}

		private bool TryFindJobTreeItemById(ObjectId jobId, JobTreeItem jobTreeItem, bool includeCanvasSizeUpdateJobs, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (includeCanvasSizeUpdateJobs)
			{
				throw new NotImplementedException("Finding a job, including CanvasSizeUpdateJobs, using only an Id is not yet supported.");
			}
			var foundNode = jobTreeItem.Children.FirstOrDefault(x => x.JobId == jobId);

			if (foundNode != null)
			{
				path = new JobTreePath(foundNode);
				return true;
			}
			else
			{
				foreach (var child in jobTreeItem.Children)
				{
					if (TryFindJobTreeItemById(jobId, child, includeCanvasSizeUpdateJobs, out var localPath))
					{
						path = new JobTreePath(child);
						path = path.Combine(localPath);
						return true;
					}
				}

				path = null;
				return false;
			}
		}

		private bool MoveCurrentTo(Job? job, JobTreeItem jobTreeItem, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job == null)
			{
				path = null;
				return false;
			}

			if (TryFindJobTreeItem(job, jobTreeItem, out path))
			{
				ExpandAndSelect(path);
				return true;
			}
			else
			{
				path = null;
				return false;
			}
		}

		private bool TryFindJobTreeItem(Job job, JobTreePath? parentPath, [MaybeNullWhen(false)] out JobTreePath path)
		{
			var parentNode = parentPath == null ? _root : parentPath.LastTerm;
			var result = TryFindJobTreeItem(job, parentNode, out path);

			return result;
		}

		private bool TryFindJobTreeItem(Job job, JobTreeItem parentNode, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				//throw new InvalidOperationException($"TryFindJobTreeItem does not support finding jobs with TransformType = CanvasSizeUpdate. Use {nameof(TryFindCanvasSizeUpdateTreeItem)} instead.");
				var result = TryFindCanvasSizeUpdateTreeItem(job, parentNode, out path);
				return result;
			}

			var foundNode = parentNode.Children.FirstOrDefault(x => x.JobId == job.Id);

			if (foundNode != null)
			{
				path = new JobTreePath(foundNode);
				return true;
			}
			else
			{
				foreach (var child in parentNode.Children)
				{
					if (TryFindJobTreeItem(job, child, out var localPath))
					{
						path = new JobTreePath(child);
						path.Combine(localPath);
						return true;
					}
				}

				path = null;
				return false;
			}
		}

		private JobTreePath GetSourceJob(Job job, JobTreeItem nodeToBeginSearch)
		{
			JobTreePath result;

			if (job.ParentJobId == null)
			{
				throw new ArgumentException("Cannot get the SourceJob if the Job's ParentJobId is null.");
			}
			else
			{
				if (TryFindJobTreeItemById(job.ParentJobId.Value, nodeToBeginSearch, includeCanvasSizeUpdateJobs: false, out var parentPath))
				{
					result = parentPath;
				}
				else
				{
					throw new InvalidOperationException($"Cannot find the parent JobTreeItem for job: {job.Id}, that has ParentJob: {job.ParentJobId.Value}.");
				}
			}

			return result;
		}

		private bool TryFindCanvasSizeUpdateTreeItem(Job job, JobTreeItem parentNode, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Attempting to Find a CanvasSizeUpdate JobTreeItem using a Job that has a TransformType other than " +
					$"{nameof(TransformType.CanvasSizeUpdate)} is not supported. The TransformType of this job is {job.TransformType}.");
			}

			var parentJobId = job.ParentJobId;

			if (parentJobId == null)
			{
				throw new InvalidOperationException("When finding a CanvasSizeUpdate, the job must have a parent.");
			}

			if (TryFindJobTreeItemById(parentJobId.Value, parentNode, includeCanvasSizeUpdateJobs: false, out var parentPath))
			{
				var parentJobTreeItem = parentPath.LastTerm;
				var canvasSizeUpdateTreeItem = parentJobTreeItem.AlternateDispSizes?.FirstOrDefault(x => x.Job.Id == job.Id);

				if (canvasSizeUpdateTreeItem != null)
				{
					path = CreatePath(parentPath, canvasSizeUpdateTreeItem); 
					return true;
				}
				else
				{
					path = null;
					return false;
				}
			}
			else
			{
				path = null;
				return false;
			}
		}

		private bool TryFindJobTreeParent(Job job, [MaybeNullWhen(false)] out JobTreePath path)
		{
			if (job.ParentJobId == null)
			{
				path = null;
				return false;
			}
			else
			{
				return TryFindJobTreeItemById(job.ParentJobId.Value, _root, includeCanvasSizeUpdateJobs: false, out path);
			}
		}

		#endregion

		#region Private Collection Methods, No Support for CanvasSizeUpdates

		private JobTreePath AddJob(Job job, JobTreePath? parentPath)
		{
			var parentNode = parentPath == null ? _root : parentPath.LastTerm;
			var newNode = parentNode.AddJob(job);
			var result = CreatePath(parentPath, newNode);

			return result;
		}

		private JobTreePath AddJobTreeItem(JobTreeItem newNode, JobTreePath? parentPath)
		{
			var parentNode = parentPath == null ? _root : parentPath.LastTerm;
			parentNode.Add(newNode);
			var result = CreatePath(parentPath, newNode);

			return result;
		}

		private bool TryFindJob(ObjectId jobId, JobTreeItem jobTreeItem, [MaybeNullWhen(false)] out Job job)
		{
			var foundNode = jobTreeItem.Children.FirstOrDefault(x => x.JobId == jobId);

			if (foundNode != null)
			{
				job = foundNode.Job;
				return true;
			}
			else
			{
				foreach (var child in jobTreeItem.Children)
				{
					if (TryFindJob(jobId, child, out job))
					{
						return true;
					}
				}

				job = null;
				return false;
			}
		}

		#endregion

		#region Private Path Methods

		private void ExpandAndSelect(JobTreePath? path)
		{
			if (path == null)
			{
				return;
			}

			foreach(var p in path.Terms.SkipLast(1))
			{
				p.IsExpanded = true;
			}

			if (path.LastTerm.TransformType == TransformType.CanvasSizeUpdate)
			{
				path.Terms[^2].IsSelected = true;
			}
			else
			{
				path.LastTerm.IsSelected = true;
			}
		}

		private Job? GetJobFromPath(JobTreePath? path)
		{
			return path?.LastTerm.Job;
		}

		private JobTreeItem GetItemComponent(IList<JobTreeItem> path)
		{
			return path[^1];
		}

		private JobTreeItem GetParentComponent(JobTreePath path)
		{
			JobTreeItem result;

			if (path.LastTerm.TransformType == TransformType.CanvasSizeUpdate)
			{
				if (path.Count < 2)
				{
					throw new InvalidOperationException("When getting the ParentComponent of a Path pointing to a JobTreeItem for a Job with a TransformType = CanvasSizeUpdate, the path must have at least two components.");
				}
				else
				{
					result = path.Count == 2 ? _root : path.Terms[^3];
				}
			}
			else
			{
				result = path.Count == 1 ? _root : path.Terms[^2];
			}

			return result;
		}

		private JobTreeItem? GetGrandParentComponent(IList<JobTreeItem> path)
		{
			if (path[^1].TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException("GetGrandParentComponent does not support CanvasSizeUpdate paths.");
			}
			
			var result = path.Count == 1 ? null : path.Count == 2 ? _root : path[^3];
			return result;
		}

		private JobTreePath? GetParentPath(JobTreePath path)
		{
			var result = path.Count == 1 ? null : new JobTreePath(path.Terms.SkipLast(1).ToList());
			return result;
		}

		private JobTreePath? GetGrandParentPath(JobTreePath path)
		{
			var result = path.Count < 3 ? null : new JobTreePath(path.Terms.SkipLast(2));
			return result;
		}

		private JobTreePath CreatePath(JobTreePath? path, JobTreeItem item)
		{
			// TODO: Consider treating a path with a single node that has a Id of ObjectId.Empty the same as null.
			var result = path == null ? new JobTreePath(item) : path.Clone().Combine(item);
			return result;
		}

		private JobTreePath CreateSiblingPath(JobTreePath path, JobTreeItem item)
		{
			var result = CreatePath(new JobTreePath(path.Terms.ToList().SkipLast(1)), item);
			return result;
		}

		#endregion

		#region Navigate Forward / Backward

		private JobTreePath? GetNextJobPath(JobTreePath path, bool skipPanJobs)
		{
			var currentItem = path.LastTerm;

			if (currentItem == null)
			{
				return null;
			}

			JobTreePath? result;

			var parentJobTreeItem = GetParentComponent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (currentItem.ParentJobId.HasValue && currentItem.IsActiveAlternateBranchHead && currentItem.Children.Any())
			{
				// The new job will be a child of the current job
				result = path.Combine(currentItem.Children[0]);
			}
			else
			{
				if (TryGetNextJobTreeItem(siblings, currentPosition, skipPanJobs, out var nextJobTreeItem))
				{
					// The new job will be a sibling of the current job
					result = CreateSiblingPath(path, nextJobTreeItem);

				}
				else
				{
					result = null;
				}
			}

			return result;
		}

		private bool TryGetNextJobTreeItem(IList<JobTreeItem> jobTreeItems, int currentPosition, bool skipPanJobs, [MaybeNullWhen(false)] out JobTreeItem nextJobTreeItem)
		{
			if (skipPanJobs)
			{
				nextJobTreeItem = jobTreeItems.Skip(currentPosition + 1).FirstOrDefault(x => x.Job.TransformType != TransformType.Pan);
			}
			else
			{
				nextJobTreeItem = jobTreeItems.Skip(currentPosition + 1).FirstOrDefault();
			}

			return nextJobTreeItem != null;
		}

		private bool CanMoveForward(JobTreePath? path)
		{
			if (path == null)
			{
				return false;
			}

			var currentItem = path.LastTerm;

			var parentJobTreeItem = GetParentComponent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			return !(currentPosition == siblings.Count - 1);
		}

		private JobTreePath? GetPreviousJobPath(JobTreePath path, bool skipPanJobs)
		{
			var currentItem = path.LastTerm;

			if (currentItem == null)
			{
				return null;
			}

			var newBasePath = path.Clone();

			var parentJobTreeItem = GetParentComponent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);
			var previousJobTreeItem = GetPreviousJobTreeItem(siblings, currentPosition, skipPanJobs);

			while (previousJobTreeItem == null && newBasePath.Count > 1)
			{
				newBasePath = new JobTreePath(newBasePath.Terms.SkipLast(1));
				currentItem = newBasePath.LastTerm;

				var grandParentJobTreeItem = GetParentComponent(newBasePath);
				var ancestors = grandParentJobTreeItem.Children;
				currentPosition = ancestors.IndexOf(currentItem);
				previousJobTreeItem = GetPreviousJobTreeItem(ancestors, currentPosition + 1, skipPanJobs);
			}

			if (previousJobTreeItem != null)
			{
				var result = CreateSiblingPath(newBasePath, previousJobTreeItem);

				return result;
			}
			else
			{
				return null;
			}
		}

		private JobTreeItem? GetPreviousJobTreeItem(IList<JobTreeItem> jobTreeItems, int currentPosition, bool skipPanJobs)
		{
			JobTreeItem? result;

			if (skipPanJobs)
			{
				result = jobTreeItems.SkipLast(jobTreeItems.Count - currentPosition).LastOrDefault(x => x.Job.TransformType != TransformType.Pan);
			}
			else
			{
				result = jobTreeItems.SkipLast(jobTreeItems.Count - currentPosition).LastOrDefault();
			}

			return result;
		}

		private bool CanMoveBack(JobTreePath? path)
		{
			if (path == null)
			{
				return false;
			}

			var currentItem = path.LastTerm;

			var parentJobTreeItem = GetParentComponent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			if (currentPosition > 0)
			{
				// We can move to the previous item at the current level.
				return true;
			}
			else
			{
				// If we can go up, return true.
				return path.Count > 1;
			}
		}

		#endregion

		#region Lock Helpers

		private T DoWithReadLock<T>(Func<T> function)
		{
			_jobsLock.EnterReadLock();

			try
			{
				return function();
			}
			finally
			{
				_jobsLock.ExitReadLock();
			}
		}

		private void DoWithWriteLock(Action action)
		{
			_jobsLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_jobsLock.ExitWriteLock();
			}
		}

		#endregion

		#region IDisposable Support

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			((IDisposable)_jobsLock).Dispose();
		}

		#endregion
	}
}

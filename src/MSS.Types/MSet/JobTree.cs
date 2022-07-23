using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSS.Types.MSet
{
	public class JobTree : IJobTree
	{
		private readonly ReaderWriterLockSlim _jobsLock;
		private readonly JobTreeItem _root;

		private List<JobTreeItem>? _currentPath;

		#region Constructor

		public JobTree(IList<Job> jobs)
		{
			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
			_root = BuildTree(jobs, out _currentPath);
		}

		#endregion

		#region Public Properties

		public ObservableCollection<JobTreeItem> JobItems => _root.Children;

		public Job CurrentJob
		{
			get => DoWithReadLock(() =>
				{
					var currentJob = GetJobFromPath(_currentPath);
					if (currentJob == null)
					{
						currentJob = JobItems[0].Job;
					}
					return currentJob;
				});
			set
			{
				_jobsLock.EnterWriteLock();
				
				try
				{
					if (value != GetJobFromPath(_currentPath))
					{
						_ = MoveCurrentTo(value, _root, out _currentPath);
					}

				}
				finally
				{
					_jobsLock.ExitWriteLock();
				}
			}
		}

		public bool CanGoBack => DoWithReadLock(() => { return CanMoveBack(_currentPath); });

		public bool CanGoForward => DoWithReadLock(() => { return CanMoveForward(_currentPath); });

		// TODO: Enumerate over the Tree to avoid actually creating lists.
		public bool AnyJobIsDirty => GetJobs().Any(x => x.IsDirty);

		#endregion

		#region Public Methods

		public void Add(Job job, bool selectTheAddedJob)
		{
			_jobsLock.EnterWriteLock();

			try
			{
				List<JobTreeItem>? newPath;

				if (job.ParentJobId == null)
				{
					if (job.TransformType != TransformType.Home)
					{
						throw new InvalidOperationException($"Attempting to create an new job with no parent and TransformType = {job.TransformType}.");
					}

					if (_root.Children.Count > 0)
					{
						throw new InvalidOperationException("Attempting to add a job with TransformType = Home to a non-empty tree.");
					}

					var newNode = new JobTreeItem(job);
					newPath = AddJob(newNode, null);
				}
				else
				{
					if (!TryFindJobTreeItem(job.ParentJobId.Value, _root, out var parentPath))
					{
						throw new InvalidOperationException("Cannot find parent for the Job that is being added.");
					}

					var parentJobTreeItem = parentPath[^1];

					if (job.TransformType == TransformType.CanvasSizeUpdate)
					{
						parentJobTreeItem.AddCanvasSizeUpdateJob(job);
						// CanvasSizeUpdates are not selected, just return
						return;
					}
					else
					{
						var newNode = new JobTreeItem(job);

						var grandParentJobTreeItem = GetParent(parentPath);
						var siblings = grandParentJobTreeItem.Children;
						var currentPosition = siblings.IndexOf(parentJobTreeItem);

						if (currentPosition != siblings.Count - 1)
						{
							// Take all items past the current position and add them to an alternate path
							var alts = siblings.Skip(currentPosition + 1).ToList();

							for(var i = 0; i < alts.Count; i++)
							{
								var child = alts[i];
								_ = siblings.Remove(child);
								child.Job.ParentJobId = parentJobTreeItem.Job.Id;
								parentJobTreeItem.Children.Add(child);
							}
						}

						ObjectId? grandParentJobId = grandParentJobTreeItem.Job.Id;
						if (grandParentJobId == ObjectId.Empty)
						{
							grandParentJobId = null;
						}

						job.ParentJobId = grandParentJobId;
						var grandParentPath = parentPath.SkipLast(1).ToList();
						newPath = AddJob(newNode, grandParentPath);
					}
				}

				if (selectTheAddedJob)
				{
					ExpandAndSelect(newPath);
					_currentPath = newPath;
				}
			}
			finally
			{
				_jobsLock.ExitWriteLock();
			}
		}

		private ObjectId GetTheHomeId()
		{
			var homeNode =_root.Children.FirstOrDefault(x => x.Job.TransformType == TransformType.Home);
			if (homeNode == null)
			{
				throw new InvalidOperationException("Could not find the home node.");
			}

			return homeNode.Job.Id;
		}

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

		public Job? GetJob(ObjectId jobId)
		{
			_ = TryFindJob(jobId, _root, out var result);
			return result;
		}

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

		public bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy)
		{
			_jobsLock.EnterUpgradeableReadLock();

			try
			{
				if (!TryFindJobTreeItem(job.Id, _root, out var path))
				{
					proxy = null;
					return false;
				}

				var parent = job.TransformType == TransformType.CanvasSizeUpdate ? GetGrandParent(path) : GetParent(path);

				if (parent == null)
				{
					proxy = null;
					return false;
				}

				if (TryGetCanvasSizeUpdateJob(parent.Children, canvasSizeInBlocks, out proxy))
				{
					return true;
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

		// TODO: Finish the CreateCopy method on the JobTree class.
		public IJobTree CreateCopy()
		{
			return new JobTree(GetJobs().ToList());
		}

		public void SaveJobs(ObjectId projectId, IProjectAdapter projectAdapter)
		{
			_jobsLock.EnterReadLock();

			try
			{
				// TODO: Walk the tree
				var unSavedJobs = GetJobs(_root).Where(x => !x.OnFile).ToList();

				foreach (var job in unSavedJobs)
				{
					job.ProjectId = projectId;
					projectAdapter.InsertJob(job);
				}

				var dirtyJobs = GetJobs(_root).Where(x => x.IsDirty).ToList();

				foreach (var job in dirtyJobs)
				{
					projectAdapter.UpdateJobDetails(job);
				}
			}
			finally
			{
				_jobsLock.ExitReadLock();
			}
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

		#endregion

		#region Load Methods

		private JobTreeItem BuildTree(IList<Job> jobs, out List<JobTreeItem>? path)
		{
			var result = new JobTreeItem();

			//if (jobs == null || jobs.Count == 0)
			if (jobs.Count == 0)
			{
				throw new ArgumentException("jobs cannot be null", nameof(jobs));
				//path = null;
			}
			else
			{
				var visited = 0;
				LoadChildItemsRecurse(jobs, null, result, ref visited);
				if (visited != jobs.Count)
				{
					Debug.WriteLine("Not all jobs were included.");
				}

				_ = MoveCurrentTo(jobs[0], result, out path);
			}

			return result;
		}

		private void LoadChildItemsRecurse(IList<Job> jobs, ObjectId? parentJobId, JobTreeItem jobTreeItem, ref int visited)
		{
			var childJobs = GetChildren(jobs, parentJobId);
			foreach (var job in childJobs)
			{
				var jobTreeItemChild = new JobTreeItem(job);
				jobTreeItem.Children.Add(jobTreeItemChild);
				visited++;
				LoadChildItemsRecurse(jobs, job.Id, jobTreeItemChild, ref visited);
			}
		}

		private IList<Job> GetChildren(IList<Job> jobs, ObjectId? parentJobId)
		{
			var result = jobs.Where(x => x.ParentJobId == parentJobId).OrderBy(x => x.Id.Timestamp).ToList();
			return result;
		}

		#endregion

		#region Collection Methods

		private List<JobTreeItem> AddJob(JobTreeItem newNode, List<JobTreeItem>? path)
		{
			if (path == null || path.Count < 1)
			{
				_root.Children.Add(newNode);
				var result = new List<JobTreeItem> { newNode };

				return result;
			}
			else
			{
				path[^1].Children.Add(newNode);
				var result = new List<JobTreeItem>(path)
				{
					newNode
				};

				return result;
			}
		}

		private bool TryFindJobTreeParent(Job job, [MaybeNullWhen(false)] out List<JobTreeItem> path)
		{
			if (job.ParentJobId == null)
			{
				path = new List<JobTreeItem> { _root };
				return true;
			}
			else
			{
				return TryFindJobTreeItem(job.ParentJobId.Value, _root, out path);
			}
		}

		private bool TryFindJobTreeItem(ObjectId jobId, JobTreeItem jobTreeItem, [MaybeNullWhen(false)] out List<JobTreeItem> path)
		{
			var foundNode = jobTreeItem.Children.FirstOrDefault(x => x.Job.Id == jobId);

			if (foundNode != null)
			{
				path = new List<JobTreeItem> { foundNode };
				return true;
			}
			else
			{
				foreach (var child in jobTreeItem.Children)
				{
					if (TryFindJobTreeItem(jobId, child, out var localPath))
					{
						path = new List<JobTreeItem> { child };
						path.AddRange(localPath);
						return true;
					}
				}

				path = null;
				return false;
			}
		}

		private bool TryFindJob(ObjectId jobId, JobTreeItem jobTreeItem, [MaybeNullWhen(false)] out Job job)
		{
			var foundNode = jobTreeItem.Children.FirstOrDefault(x => x.Job.Id == jobId);

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

		private bool MoveCurrentTo(Job? job, JobTreeItem jobTreeItem, [MaybeNullWhen(false)] out List<JobTreeItem> path)
		{
			if (job == null)
			{
				path = null;
				return false;
			}

			if (TryFindJobTreeItem(job.Id, jobTreeItem, out path))
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

		private void ExpandAndSelect(IList<JobTreeItem> path)
		{
			foreach(var p in path.SkipLast(1))
			{
				p.IsExpanded = true;
			}

			var terminal = GetJobTreeItem(path);
			if (terminal != null)
			{
				terminal.IsExpanded = true;
				terminal.IsSelected = true;
			}
		}

		private JobTreeItem GetJobTreeItem(IList<JobTreeItem> path)
		{
			return path[^1];
		}

		private Job? GetJobFromPath(IList<JobTreeItem>? path)
		{
			return path?[^1].Job;
		}

		private JobTreeItem GetParent(IList<JobTreeItem> path)
		{
			var result = path.Count == 1 ? _root : path[^2];
			return result;
		}

		private JobTreeItem? GetGrandParent(IList<JobTreeItem> path)
		{
			var result = path.Count == 1 ? null : path.Count == 2 ? _root : path[^3];
			return result;
		}

		private IList<Job> GetJobs(JobTreeItem jobTreeItem)
		{
			var result = new List<Job>();

			foreach (var child in jobTreeItem.Children)
			{
				result.Add(child.Job);
				var jobList = GetJobs(child);
				result.AddRange(jobList);
			}

			return result;
		}

		#endregion

		#region Navigate Forward / Backward

		private List<JobTreeItem>? GetNextJobPath(IList<JobTreeItem> path, bool skipPanJobs)
		{
			var currentItem = GetJobTreeItem(path);

			if (currentItem == null)
			{
				return null;
			}

			List<JobTreeItem>? result;

			//if (currentItem.Children.Count > 0)
			//{
			//	var nextJobTreeItem = GetNextJobTreeItem(currentItem.Children, -1, skipPanJobs);
			//	if (nextJobTreeItem == null)
			//	{
			//		nextJobTreeItem = currentItem.Children[0];
			//	}

			//	// The new job will be a child of the current job
			//	result = new List<JobTreeItem>(path)
			//		{
			//			nextJobTreeItem
			//		};

			//	return result;
			//}
			//else
			//{

			var parentJobTreeItem = GetParent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			var nextJobTreeItem = GetNextJobTreeItem(siblings, currentPosition, skipPanJobs);

			if (nextJobTreeItem == null)
			{
			//var children = currentItem.Children;
			//nextJobTreeItem = GetNextJobTreeItem(children, -1, skipPanJobs);
			//if (nextJobTreeItem != null)
			//{
			//	// The new job will be a child of the current job
			//	result = new List<JobTreeItem>(path)
			//	{
			//		nextJobTreeItem
			//	};
			//}
			//else
			//{
			//	return null;
			//}

				return null;
			}
			else
			{
				// The new job will be a sibling of the current job
				result = new List<JobTreeItem>(path.SkipLast(1))
				{
					nextJobTreeItem
				};
			}

			//}

			return result;
		}

		private JobTreeItem? GetNextJobTreeItem(IList<JobTreeItem> jobTreeItems, int currentPosition, bool skipPanJobs)
		{
			JobTreeItem? result;

			if (skipPanJobs)
			{
				result = jobTreeItems.Skip(currentPosition + 1).FirstOrDefault(x => x.Job.TransformType != TransformType.Pan);
			}
			else
			{
				result = jobTreeItems.Skip(currentPosition + 1).FirstOrDefault();
			}

			return result;
		}

		private bool CanMoveForward(IList<JobTreeItem>? path)
		{
			if (path == null)
			{
				return false;
			}

			var currentItem = GetJobTreeItem(path);

			var parentJobTreeItem = GetParent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			//if (currentPosition == children.Count - 1)
			//{
			//	var grandChildren = children[currentPosition].Children;
			//	return grandChildren.Count > 0;
			//}
			//else
			//{
			//	return true;
			//}

			return !(currentPosition == siblings.Count - 1);
		}

		private List<JobTreeItem>? GetPreviousJobPath(IList<JobTreeItem> path, bool skipPanJobs)
		{
			var currentItem = GetJobTreeItem(path);

			if (currentItem == null)
			{
				return null;
			}
			else
			{
				var parentJobTreeItem = GetParent(path);
				var children = parentJobTreeItem.Children;

				var currentPosition = children.IndexOf(currentItem);

				List<JobTreeItem>? result;

				var previousJobTreeItem = GetPreviousJobTreeItem(children, currentPosition, skipPanJobs);

				if (previousJobTreeItem == null)
				{
					if (path.Count > 1)
					{
						var grandParentJobTreeItem = GetParent(path.SkipLast(1).ToList());
						var ancestors = grandParentJobTreeItem.Children;
						previousJobTreeItem = GetPreviousJobTreeItem(ancestors, ancestors.Count, skipPanJobs);
						if (previousJobTreeItem != null)
						{
							result = new List<JobTreeItem>(path.SkipLast(2))
							{
								previousJobTreeItem
							};
						}
						else
						{
							result = null;
						}
					}
					else
					{
						result = null;
					}
				}
				else
				{
					result = new List<JobTreeItem>(path.SkipLast(1))
					{
						previousJobTreeItem
					};
				}

				return result;
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

		private bool CanMoveBack(IList<JobTreeItem>? path)
		{
			if (path == null)
			{
				return false;
			}

			var currentItem = GetJobTreeItem(path);

			var parentJobTreeItem = GetParent(path);
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

		/// <summary>
		/// Finds the sibling job of the given job that has a TransformType of CanvasSizeUpdate
		/// and has the same CanvasSizeInBlocks
		/// </summary>
		/// <param name="job"></param>
		/// <param name="childJob">If successful, the preferred child job of the given job</param>
		/// <returns>True if there is any child of the specified job.</returns>
		private bool TryGetCanvasSizeUpdateJob(IList<JobTreeItem> jobTreeItems, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job childJob)
		{
			var childJobTreeItem = jobTreeItems.FirstOrDefault(x => x.Job.TransformType == TransformType.CanvasSizeUpdate && x.Job.CanvasSizeInBlocks == canvasSizeInBlocks);
			childJob = childJobTreeItem?.Job;
			var result = childJob != null;

			return result;
		}

		/// <summary>
		/// 
		/// If the given job is not the preferred child, return the child of the job's parent that is the preferred child.
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		//private Job? GetPreferredSibling(Job job)
		//{
		//	if (TryFindJobTreeParent(job, out var path))
		//	{
		//		var parentGroup = path[^1];
		//		var result = parentGroup.Children.FirstOrDefault(x => x.Job.IsPreferredChild);
		//		return result?.Job;
		//	}
		//	else
		//	{
		//		// TODO: Consider throwing KeyNotFound exception. The caller should only be asking to locate jobs that exist.
		//		return null;
		//	}
		//}

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

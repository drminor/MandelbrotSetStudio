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
			
			set => DoWithWriteLock(() =>
			{
				if (value != GetJobFromPath(_currentPath))
				{
					var moved = MoveCurrentTo(value, _root, out _currentPath);
					if (!moved && value != null)
					{
						Debug.WriteLine($"WARNING: Could not MoveCurrent to job: {value.Id}.");
					}
				}
			});
		}

		public bool CanGoBack => DoWithReadLock(() => { return CanMoveBack(_currentPath); });

		public bool CanGoForward => DoWithReadLock(() => { return CanMoveForward(_currentPath); });

		public bool AnyJobIsDirty => DoWithReadLock(() => { return GetJobs(_root).Any(x => x.IsDirty); });

		#endregion

		#region Public Methods

		public IReadOnlyCollection<JobTreeItem>? GetCurrentPath()
		{
			return DoWithReadLock(() =>
			{
				return _currentPath == null ? null : new ReadOnlyCollection<JobTreeItem>(_currentPath);
			});
		}

		public IReadOnlyCollection<JobTreeItem>? GetPath(ObjectId jobId)
		{
			return DoWithReadLock(() =>
			{
				var path = GetJobPath(jobId); return path == null ? null : new ReadOnlyCollection<JobTreeItem>(path);
			});
		}

		public void AddHomeJob(Job job)
		{
			_jobsLock.EnterWriteLock();

			try
			{
				List<JobTreeItem>? newPath;

				if (job.ParentJobId != null)
				{
					throw new InvalidOperationException($"Attempting to add a home job, but the job's parentJobId is {job.ParentJobId}.");
				}

				if (job.TransformType != TransformType.Home)
				{
					throw new InvalidOperationException($"Attempting to add a home job, but the job's TransformType is {job.TransformType}.");
				}

				if (_root.Children.Any())
				{
					throw new InvalidOperationException("Attempting to add a job with TransformType = Home to a non-empty tree.");
				}

				var newNode = new JobTreeItem(job);
				newPath = AddJob(newNode, null);

				ExpandAndSelect(newPath);
				_currentPath = newPath;
			}
			finally
			{
				_jobsLock.ExitWriteLock();
			}
		}

		public void Add(Job job, bool selectTheAddedJob)
		{
			_jobsLock.EnterWriteLock();

			try
			{
				List<JobTreeItem>? newPath;

				if (job.ParentJobId == null)
				{
					throw new ArgumentException("When adding a job, the job's ParentJobId must be non-null.");
				}

				if (!TryFindJobTreeItem(job.ParentJobId.Value, _root, out var parentPath))
				{
					throw new InvalidOperationException("Cannot find parent for the job that is being added.");
				}

				var parentJobTreeItem = parentPath[^1];

				if (job.TransformType == TransformType.CanvasSizeUpdate)
				{
					parentJobTreeItem.AddCanvasSizeUpdateJob(job);
					// CanvasSizeUpdates are not selected, just return
					return;
				}

				var newNode = new JobTreeItem(job);

				var grandParentJobTreeItem = GetParent(parentPath);
				var siblings = grandParentJobTreeItem.Children;
				var currentPosition = siblings.IndexOf(parentJobTreeItem);

				if (currentPosition != siblings.Count - 1)
				{
					// Take all items past the current position and add them to an alternate path
					var alts = siblings.Skip(currentPosition + 1).ToList();

					//for(var i = 0; i < alts.Count; i++)
					//{
					//	var child = alts[i];
					//	_ = siblings.Remove(child);
					//	child.Job.ParentJobId = parentJobTreeItem.Job.Id;
					//	parentJobTreeItem.Children.Add(child);
					//}

					var child = alts[0];
					_ = siblings.Remove(child);
					child.Job.ParentJobId = parentJobTreeItem.Job.Id;
					parentJobTreeItem.Children.Add(child);

					var parent = child;

					for (var i = 1; i < alts.Count; i++)
					{
						child = alts[i];
						_ = siblings.Remove(child);
						child.Job.ParentJobId = parent.Job.Id;
						parent.Children.Add(child);
					}
				}

				ObjectId? grandParentJobId = grandParentJobTreeItem.Job.Id;
				job.ParentJobId = grandParentJobId == ObjectId.Empty ? null : grandParentJobId;
				var grandParentPath = GetParentPath(parentPath);

				newPath = AddJob(newNode, grandParentPath);

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

		public bool RestoreBranch(ObjectId jobId)
		{
			Debug.WriteLine($"Restoring Branch: {jobId}.");

			if (!TryFindJobTreeItem(jobId, _root, out var path))
			{
				throw new InvalidOperationException("Cannot find job that is being restored.");
			}

			var jobTreeItem = path[^1];
			var parentPath = GetParentPath(path);

			if (parentPath == null)
			{
				throw new InvalidOperationException("Cannot restore a branch that is already at the top level.");
			}

			var parentJobTreeItem = parentPath[^1];

			var grandParentJobTreeItem = GetParent(parentPath);
			var siblings = grandParentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(parentJobTreeItem);

			Debug.Assert(currentPosition != siblings.Count - 1, "During RestoreBranch we found a JobTreeItem whose parent is the last child.");

			// Take all items past the current position of the grandparent and add them to an alternate path
			var alts = siblings.Skip(currentPosition + 1).ToList();

			var child = alts[0];
			_ = siblings.Remove(child);
			child.Job.ParentJobId = parentJobTreeItem.Job.Id;
			parentJobTreeItem.Children.Add(child);

			var parent = child;

			for (var i = 1; i < alts.Count; i++)
			{
				child = alts[i];
				_ = siblings.Remove(child);
				child.Job.ParentJobId = parent.Job.Id;
				parent.Children.Add(child);
			}

			ObjectId? grandParentJobId = grandParentJobTreeItem.Job.Id == ObjectId.Empty ? null : grandParentJobTreeItem.Job.Id;

			_ = parentJobTreeItem.Children.Remove(jobTreeItem);
			jobTreeItem.Job.ParentJobId = grandParentJobId == ObjectId.Empty ? null : grandParentJobId;
			siblings.Add(jobTreeItem);

			var ourChildren = jobTreeItem.Children.ToList();

			for (var i = 0; i < ourChildren.Count; i++)
			{
				child = ourChildren[i];
				_ = jobTreeItem.Children.Remove(child);
				child.Job.ParentJobId = grandParentJobId;
				siblings.Add(child);
			}

			return true;
		}

		public bool DeleteBranch(ObjectId jobId)
		{
			Debug.WriteLine($"Deleting Branch: {jobId}.");
			return true;
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
				JobTreeItem parent;

				if (job.TransformType == TransformType.CanvasSizeUpdate)
				{
					if (job.ParentJobId == null)
					{
						throw new ArgumentException("Cannot Get CanvasSizeUpdateProxy from a job with transformType = CanvasSizeUpdate and having a null ParentJobId.");
					}

					if (TryFindJobTreeItem(job.ParentJobId.Value, _root, out var path))
					{
						parent = path[^1];
					}
					else
					{
						proxy = null;
						return false;
					}
				}
				else
				{
					if (TryFindJobTreeItem(job.Id, _root, out var path))
					{
						parent = path[^1];
					}
					else
					{
						proxy = null;
						return false;
					}
				}

				proxy = parent.AlternateDispSizes?.FirstOrDefault(x => x.CanvasSizeInBlocks == canvasSizeInBlocks);

				return proxy != null;
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
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

			if (!jobs.Any())
			{
				throw new ArgumentException("When building a JobTree the list of jobs cannot be empty", nameof(jobs));
			}

			var visited = 0;
			LoadChildItems(jobs, null, result, ref visited);
			if (visited != jobs.Count)
			{
				Debug.WriteLine("Not all jobs were included.");
			}

			_ = MoveCurrentTo(jobs[0], result, out path);

			return result;
		}

		private void LoadChildItems(IList<Job> jobs, ObjectId? parentJobId, JobTreeItem jobTreeItem, ref int visited)
		{
			var childJobs = GetChildren(jobs, parentJobId);
			foreach (var job in childJobs)
			{
				var jobTreeItemChild = new JobTreeItem(job);

				if (job.TransformType == TransformType.CanvasSizeUpdate)
				{
					jobTreeItem.AddCanvasSizeUpdateJob(job);
				}
				else
				{
					jobTreeItem.Children.Add(jobTreeItemChild);
				}

				visited++;
				LoadChildItems(jobs, job.Id, jobTreeItemChild, ref visited);
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
			if (path == null || !path.Any())
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

		private List<JobTreeItem>? GetJobPath(ObjectId jobId)
		{
			return TryFindJobTreeItem(jobId, _root, out var path) ? path : null;
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
				//terminal.IsExpanded = true;
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

		private List<JobTreeItem>? GetParentPath(IList<JobTreeItem> path)
		{
			var result = path.Count == 1 ? null : path.SkipLast(1).ToList();
			return result;
		}

		private IList<Job> GetJobs(JobTreeItem jobTreeItem)
		{
			// TODO: Consider implementing an IEnumerator<JobTreeItem> for the JobTree class.
			var result = new List<Job>();

			foreach (var child in jobTreeItem.Children)
			{
				result.Add(child.Job);

				if (child.AlternateDispSizes != null)
				{
					result.AddRange(child.AlternateDispSizes);
				}

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

			var parentJobTreeItem = GetParent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);

			var nextJobTreeItem = GetNextJobTreeItem(siblings, currentPosition, skipPanJobs);

			if (nextJobTreeItem == null)
			{
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

			return !(currentPosition == siblings.Count - 1);
		}

		private List<JobTreeItem>? GetPreviousJobPath(IList<JobTreeItem> path, bool skipPanJobs)
		{
			var currentItem = GetJobTreeItem(path);

			if (currentItem == null)
			{
				return null;
			}

			List<JobTreeItem>? result;
			var newBasePath = new List<JobTreeItem>(path);

			var parentJobTreeItem = GetParent(path);
			var siblings = parentJobTreeItem.Children;
			var currentPosition = siblings.IndexOf(currentItem);
			var previousJobTreeItem = GetPreviousJobTreeItem(siblings, currentPosition, skipPanJobs);

			while (previousJobTreeItem == null && newBasePath.Count > 1)
			{
				newBasePath = newBasePath.SkipLast(1).ToList();
				currentItem = GetJobTreeItem(newBasePath);

				var grandParentJobTreeItem = GetParent(newBasePath);
				var ancestors = grandParentJobTreeItem.Children;
				currentPosition = ancestors.IndexOf(currentItem);
				previousJobTreeItem = GetPreviousJobTreeItem(ancestors, currentPosition + 1, skipPanJobs);
			}

			if (previousJobTreeItem != null)
			{
				result = new List<JobTreeItem>(newBasePath.SkipLast(1).ToList())
				{
					previousJobTreeItem
				};

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

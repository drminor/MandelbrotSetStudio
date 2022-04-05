using MongoDB.Bson;
using MSetRepo;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSetExplorer
{
	public class JobCollection
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly Collection<Job> _jobsCollection;
		private readonly ReaderWriterLockSlim _jobsLock;

		private int _jobsPointer;

		#region Constructor

		public JobCollection(ProjectAdapter projectAdapter)
		{
			_projectAdapter = projectAdapter;
			_jobsCollection = new ObservableCollection<Job>();
			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
			_jobsPointer = -1;
		}

		#endregion

		#region Public Properties

		public Job? CurrentJob => DoWithReadLock(() => { return _jobsPointer == -1 ? null : _jobsCollection[_jobsPointer]; });
		public bool CanGoBack => !(CurrentJob?.ParentJob is null);
		public bool CanGoForward => DoWithReadLock(() => { return TryGetNextJobInStack(_jobsPointer, out var _); });

		public int? CurrentIndex => DoWithReadLock<int?>(() => { return _jobsPointer == -1 ? null : _jobsPointer; });

		//public IEnumerable<Job> Jobs => DoWithReadLock(() => { return new ReadOnlyCollection<Job>(_jobsCollection); });

		// TODO: Use the Last Saved Time to determine
		//public bool IsDirty => _colorsCollection.Any(x => !x.OnFile);

		#endregion

		#region Public Methods

		public void UpdateItem(int index, Job job)
		{
			DoWithWriteLock(() => { _jobsCollection[index] = job; });
		}

		public bool MoveCurrentTo(int index)
		{
			_jobsLock.EnterUpgradeableReadLock();

			try
			{
				if (index < 0 || index > _jobsCollection.Count - 1)
				{
					return false;
				}
				else
				{
					DoWithWriteLock(() => { _jobsPointer = index; });
					return true;
				}
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		public void Load(IEnumerable<Job> jobs, ObjectId? currentId)
		{
			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();

				foreach (var job in jobs)
				{
					_jobsCollection.Add(job);
				}

				if (currentId.HasValue)
				{
					var job = _jobsCollection.FirstOrDefault(x => x.Id == currentId.Value);
					if (job != null)
					{
						var idx = _jobsCollection.IndexOf(job);
						_jobsPointer = idx;
					}
					else
					{
						_jobsPointer = _jobsCollection.Count - 1;
					}
				}
				else
				{
					_jobsPointer = _jobsCollection.Count - 1;
				}
			});
		}

		public void Push(Job? job)
		{
			DoWithWriteLock(() =>
			{
				if (job != null)
				{
					_jobsCollection.Add(job);
				}
				_jobsPointer = _jobsCollection.Count - 1;
			});
		}

		public void Save(Project project)
		{
			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(project.Id);

			DoWithWriteLock(() =>
			{
				for (var i = 0; i < _jobsCollection.Count; i++)
				{
					var job = _jobsCollection[i];
					if (job.Id.CreationTime > lastSavedTime)
					{
						job.Project = project;
						var updatedJob = _projectAdapter.InsertJob(job);
						_jobsCollection[i] = updatedJob;
						UpdateJobParents(job.Id, updatedJob);
					}
					else
					{
						if (job.IsDirty)
						{
							_projectAdapter.UpdateJobDetalis(job);
							job.IsDirty = false;
						}
					}
				}
			});
		}

		public void Clear()
		{
			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();
				_jobsPointer = -1;
			});
		}

		public bool GoBack()
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				var parentJob = CurrentJob?.ParentJob;

				if (!(parentJob is null))
				{
					if (TryFindByJobId(parentJob.Id, out var job))
					{
						var jobIndex = _jobsCollection.IndexOf(job);
						DoWithWriteLock(() => UpdateJobsPtr(jobIndex));
						return true;
					}
				}

				return false;
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		public bool GoForward()
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				if (TryGetNextJobInStack(_jobsPointer, out var nextJobIndex))
				{
					DoWithWriteLock(() => UpdateJobsPtr(nextJobIndex));
					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		#endregion

		#region Private Methods

		private void UpdateJobsPtr(int newJobIndex)
		{
			if (newJobIndex < 0 || newJobIndex > _jobsCollection.Count - 1)
			{
				throw new ArgumentException($"The newJobIndex with value: {newJobIndex} is not valid.", nameof(newJobIndex));
			}

			_jobsPointer = newJobIndex;
		}

		private void UpdateJobParents(ObjectId oldParentId, Job newParentJob)
		{
			foreach (var job in _jobsCollection)
			{
				if (job?.ParentJob?.Id == oldParentId)
				{
					job.ParentJob = newParentJob;
					_projectAdapter.UpdateJobsParent(job);
				}
			}
		}

		#endregion

		#region Job Collection Management 

		private bool TryGetNextJobInStack(int jobIndex, out int nextJobIndex)
		{
			nextJobIndex = -1;

			if (TryGetJobFromStack(jobIndex, out var job))
			{
				if (TryGetLatestChildJobIndex(job, out var childJobIndex))
				{
					nextJobIndex = childJobIndex;
					return true;
				}
			}

			return false;
		}

		private bool TryGetJobFromStack(int jobIndex, [MaybeNullWhen(false)] out Job job)
		{
			if (jobIndex < 0 || jobIndex > _jobsCollection.Count - 1)
			{
				job = null;
				return false;
			}
			else
			{
				job = _jobsCollection[jobIndex];
				return true;
			}
		}

		/// <summary>
		/// Finds the most recently ran child job of the given parentJob.
		/// </summary>
		/// <param name="parentJob"></param>
		/// <param name="childJobIndex">If successful, the index of the most recent child job of the given parentJob</param>
		/// <returns>True if there is any child of the specified job.</returns>
		private bool TryGetLatestChildJobIndex(Job parentJob, out int childJobIndex)
		{
			childJobIndex = -1;
			var lastestDtFound = DateTime.MinValue;

			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];
				var thisParentJobId = job.ParentJob?.Id ?? ObjectId.Empty;

				if (thisParentJobId.Equals(parentJob.Id))
				{
					var dt = thisParentJobId.CreationTime;
					if (dt > lastestDtFound)
					{
						childJobIndex = i;
						lastestDtFound = dt;
					}
				}
			}

			var result = childJobIndex != -1;
			return result;
		}

		private bool TryFindByJobId(ObjectId id, [MaybeNullWhen(false)] out Job job)
		{
			job = _jobsCollection.FirstOrDefault(x => x.Id == id);
			return job != null;
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

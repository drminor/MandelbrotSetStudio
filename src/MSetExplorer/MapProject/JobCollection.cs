using MongoDB.Bson;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSetExplorer
{
	public class JobCollection
	{
		private readonly Collection<Job> _jobsCollection;
		private readonly ReaderWriterLockSlim _jobsLock;

		private int _jobsPointer;

		#region Constructor

		public JobCollection()
		{
			_jobsCollection = new Collection<Job>() {Job.Empty };
			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
			_jobsPointer = 0;
		}

		#endregion

		#region Public Properties

		public Job CurrentJob => DoWithReadLock(() => { return  _jobsCollection[_jobsPointer]; });
		public bool CanGoBack => !(CurrentJob?.ParentJobId is null);
		public bool CanGoForward => DoWithReadLock(() => { return TryGetNextJobInStack(_jobsPointer, out var _); });

		public int CurrentIndex => DoWithReadLock(() => { return _jobsPointer; });
		public int Count => DoWithReadLock(() => { return _jobsCollection.Count; });

		//public IEnumerable<Job> Jobs => DoWithReadLock(() => { return new ReadOnlyCollection<Job>(_jobsCollection); });

		//public bool IsDirty => _colorsCollection.Any(x => !x.OnFile);

		#endregion

		#region Public Methods

		public Job this[int index]
		{
			get => DoWithReadLock(() => { return _jobsCollection[index]; });
			set => DoWithWriteLock(() => { _jobsCollection[index] = value; });
		}

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

		public void Load(Job job)
		{
			var jobs = new List<Job>() { job };
			Load(jobs, null);
		}

		public bool Load(IEnumerable<Job> jobs, ObjectId? currentId)
		{
			var result = true;

			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();

				foreach (var job in jobs)
				{
					_jobsCollection.Add(job);
				}

				if (_jobsCollection.Count == 0)
				{
					_jobsCollection.Add(new Job());
				}

				if (currentId.HasValue)
				{
					if (TryFindByJobId(currentId.Value, out var job))
					{
						var idx = _jobsCollection.IndexOf(job);
						_jobsPointer = idx;
					}
					else
					{
						Debug.WriteLine($"WARNING: There is no Job with Id: {currentId} in the list of Jobs being loaded into the JobCollection.");
						_jobsPointer = _jobsCollection.Count - 1;
						result = false;
					}
				}
				else
				{
					_jobsPointer = _jobsCollection.Count - 1;
				}
			});

			if (!CheckJobStackIntegrity(out var reason))
			{
				Debug.WriteLine($"Job Collection is not integral. Reason: {reason}");
			}

			return result;
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

		public void Clear()
		{
			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();
				_jobsCollection.Add(new Job());
				_jobsPointer = 0;
			});
		}

		public bool GoBack()
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				var parentJobId = CurrentJob?.ParentJobId;

				if (!(parentJobId is null))
				{
					if (TryFindByJobId(parentJobId.Value, out var job))
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

		#endregion

		#region Collection Management 

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
				var thisParentJobId = job.ParentJobId ?? ObjectId.Empty;

				if (thisParentJobId.Equals(parentJob.Id))
				{
					var dt = job.DateCreated;
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

		private bool CheckJobStackIntegrity(out List<string> reasons)
		{
			reasons = new List<string>();

			var allFoundParentIds = new List<ObjectId>();

			_jobsLock.EnterReadLock();

			try
			{
				foreach (var job in _jobsCollection)
				{
					if (job.ParentJobId.HasValue)
					{
						var parentId = job.ParentJobId.Value;
						if (!TryFindByJobId(parentId, out var _))
						{
							reasons.Add($"Job with Id: {job.Id}, ParentId: {job.ParentJobId} exists, but there is no Job with Id: {job.ParentJobId}.");
						}
						else
						{
							if (job.IsPreferredChild)
							{
								if (!allFoundParentIds.Contains(parentId))
								{
									allFoundParentIds.Add(parentId);
								}
								else
								{
									reasons.Add($"Parent Job with Id: {parentId} has more than one child whose IsPreferredChild is true. {job.Id}, being one of them.");
									//return false;
								}
							}
						}
					}
				}

				var allParentIds = GetUniqueJobParentIds(_jobsCollection);

				var allUnmatched = allParentIds.Except(allFoundParentIds);

				//foreach(var t in allFoundParentIds)
				//{
				//	allParentIds.Remove(t);
				//}

				foreach(var s in allUnmatched)
				{
					reasons.Add($"Parent Job with Id: {s} has no preferred child.");
				}

				return reasons.Count == 0;
			}
			finally
			{
				_jobsLock.ExitReadLock();
			}
		}

		private IEnumerable<ObjectId> GetUniqueJobParentIds(IEnumerable<Job> jobs)
		{
			var result = jobs.Select(x => x.ParentJobId).Where(x => x.HasValue).Cast<ObjectId>(); //.Distinct();
			return result;
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

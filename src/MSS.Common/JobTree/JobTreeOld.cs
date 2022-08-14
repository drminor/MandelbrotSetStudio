using MongoDB.Bson;
using MSS.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
//using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace MSS.Types.MSet
{
	public class JobTreeOld : IJobTree
	{
		private readonly Collection<Job> _jobsCollection;
		private readonly ReaderWriterLockSlim _jobsLock;

		private int _jobsPointer;

		#region Constructor

		public JobTreeOld(IEnumerable<Job> jobs)
		{
			_jobsCollection = new Collection<Job>(jobs.ToList());
			_jobsPointer = _jobsCollection.Count - 1;

			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		#endregion

		#region Public Properties

		public Job CurrentJob
		{
			get => DoWithReadLock(() => { return _jobsPointer == -1 ? _jobsCollection[0] : _jobsCollection[_jobsPointer]; });
			set
			{
				_jobsLock.EnterUpgradeableReadLock();

				try
				{
					if (value == null)
					{
						DoWithWriteLock(() => { _jobsPointer = -1; });
					}
					else if (TryGetIndexFromId(value.Id, out var index))
					{
						DoWithWriteLock(() => { _jobsPointer = index; });
					}
					else
					{
						DoWithWriteLock(() => { _jobsPointer = -1; });
						Debug.WriteLine($"Could not set the current job to {value.Id}.");
					}
				}
				finally
				{
					_jobsLock.ExitUpgradeableReadLock();
				}
			}
		}

		public bool CanGoBack => !(CurrentJob?.ParentJobId is null);
		public bool CanGoForward => DoWithReadLock(() => { return HaveNextJobInStack(_jobsPointer); });

		//public int CurrentIndex => DoWithReadLock(() => { return _jobsPointer; });
		public int Count => DoWithReadLock(() => { return _jobsCollection.Count; });

		// TODO: Have the old JobTree implement JobItems
		public ObservableCollection<JobTreeItem> JobItems => throw new NotImplementedException();

		public IEnumerable<Job> GetJobs() => DoWithReadLock(() => { return new ReadOnlyCollection<Job>(_jobsCollection); });

		public bool AnyJobIsDirty => GetJobs().Any(x => x.IsDirty);


		#endregion

		#region Public Methods

		public Job this[int index]
		{
			get => DoWithReadLock(() => { return _jobsCollection[index]; });
			set => DoWithWriteLock(() => { _jobsCollection[index] = value; });
		}

		public Job? GetJob(ObjectId jobId)
		{
			_ = TryFindByJobId(jobId, out var job);
			return job;
		}

		public Job? GetParent(Job job)
		{
			if (job.ParentJobId == null)
			{
				return null;
			}

			_ = TryFindByJobId(job.ParentJobId.Value, out var result);
			return result;
		}

		//public void UpdateItem(int index, Job job)
		//{
		//	DoWithWriteLock(() => { _jobsCollection[index] = job; });
		//}

		//public bool MoveCurrentTo(Job job)
		//{
		//	_jobsLock.EnterUpgradeableReadLock();

		//	try
		//	{
		//		if (TryGetIndexFromId(job.Id, out var index))
		//		{
		//			DoWithWriteLock(() => { _jobsPointer = index; });
		//			return true;
		//		}
		//		else
		//		{
		//			return false;
		//		}
		//	}
		//	finally
		//	{
		//		_jobsLock.ExitUpgradeableReadLock();
		//	}
		//}

		//public bool MoveCurrentTo(ObjectId jobId)
		//{
		//	_jobsLock.EnterUpgradeableReadLock();

		//	try
		//	{
		//		if (TryGetIndexFromId(jobId, out var index))
		//		{
		//			DoWithWriteLock(() => { _jobsPointer = index; });
		//			return true;
		//		}
		//		else
		//		{
		//			return false;
		//		}
		//	}
		//	finally
		//	{
		//		_jobsLock.ExitUpgradeableReadLock();
		//	}
		//}

		//public bool MoveCurrentTo(int index)
		//{
		//	_jobsLock.EnterUpgradeableReadLock();

		//	try
		//	{
		//		if (index < 0 || index > _jobsCollection.Count - 1)
		//		{
		//			return false;
		//		}
		//		else
		//		{
		//			DoWithWriteLock(() => { _jobsPointer = index; });
		//			return true;
		//		}
		//	}
		//	finally
		//	{
		//		_jobsLock.ExitUpgradeableReadLock();
		//	}
		//}

		//public void Load(Job job)
		//{
		//	var jobs = new List<Job>() { job };
		//	Load(jobs);
		//}

		//public bool Load(IEnumerable<Job> jobs)
		//{
		//	var result = true;

		//	DoWithWriteLock(() =>
		//	{
		//		_jobsCollection.Clear();

		//		foreach (var job in jobs)
		//		{
		//			_jobsCollection.Add(job);
		//		}

		//		if (_jobsCollection.Count == 0)
		//		{
		//			_jobsCollection.Add(Job.Empty);
		//		}

		//		_jobsPointer = _jobsCollection.Count - 1;
		//	});

		//	if (!CheckCollectionIntegrity(out var reason))
		//	{
		//		Debug.WriteLine($"Job Collection is not integral. Reason: {reason}");
		//	}

		//	return result;
		//}

		//public void InsertAfter(Job job)
		//{
		//	DoWithWriteLock(() =>
		//	{
		//		job.IsPreferredChild = true;
		//		UpdateSiblings(job);

		//		_jobsCollection.Add(job);

		//		_jobsPointer = _jobsCollection.Count - 1;
		//	});
		//}

		//private void UpdateSiblings(Job newParent)
		//{
		//	var siblings = _jobsCollection.Where(x => x.ParentJobId == newParent.ParentJobId);

		//	foreach (var job in siblings)
		//	{
		//		job.ParentJobId = newParent.Id;
		//	}
		//}

		public void Add(Job job, bool selectAddedJob)
		{
			DoWithWriteLock(() =>
			{
				if (job.IsPreferredChild)
				{
					ResetSiblings(job);
				}

				_jobsCollection.Add(job);

				if (selectAddedJob)
				{
					_jobsPointer = _jobsCollection.Count - 1;
				}
			});
		}

		private void ResetSiblings(Job newSibling)
		{
			var siblings = _jobsCollection.Where(x => x.ParentJobId == newSibling.ParentJobId);

			foreach (var job in siblings)
			{
				job.IsPreferredChild = false;

				//var childJobs = _jobsCollection.Where(c => c.ParentJobId == job.Id);
				//foreach(var childJob in childJobs)
				//{
				//	childJob.ParentJobId = newSibling.Id;
				//}
			}
		}

		//public void Clear()
		//{
		//	DoWithWriteLock(() =>
		//	{
		//		_jobsCollection.Clear();
		//		_jobsCollection.Add(Job.Empty);
		//		_jobsPointer = 0;
		//	});
		//}

		//public bool GoBack()
		//{
		//	_jobsLock.EnterUpgradeableReadLock();
		//	try
		//	{
		//		var parentJobId = CurrentJob?.ParentJobId;

		//		if (!(parentJobId is null))
		//		{
		//			if (TryGetIndexFromId(parentJobId.Value, out var index))
		//			{
		//				DoWithWriteLock(() => UpdateJobsPtr(index));
		//				return true;
		//			}
		//		}

		//		return false;
		//	}
		//	finally
		//	{
		//		_jobsLock.ExitUpgradeableReadLock();
		//	}
		//}

		public bool TryGetPreviousJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job)
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				if (!TryGetJobFromStack(_jobsPointer, out job))
				{
					return false;
				}

				if (skipPanJobs)
				{
					if (AnySiblingHaveTransformTypeOf(job, TransformType.Pan))
					{
						// Move to first "non-pan" job.
						while (true)
						{
							if (!TryGetPreviousJobInStack(job, out var previousJob))
							{
								// return the first job.
								return true;
							}

							job = previousJob;
							if (!AnySiblingHaveTransformTypeOf(job, TransformType.Pan))
							{
								break;
							}
						}
					}

					if (TryGetPreviousJobInStack(job, out var previousJob2))
					{
						// return the job just prior to the found "non-pan" job.
						job = previousJob2;
						return true;
					}
					else
					{
						// return the first job.
						return true;
					}
				}
				else
				{
					var result = TryGetPreviousJobInStack(job, out job);
					return result;
				}
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		public bool MoveBack(bool skipPanJobs)
		{
			return false;
		}

		//public bool GoForward()
		//{
		//	_jobsLock.EnterUpgradeableReadLock();
		//	try
		//	{
		//		if (TryGetNextJobIndexInStack(_jobsPointer, out var nextJobIndex))
		//		{
		//			DoWithWriteLock(() => UpdateJobsPtr(nextJobIndex));
		//			return true;
		//		}
		//		else
		//		{
		//			return false;
		//		}
		//	}
		//	finally
		//	{
		//		_jobsLock.ExitUpgradeableReadLock();
		//	}
		//}

		public bool TryGetNextJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job)
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				if (!TryGetJobFromStack(_jobsPointer, out job))
				{
					return false;
				}

				if (skipPanJobs)
				{
					// Find next with TransformType != Pan
					while (true)
					{
						if (!TryGetNextJobInStack(job, out var nextJob))
						{
							return false;
						}

						job = nextJob;
						if (!AnySiblingHaveTransformTypeOf(job, TransformType.Pan))
						{
							break;
						}
					}

					// Find next with TransformType != Pan or if none, take the last job
					while (true)
					{
						if (!TryGetNextJobInStack(job, out var nextjob))
						{
							// return the last child job
							return true;
						}

						if (!AnySiblingHaveTransformTypeOf(nextjob, TransformType.Pan))
						{
							// return the job just before this "non-pan" job.
							return true;
						}
						else
						{
							job = nextjob;
						}
					}
				}
				else
				{
					var result = TryGetNextJobInStack(job, out job);
					return result;
				}
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		public bool MoveForward(bool skipPanJobs)
		{
			return false;
		}

		public bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy)
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				return TryGetCanvasSizeUpdateSiblingJob(job, canvasSizeInBlocks, out proxy);
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		/// <summary>
		/// If the job given has is not the preferred child, return child of the job's parent this is the preferred child.
		/// </summary>
		/// <param name="job"></param>
		/// <returns></returns>
		public Job? GetPreferredSibling(Job job)
		{
			Job? result;

			if (!job.IsPreferredChild /*&& job.ParentJobId.HasValue*/)
			{
				result = _jobsCollection.FirstOrDefault(x => x.ParentJobId == job.ParentJobId && x.IsPreferredChild);
			}
			else
			{
				result = job;
			}

			return result;
		}

		#endregion

		#region Private Methods

		//private void UpdateJobsPtr(int newJobIndex)
		//{
		//	if (newJobIndex < 0 || newJobIndex > _jobsCollection.Count - 1)
		//	{
		//		throw new ArgumentException($"The newJobIndex with value: {newJobIndex} is not valid.", nameof(newJobIndex));
		//	}

		//	_jobsPointer = newJobIndex;
		//}

		#endregion

		#region Collection Management 

		private bool TryGetNextJobInStack(Job job, [MaybeNullWhen(false)] out Job nextJob)
		{
			if (TryGetPreferredChildJob(job, out var childJob))
			{
				nextJob = childJob;
				return true;
			}
			else
			{
				nextJob = null;
				return false;
			}
		}

		private bool TryGetPreviousJobInStack(Job job, [MaybeNullWhen(false)] out Job previousJob)
		{
			var parentJobId = job.ParentJobId;

			if (parentJobId != null)
			{
				if (TryFindByJobId(parentJobId.Value, out previousJob))
				{
					if (!previousJob.IsPreferredChild)
					{
						previousJob = GetPreferredSibling(previousJob);
					}
					return previousJob != null;
				}
				else
				{
					previousJob = null;
					return false;
				}
			}
			else
			{
				previousJob = null;
				return false;
			}
		}

		//private bool TryGetNextJobIndexInStack(int jobIndex, out int nextJobIndex)
		//{
		//	nextJobIndex = -1;

		//	if (TryGetJobFromStack(jobIndex, out var job))
		//	{
		//		if (TryGetPreferredChildJobIndex(job, out var childJobIndex))
		//		{
		//			nextJobIndex = childJobIndex;
		//			return true;
		//		}
		//	}

		//	return false;
		//}

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

		private bool TryGetPreferredChildJob(Job job, [MaybeNullWhen(false)] out Job childJob)
		{
			var startingJob = GetPreferredSibling(job);
			if (startingJob == null)
			{
				childJob = null;
				return false;
			}
			else
			{
				childJob = _jobsCollection.FirstOrDefault(x => x.ParentJobId == startingJob?.Id && x.IsPreferredChild);

				var result = childJob != null;
				return result;
			}
		}

		private bool AnySiblingHaveTransformTypeOf(Job job, TransformType transformType)
		{
			var result = _jobsCollection.Any(x => x.ParentJobId == job.ParentJobId && x.TransformType == transformType);
			return result;
		}

		/// <summary>
		/// Finds the child job of the given parentJob that is marked as IsPreferred.
		/// If the current node is not the preferred child, then the preferred sibling is used as the current node instead.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="childJobIndex">If successful, the index of the most recent child job of the given parentJob</param>
		/// <returns>True if there is any child of the specified job.</returns>
		//private bool TryGetPreferredChildJobIndex(Job job, out int childJobIndex)
		//{
		//	var startingJob = GetPreferredSibling(job);

		//	childJobIndex = _jobsCollection.Select((value, index) => new { value, index })
		//		.Where(pair => pair.value.ParentJobId == startingJob.Id && pair.value.IsPreferredChild)
		//		.Select(pair => pair.index).DefaultIfEmpty(-1)
		//		.FirstOrDefault();

		//	var result = childJobIndex != -1;
		//	return result;
		//}

		/// <summary>
		/// Finds the sibling job of the given job that has a TransformType of CanvasSizeUpdate.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="childJob">If successful, the preferred child job of the given job</param>
		/// <returns>True if there is any child of the specified job.</returns>
		private bool TryGetCanvasSizeUpdateSiblingJob(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job childJob)
		{
			childJob = _jobsCollection.FirstOrDefault(x => x.ParentJobId == job.ParentJobId && x.TransformType == TransformType.CanvasSizeUpdate && x.CanvasSizeInBlocks == canvasSizeInBlocks);
			var result = childJob != null;

			return result;
		}

		private bool HaveNextJobInStack(int jobIndex)
		{
			if (TryGetJobFromStack(jobIndex, out var job))
			{
				var result = HasLatestChildJob(job);
				return result;
			}

			return false;
		}

		/// <summary>
		/// Finds the child job of the given parentJob that is marked as IsPreferred.
		/// </summary>
		/// <param name="job"></param>
		/// <param name="childJobIndex">If successful, the index of the most recent child job of the given parentJob</param>
		/// <returns>True if there is any child of the specified job.</returns>
		private bool HasLatestChildJob(Job job)
		{
			var startingJob = GetPreferredSibling(job);

			if (startingJob == null)
			{
				return false;
			}
			else
			{
				var result = _jobsCollection.Any(x => x.ParentJobId == startingJob.Id && x.IsPreferredChild);
				return result;
			}
		}

		private bool TryFindByJobId(ObjectId id, [MaybeNullWhen(false)] out Job job)
		{
			job = _jobsCollection.FirstOrDefault(x => x.Id == id);
			return job != null;
		}

		//private Job GetParentJob(Job job)
		//{
		//	if (!job.ParentJobId.HasValue)
		//	{
		//		throw new InvalidOperationException($"Attempting to retreive the parent job for job: {job.Id}, but this job's ParentJobId is null.");
		//	}

		//	if (TryFindByJobId(job.ParentJobId.Value, out var parentJob))
		//	{
		//		return parentJob;
		//	}
		//	else
		//	{
		//		throw new InvalidOperationException($"Attempting to retreive the parent job for job: {job.Id}, but no job with this job's ParentJobId of {job.ParentJobId.Value} exists in the JobCollection.");
		//	}
		//}

		private bool TryGetIndexFromId(ObjectId id, out int index)
		{
			var job = _jobsCollection.FirstOrDefault(x => x.Id == id);
			if (job != null)
			{
				index = _jobsCollection.IndexOf(job);
			}
			else
			{
				index = -1;
			}

			return job != null;
		}

		private bool CheckCollectionIntegrity(out List<string> reasons)
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
								}
							}
						}
					}
				}

				var allParentIds = GetAllParentIds(_jobsCollection);
				var allUnmatched = allParentIds.Except(allFoundParentIds);

				foreach (var s in allUnmatched)
				{
					reasons.Add($"Parent Job with Id: {s} has no preferred child.");
				}

				var homeCnt = _jobsCollection.Count(x => !x.ParentJobId.HasValue);
				if (homeCnt == 0)
				{
					reasons.Add("Collection has no home job.");
				}

				if (homeCnt > 1)
				{
					reasons.Add("Collection has more than one home job.");
				}

				return reasons.Count == 0;
			}
			finally
			{
				_jobsLock.ExitReadLock();
			}
		}

		private IEnumerable<ObjectId> GetAllParentIds(IEnumerable<Job> jobs)
		{
			var result = jobs.Select(x => x.ParentJobId).Where(x => x.HasValue).Cast<ObjectId>();
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

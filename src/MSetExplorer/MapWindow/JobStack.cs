﻿using MongoDB.Bson;
using MSS.Types.MSet;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using MSetRepo;
using System.Diagnostics;
using MSS.Common;

namespace MSetExplorer
{
	internal class JobStack : ViewModelBase, IJobStack, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly ObservableCollection<Job> _jobsCollection;
		private int _jobsPointer;
		private readonly ReaderWriterLockSlim _jobsLock;

		#region Constructor

		public JobStack(ProjectAdapter projectAdapter, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			BlockSize = blockSize;
			_jobsCollection = new ObservableCollection<Job>();
			_jobsPointer = -1;
			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public event EventHandler CurrentJobChanged;

		public SizeInt BlockSize { get; }
		public SizeInt CanvasSize { get; set; }
		public Project Project { get; private set; }

		public Job CurrentJob => DoWithReadLock(() => { return _jobsPointer == -1 ? null : _jobsCollection[_jobsPointer]; });
		public bool CanGoBack => !(CurrentJob?.ParentJob is null);
		public bool CanGoForward => DoWithReadLock(() => { return TryGetNextJobInStack(_jobsPointer, out var _); });

		public IEnumerable<Job> Jobs => DoWithReadLock(() => { return new ReadOnlyCollection<Job>(_jobsCollection); });

		#endregion

		#region Public Methods

		public void LoadNewProject(string projectName, MSetInfo mSetInfo)
		{
			Project = _projectAdapter.GetOrCreateProject(projectName);

			var newArea = new RectangleInt(new PointInt(), CanvasSize);
			LoadMap(mSetInfo, TransformType.None, newArea);
		}

		public void LoadProject(string projectName)
		{
			var jobs = _projectAdapter.GetAllJobs(Project.Id);

			DoWithWriteLock(() =>
			{
				foreach (var job in jobs)
				{
					_jobsCollection.Add(job);
				}

				_jobsPointer = _jobsCollection.Count - 1;

				Rerun(_jobsPointer);
			});
		}

		public void SaveProject()
		{
			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(Project.Id);

			foreach (var job in Jobs)
			{
				if (job.Id.CreationTime > lastSavedTime)
				{
					var updatedJob = _projectAdapter.InsertJob(job);
					UpdateJob(job, updatedJob);
				}
			}
		}

		public void UpdateJob(Job oldJob, Job newJob)
		{
			DoWithWriteLock(() =>
			{
				if (TryFindByJobId(oldJob.Id, out var foundJob))
				{
					var idx = _jobsCollection.IndexOf(foundJob);
					_jobsCollection[idx] = newJob;

					foreach (var job in _jobsCollection)
					{
						if (job?.ParentJob?.Id == oldJob.Id)
						{
							job.ParentJob = newJob;
						}
					}
				}
				else
				{
					throw new KeyNotFoundException("The old job could not be found.");
				}
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
					_jobsLock.EnterWriteLock();
					try
					{
						var job = _jobsCollection.FirstOrDefault(x => parentJob.Id == x.Id);
						if (!(job is null))
						{
							var idx = _jobsCollection.IndexOf(job);
							Rerun(idx);
							return true;
						}
					}
					finally
					{
						_jobsLock.ExitWriteLock();
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
				if (TryGetNextJobInStack(_jobsPointer, out var nextRequestStackPointer))
				{
					_jobsLock.EnterWriteLock();
					try
					{
						Rerun(nextRequestStackPointer);
						return true;
					}
					finally
					{
						_jobsLock.ExitWriteLock();
					}
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

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			var curJob = CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);

			var updatedInfo = MSetInfo.UpdateWithNewCoords(curJob.MSetInfo, coords);

			//if (Iterations > 0 && Iterations != updatedInfo.MapCalcSettings.MaxIterations)
			//{
			//	updatedInfo = MSetInfo.UpdateWithNewIterations(updatedInfo, Iterations, Steps);
			//}

			LoadMap(updatedInfo, transformType, newArea);
		}

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea)
		{
			var parentJob = CurrentJob;
			var jobName = MapWindowHelper.GetJobName(transformType);
			var job = MapWindowHelper.BuildJob(parentJob, Project, jobName, CanvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);

			//Job job = null;

			Debug.WriteLine($"Starting Job with new coords: {mSetInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			Push(job);
		}

		private void Push(Job job)
		{
			DoWithWriteLock(() =>
			{
				CheckForDuplicateJob(job.Id);

				_jobsCollection.Add(job);
				_jobsPointer = _jobsCollection.Count - 1;

				CurrentJobChanged?.Invoke(this, new EventArgs());
				OnPropertyChanged(nameof(CanGoBack));
				OnPropertyChanged(nameof(CanGoForward));
			});
		}

		private void Rerun(int newJobsCollectionPointer)
		{
			if (newJobsCollectionPointer < 0 || newJobsCollectionPointer > _jobsCollection.Count - 1)
			{
				throw new ArgumentException($"The newJobsCollectionPointer with value: {newJobsCollectionPointer} is not valid.", nameof(newJobsCollectionPointer));
			}

			var job = _jobsCollection[newJobsCollectionPointer];
			_jobsPointer = newJobsCollectionPointer;
			CurrentJobChanged?.Invoke(this, new EventArgs());
			OnPropertyChanged(nameof(CanGoBack));
			OnPropertyChanged(nameof(CanGoForward));
		}

		#endregion

		#region Job Collection Management 

		private bool TryGetNextJobInStack(int requestStackPointer, out int nextRequestStackPointer)
		{
			nextRequestStackPointer = -1;

			if (TryGetJobFromStack(requestStackPointer, out var job))
			{
				if (TryGetLatestChildJobIndex(job, out var childJobRequestStackPointer))
				{
					nextRequestStackPointer = childJobRequestStackPointer;
					return true;
				}
			}

			return false;
		}

		private bool TryGetJobFromStack(int requestStackPointer, out Job job)
		{
			if (requestStackPointer < 0 || requestStackPointer > _jobsCollection.Count - 1)
			{
				job = null;
				return false;
			}
			else
			{
				job = _jobsCollection[requestStackPointer];
				return true;
			}
		}

		/// <summary>
		/// Finds the most recently ran child job of the given parentJob.
		/// </summary>
		/// <param name="parentJob"></param>
		/// <param name="requestStackPointer">If successful, the index of the most recent child job of the given parentJob</param>
		/// <returns>True if there is any child of the specified job.</returns>
		private bool TryGetLatestChildJobIndex(Job parentJob, out int requestStackPointer)
		{
			requestStackPointer = -1;
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
						requestStackPointer = i;
						lastestDtFound = dt;
					}
				}
			}

			var result = requestStackPointer != -1;
			return result;
		}

		private void CheckForDuplicateJob(ObjectId id)
		{
			if (_jobsCollection.Any(x => x.Id == id))
			{
				throw new InvalidOperationException($"A job with id: {id} has already been pushed.");
			}
		}

		private bool TryFindByJobId(ObjectId id, out Job job)
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

		private bool _disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					_jobsLock.Dispose();
				}

				_disposedValue = true;
			}
		}


		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}

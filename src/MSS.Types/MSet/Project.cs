﻿using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MSS.Types.MSet
{
	public class Project : IDisposable, INotifyPropertyChanged
	{
		private string _name;
		private string? _description;

		private readonly JobCollection _jobsCollection;
		private readonly ColorBandSetCollection _colorBandSetCollection;

		private readonly ReaderWriterLockSlim _stateLock;

		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		private ObjectId _originalCurrentJobId;

		#region Constructor

		public Project(string name, string? description, IEnumerable<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId) 
			: this(ObjectId.GenerateNewId(), name, description, jobs, colorBandSets, currentJobId, DateTime.MinValue)
		{
			OnFile = false;
		}

		public Project(ObjectId id, string name, string? description, IEnumerable<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId, DateTime lastSavedUtc)
		{
			Id = id;

 			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			_jobsCollection = new JobCollection(jobs);
			_colorBandSetCollection = new ColorBandSetCollection(colorBandSets);
			_stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			_originalCurrentJobId = currentJobId;
			LastUpdatedUtc = DateTime.MinValue;
			LastSavedUtc = lastSavedUtc;

			var currentJob = _jobsCollection.GetJobs().FirstOrDefault(x => x.Id == currentJobId);

			if (currentJob != null)
			{
				CurrentJob = currentJob;
			}
			else
			{
				LastUpdatedUtc = DateTime.UtcNow;
				Debug.WriteLine($"Warning the Project a CurrentJobId of {Id}, but this job cannot be found. Setting the current job to be the last job.");
			}

			Debug.WriteLine($"Loading ColorBandSet: {CurrentJob.ColorBandSetId} as projects is being constructed.");
			var colorBandSetId = LoadColorBandSetForJob(CurrentJob.ColorBandSetId);
			if (CurrentJob.ColorBandSetId != colorBandSetId)
			{
				CurrentJob.ColorBandSetId = colorBandSetId;
				LastUpdatedUtc = DateTime.UtcNow;
			}

			Debug.WriteLine($"Project is loaded. CurrentJobId: {_jobsCollection.CurrentJob.Id}, Current ColorBandSetId: {_colorBandSetCollection.CurrentColorBandSet.Id}. IsDirty = {IsDirty}");
		}

		#endregion

		#region Public Properties

		public DateTime DateCreated => Id == ObjectId.Empty ? LastSavedUtc : Id.CreationTime;

		public bool CanGoBack => _jobsCollection.CanGoBack;
		public bool CanGoForward => _jobsCollection.CanGoForward;

		public bool AnyJobIsDirty => _jobsCollection.GetJobs().Any(x => x.IsDirty);

		public bool IsDirty => LastUpdatedUtc > LastSavedUtc; // || DateCreated > LastSavedUtc;

		public bool IsCurrentJobIdChanged
		{
			get
			{
				var result = CurrentJobId != _originalCurrentJobId;
				return result;
			}
		}

		public IEnumerable<Job> GetJobs() => _jobsCollection.GetJobs();

		public IEnumerable<ColorBandSet> GetColorBandSets() => _colorBandSetCollection.GetColorBandSets();

		public ObjectId Id { get; init; }

		public bool OnFile { get; private set; }

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public string? Description
		{
			get => _description;
			set
			{
				if (_description != value)
				{
					_description = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			private set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
				OnFile = true;
			}
		}

		public DateTime LastUpdatedUtc
		{
			get => _lastUpdatedUtc;

			private set
			{
				var isDirtyBefore = IsDirty;
				_lastUpdatedUtc = value;

				if (IsDirty != isDirtyBefore)
				{
					OnPropertyChanged(nameof(IsDirty));
				}
			}
		}

		public Job CurrentJob
		{
			get => _jobsCollection.CurrentJob;
			set
			{
				if (CurrentJob != value)
				{
					_ = _jobsCollection.MoveCurrentTo(value);
					if (CurrentColorBandSet.Id != CurrentJob.ColorBandSetId)
					{
						Debug.WriteLine($"Loading ColorBandSet: {CurrentJob.ColorBandSetId} as the Current Job is being updated.");
						var colorBandSetId = LoadColorBandSetForJob(CurrentJob.ColorBandSetId);
						if (CurrentJob.ColorBandSetId != colorBandSetId)
						{
							CurrentJob.ColorBandSetId = colorBandSetId;
							LastUpdatedUtc = DateTime.UtcNow;
						}
					}

					OnPropertyChanged();
				}
			}
		}

		public ObjectId CurrentJobId => CurrentJob.Id;

		public ColorBandSet CurrentColorBandSet
		{
			get => _colorBandSetCollection.CurrentColorBandSet;
			set
			{
				if (CurrentColorBandSet != value)
				{
					if (!_colorBandSetCollection.MoveCurrentTo(value))
					{
						// Set the incoming ColorBandSet's ProjectId to this Project's Id.
						value.ProjectId = Id;
						_colorBandSetCollection.Push(value);
					}

					CurrentJob.ColorBandSetId = value.Id;
					LastUpdatedUtc = DateTime.UtcNow;

					OnPropertyChanged(nameof(CurrentColorBandSet));
				}
			}
		}

		#endregion

		#region Public Methods

		public bool Save(IProjectAdapter projectAdapter)
		{
			if (AnyJobIsDirty && !IsDirty && !(DateCreated > LastSavedUtc))
			{
				Debug.WriteLine("Warning: Project is not marked as 'IsDirty', but one or more of the jobs are dirty.");
			}

			projectAdapter.UpdateProjectCurrentJobId(Id, CurrentJobId);
			if (IsDirty || AnyJobIsDirty)
			{
				SaveColorBandSets(Id, projectAdapter);
				SaveJobs(Id, projectAdapter);

				LastSavedUtc = DateTime.UtcNow;
				_originalCurrentJobId = CurrentJobId;
				return true;
			}
			else
			{
				Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
				return false;
			}
		}

		public void Add(Job job)
		{
			_jobsCollection.Push(job);

			if (!_colorBandSetCollection.MoveCurrentTo(job.ColorBandSetId))
			{
				throw new InvalidOperationException("Cannot add this job, the job's ColorBandSet has not yet been added.");
			}

			LastUpdatedUtc = DateTime.UtcNow;
		}

		public void Add(ColorBandSet colorBandSet)
		{
			if (_colorBandSetCollection.Add(colorBandSet))
			{
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public bool GoBack(bool skipPanJobs)
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (_jobsCollection.TryGetPreviousJob(skipPanJobs, out var job))
				{
					//int idx = index;
					DoWithWriteLock(() =>
					{
						CurrentJob = job;
					});

					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_stateLock.ExitUpgradeableReadLock();
			}
		}

		public bool GoForward(bool skipPanJobs)
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (_jobsCollection.TryGetNextJob(skipPanJobs, out var job))
				{
					DoWithWriteLock(() =>
					{
						CurrentJob = job;
					});

					return true;
				}
				else
				{
					return false;
				}
			}
			finally
			{
				_stateLock.ExitUpgradeableReadLock();
			}
		}

		public bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt newCanvasSizeInBlocks, [MaybeNullWhen(false)] out Job matchingProxy)
		{
			return _jobsCollection.TryGetCanvasSizeUpdateProxy(job, newCanvasSizeInBlocks, out matchingProxy);
		}

		public Job GetPreferredSibling(Job job) => _jobsCollection.GetPreferredSibling(job);

		#endregion

		#region Private Methods

		private ObjectId LoadColorBandSetForJob(ObjectId colorBandSetId)
		{
			var colorBandSet = TrySetCurrentColorBandSet(colorBandSetId);

			var targetIterations = CurrentJob.MapCalcSettings.TargetIterations;
			if (colorBandSet == null || colorBandSet.HighCutoff != targetIterations)
			{
				colorBandSet = ColorBandSetHelper.GetBestMatchingColorBandSet(targetIterations, _colorBandSetCollection.GetColorBandSets());

				if (colorBandSet.HighCutoff != targetIterations)
				{
					var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, targetIterations);
					Debug.WriteLine($"Creating new adjusted ColorBandSet: {adjustedColorBandSet.Id} to replace {colorBandSet.Id} for job: {CurrentJobId}.");

					_colorBandSetCollection.Push(adjustedColorBandSet);
				}
			}

			return CurrentColorBandSet.Id;
		}

		private ColorBandSet? TrySetCurrentColorBandSet(ObjectId colorBandSetId)
		{
			ColorBandSet? result;

			if (CurrentColorBandSet.Id == colorBandSetId)
			{
				result = CurrentColorBandSet;
			}
			else
			{
				if (_colorBandSetCollection.TryFindByColorBandSetId(colorBandSetId, out result))
				{
					_colorBandSetCollection.MoveCurrentTo(result);
				}
			}

			return result;
		}

		private void SaveColorBandSets(ObjectId projectId, IProjectAdapter projectAdapter)
		{
			for (var i = 0; i < _colorBandSetCollection.Count; i++)
			{
				var cbs = _colorBandSetCollection[i];
				if (!cbs.OnFile)
				{
					cbs.ProjectId = projectId;
					projectAdapter.InsertColorBandSet(cbs);
				}
			}

			for (var i = 0; i < _colorBandSetCollection.Count; i++)
			{
				var cbs = _colorBandSetCollection[i];
				if (cbs.IsDirty)
				{
					projectAdapter.UpdateColorBandSetDetails(cbs);
				}
			}
		}


		private void SaveJobs(ObjectId projectId, IProjectAdapter projectAdapter)
		{
			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];

				if (!job.OnFile)
				{
					job.ProjectId = projectId;
					projectAdapter.InsertJob(job);
				}
			}

			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];

				if (job.IsDirty)
				{
					projectAdapter.UpdateJobDetails(job);
				}
			}
		}

		#endregion

		#region Lock Helpers

		//private T DoWithReadLock<T>(Func<T> function)
		//{
		//	_stateLock.EnterReadLock();

		//	try
		//	{
		//		return function();
		//	}
		//	finally
		//	{
		//		_stateLock.ExitReadLock();
		//	}
		//}

		private void DoWithWriteLock(Action action)
		{
			_stateLock.EnterWriteLock();

			try
			{
				action();
			}
			finally
			{
				_stateLock.ExitWriteLock();
			}
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)

					if (_jobsCollection != null)
					{
						_jobsCollection.Dispose();
					}

					if (_colorBandSetCollection != null)
					{
						_colorBandSetCollection.Dispose();
						//_colorBandSetCollection = null;
					}

				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}


		#endregion
	}
}

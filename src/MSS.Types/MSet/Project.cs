using MongoDB.Bson;
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

		private readonly ObjectId _originalCurrentJobId;

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
						value.ProjectId = Id;
						_colorBandSetCollection.Push(value);
						LastUpdatedUtc = DateTime.UtcNow;
					}

					OnPropertyChanged(nameof(CurrentColorBandSet));
				}
			}
		}

		#endregion

		#region Public Methods

		public bool Save(IProjectAdapter projectAdapter)
		{
			if (AnyJobIsDirty)
			{
				Debug.Assert(IsDirty, "Warning: Project is not marked as 'IsDirty', but one or more of the jobs are dirty.");
			}

			projectAdapter.UpdateProjectCurrentJobId(Id, CurrentJobId);
			if (IsDirty || AnyJobIsDirty)
			{
				SaveColorBandSets(Id, projectAdapter);
				SaveJobs(Id, projectAdapter);

				LastSavedUtc = DateTime.UtcNow;
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
			LastUpdatedUtc = DateTime.UtcNow;
		}

		public bool GoBack()
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (_jobsCollection.TryGetPreviousJob(out var job))
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

		public bool GoForward()
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (_jobsCollection.TryGetNextJob(out var job))
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

		public Job GetOriginalJob(Job job) => _jobsCollection.GetOriginalJob(job);

		#endregion

		#region Private Methods

		private ObjectId LoadColorBandSetForJob(ObjectId colorBandSetId)
		{
			if (CurrentColorBandSet.Id != colorBandSetId)
			{
				_colorBandSetCollection.MoveCurrentTo(colorBandSetId);
			}

			var colorBandSet = CurrentColorBandSet;

			var targetIterations = CurrentJob.MSetInfo.MapCalcSettings.TargetIterations;

			if (targetIterations < colorBandSet.HighCutoff)
			{
				if (_colorBandSetCollection.TryGetCbsSmallestCutoffGtrThan(targetIterations, out var index))
				{
					_colorBandSetCollection.MoveCurrentTo(index);
					colorBandSet = CurrentColorBandSet;
				}
				else
				{
					Debug.WriteLine("No Matching ColorBandSet found.");

					//if (_colorBandSetCollection.TryGetCbsLargestCutoffLessThan(targetIterations, out var index2))
					//{
					//	_colorBandSetCollection.MoveCurrentTo(index2);
					//}
					//else
					//{
					//	Debug.WriteLine("HUH?");
					//}
				}
			}

			if (colorBandSet.HighCutoff != targetIterations)
			{
				var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, targetIterations);
				_colorBandSetCollection.Push(adjustedColorBandSet);
			}

			return CurrentColorBandSet.Id;
		}

		private void SaveColorBandSets(ObjectId projectId, IProjectAdapter projectAdapter)
		{
			for (var i = 0; i < _colorBandSetCollection.Count; i++)
			{
				var cbs = _colorBandSetCollection[i];
				if (cbs.DateCreated > LastSavedUtc)
				{
					cbs.ProjectId = projectId;
					//var updatedCbs = projectAdapter.InsertColorBandSet(cbs);
					_ = projectAdapter.InsertColorBandSet(cbs);
					//_colorBandSetCollection[i] = updatedCbs;
					//UpdateCbsParentIds(cbs.Id, updatedCbs.Id/*, projectAdapter*/);
					//UpdateJobCbsIds(cbs.Id, updatedCbs.Id);
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

		//private void UpdateCbsParentIds(ObjectId oldParentId, ObjectId newParentId/*, IProjectAdapter projectAdapter*/)
		//{
		//	for (var i = 0; i < _colorBandSetCollection.Count; i++)
		//	{
		//		var cbs = _colorBandSetCollection[i];
		//		if (oldParentId == cbs.ParentId)
		//		{
		//			Debug.WriteLine($"Updating the parent of ColorBandSet with ID: {cbs.Id}, created: {cbs.DateCreated} with new parent ID: {newParentId}.");
		//			cbs.ParentId = newParentId;
		//			//projectAdapter.UpdateColorBandSetParentId(cbs.Id, cbs.ParentId);
		//		}
		//	}
		//}

		//private void UpdateJobCbsIds(ObjectId oldCbsId, ObjectId newCbsId)
		//{
		//	for (var i = 0; i < _jobsCollection.Count; i++)
		//	{
		//		var job = _jobsCollection[i];
		//		if (job.ColorBandSetId == oldCbsId)
		//		{
		//			job.ColorBandSetId = newCbsId;
		//		}
		//	}
		//}

		private void SaveJobs(ObjectId projectId, IProjectAdapter projectAdapter)
		{
			//var lastSavedTime = _projectAdapter.GetProjectJobsLastSaveTime(projectId);

			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];

				if (job.DateCreated > LastSavedUtc)
				{
					job.ProjectId = projectId;
					_ = projectAdapter.InsertJob(job);
					//var updatedJob = projectAdapter.InsertJob(job);
					//_jobsCollection[i] = updatedJob;
					//UpdateJobParents(job.Id, updatedJob.Id/*, projectAdapter*/);
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

		//private void UpdateJobParents(ObjectId oldParentId, ObjectId newParentId/*, IProjectAdapter projectAdapter*/)
		//{
		//	for (var i = 0; i < _jobsCollection.Count; i++)
		//	{
		//		var job = _jobsCollection[i];
		//		if (oldParentId == job.ParentJobId)
		//		{
		//			job.ParentJobId = newParentId;
		//			//projectAdapter.UpdateJobsParent(job);
		//		}
		//	}
		//}

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

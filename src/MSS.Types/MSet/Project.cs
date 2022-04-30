using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
		
		private DateTime _lastSavedUtc;

		private readonly ObjectId _originalCurrentJobId;

		#region Constructor

		public Project(string name, string? description, IEnumerable<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId) 
			: this(ObjectId.Empty, name, description, jobs, colorBandSets, currentJobId, DateTime.MinValue)
		{ }

		public Project(ObjectId id, string name, string? description, IEnumerable<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, ObjectId currentJobId, DateTime lastSavedUtc)
		{
			Id = id;

 			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			_jobsCollection = new JobCollection(jobs);
			_colorBandSetCollection = new ColorBandSetCollection(colorBandSets);

			_stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			_originalCurrentJobId = currentJobId;

			if (!_jobsCollection.MoveCurrentTo(currentJobId))
			{
				Debug.WriteLine($"Warning the Project a CurrentJobId of {Id}, but this job cannot be found. Setting the current job to be the last job.");
			}

			LoadColorBandSetForJob(CurrentJob.ColorBandSet);
			_jobsCollection.CurrentJob.ColorBandSet = _colorBandSetCollection.CurrentColorBandSet;

			Debug.WriteLine($"Project is loaded. CurrentJobId: {_jobsCollection.CurrentJob.Id}, Current ColorBandSetId: {_colorBandSetCollection.CurrentColorBandSet.Id}.");

			LastSavedUtc = lastSavedUtc;
		}

		#endregion

		#region Public Properties

		public DateTime DateCreated => Id == ObjectId.Empty ? LastSavedUtc : Id.CreationTime;
		public bool OnFile => Id != ObjectId.Empty;

		public bool CanGoBack => _jobsCollection.CanGoBack;
		public bool CanGoForward => _jobsCollection.CanGoForward;
		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;

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
					OnPropertyChanged(nameof(IsDirty));
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
					OnPropertyChanged(nameof(IsDirty));
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
				OnPropertyChanged(nameof(IsDirty));
			}
		}

		public DateTime LastUpdatedUtc { get; private set; }

		public Job CurrentJob
		{
			get => _jobsCollection.CurrentJob;
			set
			{
				if (CurrentJob != value)
				{
					var isDirtyBefore = IsDirty;
					
					_ = _jobsCollection.MoveCurrentTo(value);
					if (_colorBandSetCollection.CurrentColorBandSet != _jobsCollection.CurrentJob.ColorBandSet)
					{
						LoadColorBandSetForJob(_jobsCollection.CurrentJob.ColorBandSet);
						_jobsCollection.CurrentJob.ColorBandSet = _colorBandSetCollection.CurrentColorBandSet;
					}

					OnPropertyChanged();

					if (IsDirty != isDirtyBefore)
					{
						OnPropertyChanged(nameof(IsDirty));
					}
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
					var isDirtyBefore = IsDirty;

					if (!_colorBandSetCollection.MoveCurrentTo(value))
					{
						value.ProjectId = Id;
						_colorBandSetCollection.Push(value);
						LastUpdatedUtc = DateTime.UtcNow;
					}

					if (_jobsCollection.CurrentJob.ColorBandSet != value)
					{
						LoadColorBandSetForJob(value);
						_jobsCollection.CurrentJob.ColorBandSet = _colorBandSetCollection.CurrentColorBandSet;
					}

					OnPropertyChanged();

					if (IsDirty != isDirtyBefore)
					{
						OnPropertyChanged(nameof(IsDirty));
					}
				}
			}
		}

		#endregion

		#region Public Methods

		public void Save(IProjectAdapter projectAdapter)
		{
			if (IsDirty)
			{
				SaveColorBandSets(Id, projectAdapter);
				SaveJobs(Id, projectAdapter);

				projectAdapter.UpdateProjectCurrentJobId(Id, CurrentJobId);

				LastSavedUtc = DateTime.UtcNow;
			}
			else if (IsCurrentJobIdChanged)
			{
				projectAdapter.UpdateProjectCurrentJobId(Id, CurrentJobId);
			}
			else
			{
				Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
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
				if (_jobsCollection.GoBack())
				{
					DoWithWriteLock(() =>
					{
						if (_colorBandSetCollection.CurrentColorBandSet != _jobsCollection.CurrentJob.ColorBandSet)
						{
							LoadColorBandSetForJob(_jobsCollection.CurrentJob.ColorBandSet);
							_jobsCollection.CurrentJob.ColorBandSet = _colorBandSetCollection.CurrentColorBandSet;
						}
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
				if (_jobsCollection.GoForward())
				{
					DoWithWriteLock(() =>
					{
						if (_colorBandSetCollection.CurrentColorBandSet != _jobsCollection.CurrentJob.ColorBandSet)
						{
							LoadColorBandSetForJob(_jobsCollection.CurrentJob.ColorBandSet);
							_jobsCollection.CurrentJob.ColorBandSet = _colorBandSetCollection.CurrentColorBandSet;
						}
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

		private void LoadColorBandSetForJob(ColorBandSet colorBandSet)
		{
			var targetIterations = CurrentJob.MSetInfo.MapCalcSettings.TargetIterations;

			if (targetIterations < colorBandSet.HighCutOff)
			{
				if (_colorBandSetCollection.TryGetCbsSmallestCutOffGtrThan(targetIterations, out var index))
				{
					_colorBandSetCollection.MoveCurrentTo(index);
				}
				else
				{
					Debug.WriteLine("No Matching ColorBandSet found.");

					//if (_colorBandSetCollection.TryGetCbsLargestCutOffLessThan(targetIterations, out var index2))
					//{
					//	_colorBandSetCollection.MoveCurrentTo(index2);
					//}
					//else
					//{
					//	Debug.WriteLine("HUH?");
					//}
				}
			}
			else
			{
				if (!_colorBandSetCollection.MoveCurrentTo(colorBandSet))
				{
					Debug.WriteLine($"Warning: the MapProjectViewModel found a ColorBandSet for Job: {CurrentJob.Id} that was not associated with the project: {Id}.");
					colorBandSet = colorBandSet.CreateNewCopy();
					colorBandSet.ProjectId = Id;
					_colorBandSetCollection.Push(colorBandSet);
					LastUpdatedUtc = DateTime.UtcNow;
				}
			}

			colorBandSet = _colorBandSetCollection.CurrentColorBandSet;
			if (colorBandSet.HighCutOff != targetIterations)
			{
				colorBandSet = colorBandSet.CreateNewCopy(targetIterations);
				_colorBandSetCollection.Push(colorBandSet);
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		private void SaveColorBandSets(ObjectId projectId, IProjectAdapter projectAdapter)
		{
			for (var i = 0; i < _colorBandSetCollection.Count; i++)
			{
				var cbs = _colorBandSetCollection[i];
				if (cbs.DateCreated > LastSavedUtc)
				{
					cbs.ProjectId = projectId;
					var updatedCbs = projectAdapter.CreateColorBandSet(cbs);
					_colorBandSetCollection[i] = updatedCbs;
					UpdateCbsParentIds(cbs.Id, updatedCbs.Id/*, projectAdapter*/);
					UpdateJobCbsIds(cbs, updatedCbs);
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

		private void UpdateCbsParentIds(ObjectId oldParentId, ObjectId newParentId/*, IProjectAdapter projectAdapter*/)
		{
			for (var i = 0; i < _colorBandSetCollection.Count; i++)
			{
				var cbs = _colorBandSetCollection[i];
				if (oldParentId == cbs.ParentId)
				{
					Debug.WriteLine($"Updating the parent of ColorBandSet with ID: {cbs.Id}, created: {cbs.DateCreated} with new parent ID: {newParentId}.");
					cbs.ParentId = newParentId;
					//projectAdapter.UpdateColorBandSetParentId(cbs.Id, cbs.ParentId);
				}
			}
		}

		private void UpdateJobCbsIds(ColorBandSet oldCbs, ColorBandSet newCbs)
		{
			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];
				if (oldCbs == job.ColorBandSet)
				{
					job.ColorBandSet = newCbs;
				}
			}
		}

		private void SaveJobs(ObjectId projectId, IProjectAdapter projectAdapter)
		{
			//var lastSavedTime = _projectAdapter.GetProjectJobsLastSaveTime(projectId);

			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];

				if (job.DateCreated > LastSavedUtc)
				{
					job.ProjectId = projectId;
					var updatedJob = projectAdapter.InsertJob(job);
					_jobsCollection[i] = updatedJob;
					UpdateJobParents(job.Id, updatedJob.Id/*, projectAdapter*/);
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

		private void UpdateJobParents(ObjectId oldParentId, ObjectId newParentId/*, IProjectAdapter projectAdapter*/)
		{
			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];
				if (oldParentId == job.ParentJobId)
				{
					job.ParentJobId = newParentId;
					//projectAdapter.UpdateJobsParent(job);
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

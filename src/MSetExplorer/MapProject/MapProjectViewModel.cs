using MongoDB.Bson;
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
	internal class MapProjectViewModel : ViewModelBase, IMapProjectViewModel, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly ObservableCollection<Job> _jobsCollection;
		private readonly ReaderWriterLockSlim _jobsLock;

		private int _jobsPointer;
		private SizeInt _canvasSize;

		private Project _currentProject;

		#region Constructor

		public MapProjectViewModel(ProjectAdapter projectAdapter, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			BlockSize = blockSize;
			_jobsCollection = new ObservableCollection<Job>();
			_jobsPointer = -1;

			_canvasSize = new SizeInt();
			_currentProject = null;

			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public event EventHandler CurrentJobChanged;

		public SizeInt BlockSize { get; }

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if(value != _canvasSize)
				{
					_canvasSize = value;
					Reload();
				}
			}
		}

		public Project Project
		{
			get => _currentProject;
			private set
			{
				if(value != _currentProject)
				{
					_currentProject = value;
					OnPropertyChanged();
				}
			}
		}

		public Job CurrentJob => DoWithReadLock(() => { return _jobsPointer == -1 ? null : _jobsCollection[_jobsPointer]; });
		public bool CanGoBack => !(CurrentJob?.ParentJob is null);
		public bool CanGoForward => DoWithReadLock(() => { return TryGetNextJobInStack(_jobsPointer, out var _); });

		#endregion

		#region Public Methods

		public void StartNewProject(MSetInfo mSetInfo)
		{
			Project = new Project(ObjectId.Empty, "Home", description: null);
			LoadMap(mSetInfo, TransformType.None);
		}

		public void LoadNewProject(string projectName, string projectDescription, MSetInfo mSetInfo)
		{
			Project = _projectAdapter.CreateProject(projectName, projectDescription);
			LoadMap(mSetInfo, TransformType.None);
		}

		public void LoadProject(string projectName)
		{
			Project = _projectAdapter.GetProject(projectName);
			var jobs = _projectAdapter.GetAllJobs(Project.Id);

			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();

				foreach (var job in jobs)
				{
					_jobsCollection.Add(job);
				}

				_jobsPointer = _jobsCollection.Count - 1;

				Rerun(_jobsPointer);
			});
		}

		public void SaveProject(string projectName, string description)
		{
			if (Project.Id != ObjectId.Empty)
			{
				throw new InvalidOperationException("Cannot change the name of a project already loaded, use SaveLoadedProject instead.");
			}

			Project = _projectAdapter.CreateProject(projectName, description);

			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(Project.Id);

			var jobsList = Jobs.ToList();

			for (var i = 0; i < jobsList.Count; i++)
			{
				var job = jobsList[i];
				if (job.Id.CreationTime > lastSavedTime)
				{
					job.Project = Project;
					var updatedJob = _projectAdapter.InsertJob(job);
					UpdateJob(job, updatedJob);
				}
			}
		}

		public void SaveLoadedProject()
		{
			if (Project.Id == ObjectId.Empty)
			{
				throw new InvalidOperationException("Cannot save an unloaded project, use SaveProject instead.");
			}

			var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(Project.Id);

			var jobsList = Jobs.ToList();

			for(var i = 0; i < jobsList.Count; i++ )
			{
				var job = jobsList[i];
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
					if (TryFindByJobId(parentJob.Id, out var job))
					{
						var jobIndex = _jobsCollection.IndexOf(job);
						DoWithWriteLock(() => Rerun(jobIndex));
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
					DoWithWriteLock(() => Rerun(nextJobIndex));
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

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			var curJob = CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
			var updatedInfo = MSetInfo.UpdateWithNewCoords(curJob.MSetInfo, coords);

			LoadMap(updatedInfo, transformType, newArea);
		}

		public void UpdateTargetInterations(int targetIterations, int iterationsPerRequest)
		{
			var curJob = CurrentJob;
			var mSetInfo = curJob.MSetInfo;
			var updatedInfo = MSetInfo.UpdateWithNewIterations(mSetInfo, targetIterations, iterationsPerRequest);

			LoadMap(updatedInfo, TransformType.IterationUpdate);
		}

		public void UpdateColorMapEntries(ColorBandSet colorBands)
		{
			var curJob = CurrentJob;
			var mSetInfo = curJob.MSetInfo;
			var updatedInfo = MSetInfo.UpdateWithNewColorMapEntries(mSetInfo, colorBands);

			LoadMap(updatedInfo, TransformType.ColorMapUpdate);
		}

		#endregion

		#region Private Methods

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType)
		{
			var newArea = new RectangleInt(new PointInt(), CanvasSize);
			LoadMap(mSetInfo, transformType, newArea);
		}

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea)
		{
			var parentJob = CurrentJob;
			var jobName = MapJobHelper.GetJobName(transformType);
			var job = MapJobHelper.BuildJob(parentJob, Project, jobName, CanvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);

			Debug.WriteLine($"Starting Job with new coords: {mSetInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			DoWithWriteLock(() =>
			{
				_jobsCollection.Add(job);
				_jobsPointer = _jobsCollection.Count - 1;

				CurrentJobChanged?.Invoke(this, new EventArgs());
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
			});
		}

		private void Reload()
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				if (!(_jobsPointer < 0 || _jobsPointer > _jobsCollection.Count - 1))
				{
					DoWithWriteLock(() => Rerun(_jobsPointer));
				}
			}
			finally
			{
				_jobsLock.ExitUpgradeableReadLock();
			}
		}

		private void Rerun(int newJobIndex)
		{
			if (newJobIndex < 0 || newJobIndex > _jobsCollection.Count - 1)
			{
				throw new ArgumentException($"The newJobIndex with value: {newJobIndex} is not valid.", nameof(newJobIndex));
			}

			if (_jobsPointer == newJobIndex)
			{
				// Forced a redraw
				CurrentJobChanged?.Invoke(this, new EventArgs());
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			}
			else
			{
				var job = _jobsCollection[newJobIndex];
				_jobsPointer = newJobIndex;
				CurrentJobChanged?.Invoke(this, new EventArgs());
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
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

		private bool TryGetJobFromStack(int jobIndex, out Job job)
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

		private bool TryFindByJobId(ObjectId id, out Job job)
		{
			job = _jobsCollection.FirstOrDefault(x => x.Id == id);
			return job != null;
		}

		private IEnumerable<Job> Jobs => DoWithReadLock(() => { return new ReadOnlyCollection<Job>(_jobsCollection); });

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

using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Threading;

namespace MSetExplorer
{
	internal class MapProjectViewModel : ViewModelBase, IMapProjectViewModel, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;

		private readonly JobCollection _jobsCollection;
		private readonly ColorBandSetCollection _colorBandSetCollection;

		private readonly ReaderWriterLockSlim _stateLock;

		private SizeInt _canvasSize;

		private Project? _currentProject;
		private bool _currentProjectIsDirty;

		#region Constructor

		public MapProjectViewModel(ProjectAdapter projectAdapter, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;

			_jobsCollection = new JobCollection();
			_colorBandSetCollection = new ColorBandSetCollection();
			BlockSize = blockSize;

			_stateLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

			_canvasSize = new SizeInt();
			_currentProject = null;
			_currentProjectIsDirty = false;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; }

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if(value != _canvasSize)
				{
					_canvasSize = value;
					OnPropertyChanged(nameof(IMapProjectViewModel.CanvasSize));

					RerunWithNewDisplaySize();
				}
			}
		}

		public Project? CurrentProject
		{
			get => _currentProject;
			private set
			{
				if(value != _currentProject)
				{
					_currentProject = value;

					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProject));
				}
			}
		}

		public bool CurrentProjectIsDirty
		{
			get => _currentProjectIsDirty;
			private set
			{
				if (value != _currentProjectIsDirty)
				{
					_currentProjectIsDirty = value;
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectIsDirty));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanSaveProject));
				}
			}
		}

		public string? CurrentProjectName => CurrentProject?.Name;
		public bool CurrentProjectOnFile => CurrentProject?.OnFile ?? false;
		public bool CanSaveProject => CurrentProjectOnFile && CurrentProjectIsDirty;

		public Job CurrentJob => _jobsCollection.CurrentJob;

		public bool CanGoBack => _jobsCollection.CanGoBack;
		public bool CanGoForward => _jobsCollection.CanGoForward;

		public ColorBandSet CurrentColorBandSet => _colorBandSetCollection.CurrentColorBandSet;

		#endregion

		#region Public Methods -- Project

		public void ProjectStartNew(MSetInfo mSetInfo, ColorBandSet colorBandSet)
		{
			CurrentProject = new Project("New", description: null, currentJobId: ObjectId.Empty);

			_jobsCollection.Clear();

			_colorBandSetCollection.Load(colorBandSet);
			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			LoadMap(mSetInfo, TransformType.None);

			_jobsCollection.Load(_jobsCollection.CurrentJob);

			CurrentProject.CurrentJobId = CurrentJob.Id;

			CurrentProjectIsDirty = false;
		}

		public bool ProjectOpen(string projectName)
		{
			if (_projectAdapter.TryGetProject(projectName, out var project))
			{
				CurrentProjectIsDirty = false;
				LoadProject(project);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void LoadProject(Project project)
		{
			CurrentProject = project;
			CurrentProjectIsDirty = false;

			var colorBandSets = _projectAdapter.GetColorBandSetsForProject(CurrentProject.Id);
			_ = _colorBandSetCollection.Load(colorBandSets);

			var jobs = _projectAdapter.GetAllJobsForProject(CurrentProject.Id, _colorBandSetCollection.GetColorBandSets());
			_ = _jobsCollection.Load(jobs);

			if (!_jobsCollection.MoveCurrentTo(project.CurrentJobId))
			{
				Debug.WriteLine($"Warning the Project a CurrentJobId of {project.CurrentJobId}, but this job cannot be found. Setting the current job to be the last job.");
				project.CurrentJobId = CurrentJob.Id;
			}

			CurrentJob.ColorBandSet = LoadColorBandSetForJob(CurrentJob.ColorBandSet);

			var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, BlockSize);
			if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
			{
				FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
				CurrentProjectIsDirty = true;
			}

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
		}

		public void ProjectSaveAs(string name, string? description)
		{
			DoWithWriteLock(() =>
			{
				if (_projectAdapter.TryGetProject(name, out var existingProject))
				{
					_projectAdapter.DeleteProject(existingProject.Id);
				}

				Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

				var currentJobId = CurrentJob.Id;

				var project = _projectAdapter.CreateProject(name, description, currentJobId);

				SaveColorBandSetsForProject(project.Id, updateAll: true);
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

				SaveJobs(project.Id, updateAll: true);
				currentJobId = CurrentJob.Id;

				project.CurrentJobId = currentJobId;
				_projectAdapter.UpdateProjectCurrentJobId(project.Id, project.CurrentJobId);

				CurrentProject = project;

				CurrentProjectIsDirty = false;
			});
		}

		public void ProjectSave()
		{
			DoWithWriteLock(() =>
			{
				var project = CurrentProject;

				if (project != null)
				{
					if (!CurrentProjectOnFile)
					{
						throw new InvalidOperationException("Cannot save an unloaded project, use SaveProject instead.");
					}

					Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

					SaveColorBandSetsForProject(project.Id, updateAll: false);
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

					SaveJobs(project.Id, updateAll: false);
					var currentJobId = CurrentJob.Id;

					project.CurrentJobId = currentJobId;
					_projectAdapter.UpdateProjectCurrentJobId(project.Id, project.CurrentJobId);
					OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentJob));

					CurrentProjectIsDirty = false;
				}
			});
		}

		public void SaveJobs(ObjectId projectId, bool updateAll)
		{
			var lastSavedTime = _projectAdapter.GetProjectJobsLastSaveTime(projectId);

			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];

				if (job.DateCreated > lastSavedTime)
				{
					job.ProjectId = projectId;
					var updatedJob = _projectAdapter.InsertJob(job);
					_jobsCollection[i] = updatedJob;
					UpdateJobParents(job.Id, updatedJob.Id);
				}
				else
				{
					if (updateAll)
					{
						job.ProjectId = projectId;
						_projectAdapter.UpdateJobsProject(job.Id, projectId);
					}

					if (job.IsDirty)
					{
						_projectAdapter.UpdateJobDetails(job);
					}
				}
			}
		}

		public void UpdateJobParents(ObjectId oldParentId, ObjectId newParentId)
		{
			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];
				if (oldParentId == job.ParentJobId)
				{
					job.ParentJobId = newParentId;
					_projectAdapter.UpdateJobsParent(job);
				}
			}
		}

		private void SaveColorBandSetsForProject(ObjectId projectId, bool updateAll) 
		{
			var lastSavedTime = _projectAdapter.GetProjectCbSetsLastSaveTime(projectId);

			for (var i = 0; i < _colorBandSetCollection.Count; i++)
			{
				var cbs = _colorBandSetCollection[i];
				if (cbs.DateCreated > lastSavedTime)
				{
					cbs.ProjectId = projectId;
					var updatedCbs = _projectAdapter.CreateColorBandSet(cbs);
					_colorBandSetCollection[i] = updatedCbs;
					UpdateCbsParentIds(cbs.Id, updatedCbs.Id);
					UpdateJobCbsIds(cbs, updatedCbs);
				}
				else
				{
					if (updateAll)
					{
						cbs.ProjectId = projectId;
						_projectAdapter.UpdateColorBandSetProjectId(cbs.Id, cbs.ProjectId);
					}
				}
			}
		}

		public void UpdateCbsParentIds(ObjectId oldParentId, ObjectId newParentId)
		{
			for (var i = 0; i < _colorBandSetCollection.Count; i++)
			{
				var cbs = _colorBandSetCollection[i];
				if (oldParentId == cbs.ParentId)
				{
					Debug.WriteLine($"Updating the parent of ColorBandSet with ID: {cbs.Id}, created: {cbs.DateCreated} with new parent ID: {newParentId}.");
					cbs.ParentId = newParentId;
					_projectAdapter.UpdateColorBandSetParentId(cbs.Id, cbs.ParentId);
				}
			}
		}


		public void UpdateJobCbsIds(ColorBandSet oldCbs, ColorBandSet newCbs)
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

		#endregion

		#region Public Methods - Job

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			if (CurrentProject == null)
			{
				return;
			}

			var curJob = CurrentJob;

			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
			var updatedMSetInfo = MSetInfo.UpdateWithNewCoords(curJob.MSetInfo, coords);
			LoadMap(updatedMSetInfo, transformType, newArea);
		}

		public void UpdateMapCoordinates(RRectangle coords)
		{
			if (CurrentProject == null)
			{
				return;
			}

			var mSetInfo = CurrentJob.MSetInfo;

			if (mSetInfo.Coords != coords)
			{
				var updatedMSetInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
				LoadMap(updatedMSetInfo, TransformType.CoordinatesUpdate);
			}
		}

		public void UpdateColors(ColorBandSet colorBandSet)
		{
			var isTargetIterationsBeingUpdated = colorBandSet.HighCutOff != _colorBandSetCollection.CurrentColorBandSet.HighCutOff;
			Debug.WriteLine($"MapProjectViewModel is having its ColorBandSet value updated. Old = {_colorBandSetCollection.CurrentColorBandSet?.Id}, New = {colorBandSet.Id} Iterations Updated = {isTargetIterationsBeingUpdated}.");

			if (_colorBandSetCollection.Contains(colorBandSet))
			{
				_ = _colorBandSetCollection.MoveCurrentTo(colorBandSet);
			}
			else
			{
				_colorBandSetCollection.Push(colorBandSet);
			}

			CurrentJob.ColorBandSet = colorBandSet;

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			if (isTargetIterationsBeingUpdated)
			{
				UpdateTargetInterations(colorBandSet.HighCutOff);
			}
			else
			{
				// No new job is being created, instead this job is being updated.
				Debug.Assert(CurrentJob.MSetInfo.MapCalcSettings.TargetIterations == colorBandSet.HighCutOff, "HighCutOff mismatch on UpdateColors.");
			}

			CurrentProjectIsDirty = true;
		}

		public void UpdateTargetInterations(int targetIterations)
		{
			if (CurrentProject == null)
			{
				return;
			}
			
			var mSetInfo = CurrentJob.MSetInfo;

			if (mSetInfo.MapCalcSettings.TargetIterations != targetIterations)
			{
				var updatedMSetInfo = MSetInfo.UpdateWithNewIterations(mSetInfo, targetIterations);
				LoadMap(updatedMSetInfo, TransformType.IterationUpdate);
			}
		}

		public RRectangle? GetUpdateCoords(TransformType transformType, RectangleInt newArea)
		{
			if (CurrentProject == null)
			{
				return null;
			}

			var curJob = CurrentJob;

			if (newArea == new RectangleInt())
			{
				return curJob.MSetInfo.Coords;
			}
			else
			{
				var position = curJob.MSetInfo.Coords.Position;
				var samplePointDelta = curJob.Subdivision.SamplePointDelta;
				var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);

				return coords;
			}
		}

		public bool GoBack()
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (CurrentProject == null)
				{
					return false;
				}

				if (_jobsCollection.GoBack())
				{
					DoWithWriteLock(() =>
					{
						if (CurrentJob.ColorBandSet != CurrentColorBandSet)
						{
							CurrentJob.ColorBandSet = LoadColorBandSetForJob(CurrentJob.ColorBandSet);
						}

						var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, BlockSize);
						if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
						{
							FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
						}
					});

					CurrentProjectIsDirty = true;
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));

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
				if (CurrentProject == null)
				{
					return false;
				}

				if (_jobsCollection.GoForward())
				{
					DoWithWriteLock(() =>
					{
						if (CurrentJob.ColorBandSet != CurrentColorBandSet)
						{
							CurrentJob.ColorBandSet = LoadColorBandSetForJob(CurrentJob.ColorBandSet);
						}

						var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, BlockSize);
						if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
						{
							FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
						}
					});

					CurrentProjectIsDirty = true;
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));

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

		#endregion

		#region Private Methods

		private void RerunWithNewDisplaySize()
		{
			var wasUpdated = false;

			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (CurrentProject == null)
				{
					return;
				}

				DoWithWriteLock(() =>
				{
					var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, BlockSize);
					if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
					{
						FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
						wasUpdated = true;
					}
				});
			}
			finally
			{
				_stateLock.ExitUpgradeableReadLock();
			}

			if (wasUpdated)
			{
				CurrentProjectIsDirty = true;
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			}
		}

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType)
		{
			LoadMap(mSetInfo, transformType, null);
		}

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt? newArea)
		{
			var curProject = CurrentProject;

			if (curProject == null)
			{
				return;
			}

			if (mSetInfo.MapCalcSettings.TargetIterations != CurrentColorBandSet.HighCutOff)
			{
				Debug.WriteLine($"WARNING: Job's ColorMap HighCutOff doesn't match the TargetIterations.");
			}

			var jobName = MapJobHelper.GetJobName(transformType);
			var job = MapJobHelper.BuildJob(CurrentJob.Id, curProject.Id, jobName, CanvasSize, mSetInfo, CurrentColorBandSet, transformType, newArea, BlockSize, _projectAdapter);

			Debug.WriteLine($"Starting Job with new coords: {mSetInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			DoWithWriteLock(() =>
			{
				_jobsCollection.Push(job);

				CurrentProjectIsDirty = true;
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
			});
		}

		private void FindOrCreateJobForNewCanvasSize(Project project, Job job, SizeInt newCanvasSizeInBlocks)
		{
			// Note if this job is itself a CanvasSizeUpdate Proxy Job, then its parent is used to conduct the search.
			if (_jobsCollection.TryGetCanvasSizeUpdateProxy(job, newCanvasSizeInBlocks, out var matchingProxy))
			{
				_ = _jobsCollection.MoveCurrentTo(matchingProxy);
				if (CurrentJob.ColorBandSet != CurrentColorBandSet)
				{
					CurrentJob.ColorBandSet = LoadColorBandSetForJob(CurrentJob.ColorBandSet);
				}

				return;
			}

			// Make sure we use the original job and not a 'CanvasSizeUpdate Proxy Job'.
			var origJob = _jobsCollection.GetOriginalJob(job);

			if (origJob.CanvasSizeInBlocks == newCanvasSizeInBlocks)
			{
				_jobsCollection.MoveCurrentTo(origJob);
				if (CurrentJob.ColorBandSet != CurrentColorBandSet)
				{
					CurrentJob.ColorBandSet = LoadColorBandSetForJob(CurrentJob.ColorBandSet);
				}

				return;
			}

			// Create a new job
			job = origJob;

			var newCoords = RMapHelper.GetNewCoordsForNewCanvasSize(job.MSetInfo.Coords, job.CanvasSizeInBlocks, newCanvasSizeInBlocks, job.Subdivision.SamplePointDelta, BlockSize);
			var newMSetInfo = MSetInfo.UpdateWithNewCoords(job.MSetInfo, newCoords);

			var transformType = TransformType.CanvasSizeUpdate;
			RectangleInt? newArea = null;

			var jobName = MapJobHelper.GetJobName(transformType);
			var newJob = MapJobHelper.BuildJob(job.Id, project.Id, jobName, CanvasSize, newMSetInfo, CurrentColorBandSet, transformType, newArea, BlockSize, _projectAdapter);

			Debug.WriteLine($"Re-runing job. Current CanvasSize: {job.CanvasSizeInBlocks}, new CanvasSize: {newCanvasSizeInBlocks}.");
			Debug.WriteLine($"Starting Job with new coords: {newMSetInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			_jobsCollection.Push(newJob);
		}

		private ColorBandSet LoadColorBandSetForJob(ColorBandSet colorBandSet)
		{
			if (CurrentProject == null)
			{
				return colorBandSet;
			}

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
					Debug.WriteLine($"Warning: the MapProjectViewModel found a ColorBandSet for Job: {CurrentJob.Id} that was not associated with the project: {CurrentProject.Id}.");
					colorBandSet = colorBandSet.CreateNewCopy();
					colorBandSet.ProjectId = CurrentProject.Id;
					_colorBandSetCollection.Push(colorBandSet);
				}
			}

			colorBandSet = _colorBandSetCollection.CurrentColorBandSet;
			if (colorBandSet.HighCutOff != targetIterations)
			{
				colorBandSet = colorBandSet.CreateNewCopy();
				colorBandSet.HighCutOff = targetIterations;
				_colorBandSetCollection.Push(colorBandSet);
			}

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			return colorBandSet;
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

		#region IDisposable Support

		private bool _disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
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

					_stateLock.Dispose();
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

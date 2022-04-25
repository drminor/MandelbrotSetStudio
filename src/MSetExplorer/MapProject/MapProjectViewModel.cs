using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
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

			var jobs = _projectAdapter.GetAllJobs(CurrentProject.Id);
			_ = _jobsCollection.Load(jobs, currentId: project.CurrentJobId);

			var colorBandSets = _projectAdapter.GetColorBandSetsForProject(CurrentProject.Id);
			_ = _colorBandSetCollection.Load(colorBandSets, CurrentJob.ColorBandSet.Id);

			CurrentColorBandSet.HighCutOff = CurrentJob.MSetInfo.MapCalcSettings.TargetIterations;

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			DoWithWriteLock(() => 
			{
				_ = UpdateTheJobsCanvasSize(CurrentJob);
			});

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

				//if (job.IsDirty || job.Id.CreationTime > lastSavedTime || job.LastUpdatedUtc > lastSavedTime)
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

					if (job.IsDirty || job.Id.CreationTime > lastSavedTime || job.LastUpdatedUtc > lastSavedTime)
					{
						_projectAdapter.UpdateJobDetails(job);
						job.IsDirty = false;
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
					UpdateJobCbsIds(cbs.Id, updatedCbs.Id);
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


		public void UpdateJobCbsIds(ObjectId oldCbsId, ObjectId newCbsId)
		{
			for (var i = 0; i < _jobsCollection.Count; i++)
			{
				var job = _jobsCollection[i];
				if (oldCbsId == job.ColorBandSet.Id)
				{
					job.ColorBandSet.Id = newCbsId;
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
			var isTargetIterationsBeingUpdated = colorBandSet.HighColorBand.CutOff != _colorBandSetCollection.CurrentColorBandSet.HighColorBand.CutOff;
			Debug.WriteLine($"MapProjectViewModel is having its ColorBandSet value updated. Old = {_colorBandSetCollection.CurrentColorBandSet?.Id}, New = {colorBandSet.Id} Iterations Updated = {isTargetIterationsBeingUpdated}.");

			if (_colorBandSetCollection.Contains(colorBandSet))
			{
				_ = _colorBandSetCollection.MoveCurrentTo(colorBandSet);
			}
			else
			{
				_colorBandSetCollection.Push(colorBandSet);
				CurrentProjectIsDirty = true;
			}

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			if (isTargetIterationsBeingUpdated)
			{
				UpdateTargetInterations(colorBandSet.HighColorBand.CutOff);
			}
			else
			{
				// No new job is being created, instead this job is being updated.
				CurrentJob.ColorBandSet = colorBandSet;
			}
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
						if (! _colorBandSetCollection.MoveCurrentTo(CurrentJob.ColorBandSet))
						{
							throw new InvalidOperationException("The MapProjectViewModel cannot find a ColorBandSet for the job we are moving back to.");
						}
						OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
					}
					_ = UpdateTheJobsCanvasSize(CurrentJob);
				});

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

		public bool GoForward()
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
						if (!_colorBandSetCollection.MoveCurrentTo(CurrentJob.ColorBandSet))
						{
							throw new InvalidOperationException("The MapProjectViewModel cannot find a ColorBandSet for the job we are moving forward to.");
						}
						OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
					}
					_ = UpdateTheJobsCanvasSize(CurrentJob);
				});

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

		#endregion

		#region Private Methods

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

			var parentJobId = GetParentJobId(CurrentJob);
			var jobName = MapJobHelper.GetJobName(transformType);
			var job = MapJobHelper.BuildJob(parentJobId, curProject.Id, jobName, CanvasSize, mSetInfo, CurrentColorBandSet, transformType, newArea, BlockSize, _projectAdapter);

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

		private ObjectId? GetParentJobId(Job curentJob)
		{
			ObjectId? result = curentJob.Id == ObjectId.Empty ? null : curentJob.Id;
			return result;
		}

		private void RerunWithNewDisplaySize()
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				if (CurrentProject == null)
				{
					return;
				}

				var curJob = CurrentJob;
				var wasUpdated = false;
				DoWithWriteLock(() =>
				{
					wasUpdated = UpdateTheJobsCanvasSize(curJob);
				});

				if (wasUpdated)
				{
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				}
			}
			finally
			{
				_stateLock.ExitUpgradeableReadLock();
			}
		}
	
		private bool UpdateTheJobsCanvasSize(Job job)
		{
			var newCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, BlockSize);
			//var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(new SizeDbl(CanvasSize), BlockSize, keepSquare: false);
			//var newCanvasSizeInBlocks = sizeInWholeBlocks.Inflate(2);

			if (newCanvasSizeInBlocks != job.CanvasSizeInBlocks)
			{
				var diff = newCanvasSizeInBlocks.Sub(job.CanvasSizeInBlocks);

				diff = diff.Scale(BlockSize);
				diff = diff.DivInt(new SizeInt(2));
				var rDiff = job.Subdivision.SamplePointDelta.Scale(diff);

				var coords = job.MSetInfo.Coords;
				var newCoords = AdjustCoords(coords, rDiff);

				var mapBlockOffset = RMapHelper.GetMapBlockOffset(newCoords, job.Subdivision.Position, job.Subdivision.SamplePointDelta, BlockSize, out var canvasControlOffset);

				var newMsetInfo = MSetInfo.UpdateWithNewCoords(job.MSetInfo, newCoords);

				Debug.WriteLine($"Reruning job. Current CanvasSize: {job.CanvasSizeInBlocks}, new CanvasSize: {newCanvasSizeInBlocks}.");

				job.MSetInfo = newMsetInfo;
				job.MapBlockOffset = mapBlockOffset;
				job.CanvasControlOffset = canvasControlOffset;
				job.CanvasSizeInBlocks = newCanvasSizeInBlocks;
				CurrentProjectIsDirty = true;

				return true;
			}
			else
			{
				return false;
			}
		}

		private RRectangle AdjustCoords(RRectangle coords, RSize rDiff)
		{
			var nrmArea = RNormalizer.Normalize(coords, rDiff, out var nrmDiff);

			var x1 = nrmArea.X1 - nrmDiff.Width.Value;
			var x2 = nrmArea.X2 + nrmDiff.Width.Value;

			var y1 = nrmArea.Y1 - nrmDiff.Height.Value;
			var y2 = nrmArea.Y2 + nrmDiff.Height.Value;

			var result = new RRectangle(x1, x2, y1, y2, nrmArea.Exponent);

			return result;
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

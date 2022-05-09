﻿using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class MapProjectViewModel : ViewModelBase, IMapProjectViewModel, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly SizeInt _blockSize;

		private SizeInt _canvasSize;
		private Project? _currentProject;

		#region Constructor

		public MapProjectViewModel(ProjectAdapter projectAdapter, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_blockSize = blockSize;

			_canvasSize = new SizeInt();
			_currentProject = null;
			//_currentProjectIsDirty = false;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if(value != _canvasSize)
				{
					_canvasSize = value;
					OnPropertyChanged(nameof(IMapProjectViewModel.CanvasSize));

					if (CurrentProject != null)
					{
						RerunWithNewDisplaySize(CurrentProject);
					}
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
					if (_currentProject != null)
					{
						_currentProject.PropertyChanged -= CurrentProject_PropertyChanged;
					}
					_currentProject = value;
					if (_currentProject != null)
					{
						_currentProject.PropertyChanged += CurrentProject_PropertyChanged;
					}

					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProject));
					//OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectIsDirty));
					//OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectOnFile));

					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
				}
			}
		}

		private void CurrentProject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Project.IsDirty))
			{
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectIsDirty));
			}

			if (e.PropertyName == nameof(Project.OnFile))
			{
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectOnFile));
			}

			if (e.PropertyName == nameof(Project.CurrentColorBandSet))
			{
				Debug.WriteLine("The MapProjectViewModel is raising PropertyChanged: IMapProjectViewModel.CurrentColorBandSet as the Project's ColorBandSet is being updated.");
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
			}

			if (e.PropertyName == nameof(Project.CurrentJob))
			{
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			}
		}

		public bool CurrentProjectIsDirty => CurrentProject?.IsDirty ?? false;

		public bool IsCurrentJobIdChanged => CurrentProject?.IsCurrentJobIdChanged ?? false;


		public string? CurrentProjectName => CurrentProject?.Name;
		public bool CurrentProjectOnFile => CurrentProject?.OnFile ?? false;
		//public bool CanSaveProject => CurrentProjectOnFile;

		public Job CurrentJob => CurrentProject?.CurrentJob ?? new Job();

		public bool CanGoBack => CurrentProject?.CanGoBack ?? false;
		public bool CanGoForward => CurrentProject?.CanGoForward ?? false;

		public ColorBandSet CurrentColorBandSet => CurrentProject?.CurrentColorBandSet ?? new ColorBandSet();

		#endregion

		#region Public Methods -- Project

		public void ProjectStartNew(RRectangle coords, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			if (mapCalcSettings.TargetIterations != colorBandSet.HighCutoff)
			{
				Debug.WriteLine($"WARNING: Job's ColorMap HighCutoff doesn't match the TargetIterations. At ProjectStartNew.");
			}

			var projectId = ObjectId.Empty;

			var job = MapJobHelper.BuildJob(null, projectId, CanvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.None, null, _blockSize, _projectAdapter);
			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			CurrentProject = new Project("New", description: null, new List<Job> { job }, new List<ColorBandSet> { colorBandSet }, currentJobId: job.Id);
		}

		public bool ProjectOpen(string projectName)
		{
			if (_projectAdapter.TryGetProject(projectName, out var project))
			{
				CurrentProject = project;

				var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, _blockSize);
				if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
				{
					FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public void ProjectSave()
		{
			var project = CurrentProject;

			if (project != null)
			{
				if (!CurrentProjectOnFile)
				{
					throw new InvalidOperationException("Cannot save an unloaded project, use SaveProject instead.");
				}

				Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

				project.Save(_projectAdapter);

				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectIsDirty));
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectOnFile));

				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			}
		}

		public bool ProjectSaveAs(string name, string? description)
		{
			var currentProject = CurrentProject;

			if (currentProject == null)
			{
				return false;
			}

			if (_projectAdapter.TryGetProject(name, out var existingProject))
			{
				_projectAdapter.DeleteProject(existingProject.Id);
			}

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

			var project = _projectAdapter.CreateNewProject(name, description, currentProject.GetJobs(), currentProject.GetColorBandSets());

			if (project is null)
			{
				return false;
			}
			else
			{
				project.CurrentJob = currentProject.CurrentJob;
				project.Save(_projectAdapter);
				CurrentProject = project;

				return true;
			}
		}

		public long? DeleteMapSectionsSinceLastSave()
		{
			if (CurrentProject is null)
			{
				return 0;
			}

			var lastSaved = CurrentProject.LastSavedUtc;
			var deleteCnt = _projectAdapter.DeleteMapSectionsSince(lastSaved);

			return deleteCnt;
		}

		#endregion

		#region Public Methods - Job

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			Debug.Assert(transformType == TransformType.ZoomIn || transformType == TransformType.Pan, "UpdateMapView received a TransformType other than ZoomIn or Pan.");
			if (CurrentProject == null)
			{
				return;
			}

			var curJob = CurrentJob;

			var position = curJob.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
			LoadMap(CurrentProject, curJob, curJob.ColorBandSetId, coords, curJob.MapCalcSettings, transformType, newArea);
		}

		// Currently Not Used.
		public void UpdateMapCoordinates(RRectangle coords)
		{
			if (CurrentProject == null)
			{
				return;
			}

			if (CurrentJob.Coords != coords)
			{
				LoadMap(CurrentProject, CurrentJob, CurrentJob.ColorBandSetId, coords, CurrentJob.MapCalcSettings, TransformType.CoordinatesUpdate, null);
			}
		}

		public void UpdateColorBandSet(ColorBandSet colorBandSet)
		{
			if (CurrentProject == null)
			{
				return;
			}

			Debug.Assert(CurrentColorBandSet.Id == CurrentJob.ColorBandSetId, "The project's CurrentColorBandSet and CurrentJob's ColorBandSet is out of sync.");

			if (CurrentColorBandSet == colorBandSet)
			{
				Debug.WriteLine($"MapProjectViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
				return;
			}

			var isTargetIterationsBeingUpdated = colorBandSet.HighCutoff != CurrentProject.CurrentColorBandSet.HighCutoff;
			Debug.WriteLine($"MapProjectViewModel is having its ColorBandSet value updated. Old = {CurrentProject.CurrentColorBandSet.Id}, New = {colorBandSet.Id} Iterations Updated = {isTargetIterationsBeingUpdated}.");

			if (isTargetIterationsBeingUpdated)
			{
				var targetIterations = colorBandSet.HighCutoff;

				if (CurrentJob.MapCalcSettings.TargetIterations != targetIterations)
				{
					var mapCalcSettings = new MapCalcSettings(targetIterations, CurrentJob.MapCalcSettings.RequestsPerJob);
					LoadMap(CurrentProject, CurrentJob, colorBandSet.Id, CurrentJob.Coords, mapCalcSettings, TransformType.IterationUpdate, null);
				}
			}
			else
			{
				CurrentProject.CurrentColorBandSet = colorBandSet;
			}

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
		}

		public RRectangle? GetUpdatedCoords(TransformType transformType, RectangleInt newArea)
		{
			if (CurrentProject == null)
			{
				return null;
			}

			var curJob = CurrentJob;

			if (newArea == new RectangleInt())
			{
				return curJob.Coords;
			}
			else
			{
				var position = curJob.Coords.Position;
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

			var cbsBefore = CurrentColorBandSet;

			if (CurrentProject.GoBack())
			{
				var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, _blockSize);
				if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
				{
					FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
				}

				if (CurrentColorBandSet != cbsBefore)
				{
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
				}

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

			var cbsBefore = CurrentColorBandSet;

			if (CurrentProject.GoForward())
			{
				var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, _blockSize);
				if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
				{
					FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
				}

				if (CurrentColorBandSet != cbsBefore)
				{
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
				}

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

		private void RerunWithNewDisplaySize(Project project)
		{
			var wasUpdated = false;

			var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, _blockSize);
			if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
			{
				FindOrCreateJobForNewCanvasSize(project, CurrentJob, currentCanvasSizeInBlocks);
				wasUpdated = true;
			}

			if (wasUpdated)
			{
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			}
		}

		private void LoadMap(Project project, Job? currentJob, ObjectId colorBandSetId, RRectangle coords, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newArea)
		{
			var job = MapJobHelper.BuildJob(currentJob?.Id, project.Id, CanvasSize, coords, colorBandSetId, mapCalcSettings, transformType, newArea, _blockSize, _projectAdapter);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(job);

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
		}

		private void FindOrCreateJobForNewCanvasSize(Project project, Job job, SizeInt newCanvasSizeInBlocks)
		{
			// Note if this job is itself a CanvasSizeUpdate Proxy Job, then its parent is used to conduct the search.
			if (project.TryGetCanvasSizeUpdateProxy(job, newCanvasSizeInBlocks, out var matchingProxy))
			{
				project.CurrentJob = matchingProxy;

				return;
			}

			// Make sure we use the original job and not a 'CanvasSizeUpdate Proxy Job'.
			var origJob = project.GetOriginalJob(job);

			if (origJob.CanvasSizeInBlocks == newCanvasSizeInBlocks)
			{
				project.CurrentJob = origJob;

				return;
			}

			// Create a new job
			job = origJob;

			var newCoords = RMapHelper.GetNewCoordsForNewCanvasSize(job.Coords, job.CanvasSizeInBlocks, newCanvasSizeInBlocks, job.Subdivision.SamplePointDelta, _blockSize);
			//var newMSetInfo = MSetInfo.UpdateWithNewCoords(job.MSetInfo, newCoords);

			var transformType = TransformType.CanvasSizeUpdate;
			RectangleInt? newArea = null;

			var newJob = MapJobHelper.BuildJob(job.Id, project.Id, CanvasSize, newCoords, CurrentColorBandSet.Id, job.MapCalcSettings, transformType, newArea, _blockSize, _projectAdapter);

			Debug.WriteLine($"Re-runing job. Current CanvasSize: {job.CanvasSizeInBlocks}, new CanvasSize: {newCanvasSizeInBlocks}.");
			Debug.WriteLine($"Starting Job with new coords: {newCoords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(newJob);
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

					if (CurrentProject != null)
					{
						CurrentProject.Dispose();
						CurrentProject = null;
					}
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

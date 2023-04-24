using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Input;
using System.Windows.Media;
using Windows.UI.WebUI;

namespace MSetExplorer
{
	internal class ProjectViewModel : ViewModelBase, IProjectViewModel, IDisposable
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly SizeInt _blockSize;

		private SizeInt _canvasSize;
		private Project? _currentProject;

		private ColorBandSet? _previewColorBandSet;

		//private Func<SizeInt> _getCanvasSizeFunc;

		#region Constructor

		public ProjectViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;
			_blockSize = blockSize;

			//_getCanvasSizeFunc = getCanvasSizeFunc;

			_canvasSize = new SizeInt();
			_currentProject = null;
			_previewColorBandSet = null;
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
					OnPropertyChanged(nameof(IProjectViewModel.CanvasSize));

					//if (CurrentProject != null)
					//{
					//	RerunWithNewDisplaySize(CurrentProject);
					//}
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

					OnPropertyChanged(nameof(IProjectViewModel.CurrentProject));
					OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
					OnPropertyChanged(nameof(IProjectViewModel.CurrentColorBandSet));
				}
			}
		}

		public bool CurrentProjectIsDirty => CurrentProject?.IsDirty ?? false;

		public int GetGetNumberOfDirtyJobs()
		{
			return CurrentProject?.GetNumberOfDirtyJobs() ?? 0;
		}

		public bool IsCurrentJobIdChanged => CurrentProject?.IsCurrentJobIdChanged ?? false;

		public string? CurrentProjectName => CurrentProject?.Name;
		public bool CurrentProjectOnFile => CurrentProject?.OnFile ?? false;

		public Job CurrentJob => CurrentProject?.CurrentJob ?? Job.Empty;

		public ColorBandSet CurrentColorBandSet
		{
			get => PreviewColorBandSet ?? CurrentProject?.CurrentColorBandSet ?? new ColorBandSet();
			set
			{
				var currentProject = CurrentProject;
				if (currentProject != null && !currentProject.CurrentJob.IsEmpty)
				{
					CheckCurrentProject(currentProject);

					// Discard the Preview ColorBandSet. 
					_previewColorBandSet = null;

					if (value.Id == CurrentColorBandSet.Id)
					{
						Debug.WriteLine($"ProjectViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
					}

					var targetIterations = value.HighCutoff;
					var currentJob = currentProject.CurrentJob;

					if (targetIterations != currentJob.MapCalcSettings.TargetIterations)
					{
						Debug.WriteLine($"ProjectViewModel is updating the Target Iterations. Current ColorBandSetId = {currentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");

						currentProject.Add(value);
						//var newMapCalcSettings = new MapCalcSettings(targetIterations, currentJob.MapCalcSettings.RequestsPerJob);
						//LoadMap(currentProject, currentJob, currentJob.Coords, value.Id, newMapCalcSettings, TransformType.IterationUpdate, null);
						AddNewIterationUpdateJob(currentProject, value);
					}
					else
					{
						Debug.WriteLine($"ProjectViewModel is updating the ColorBandSet. Current ColorBandSetId = {currentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");
						currentProject.CurrentColorBandSet = value;
					}

					OnPropertyChanged(nameof(IProjectViewModel.CurrentColorBandSet));
				}
			}
		}

		public ColorBandSet? PreviewColorBandSet
		{
			get => _previewColorBandSet;
			set
			{
				if (value != _previewColorBandSet)
				{
					if (value == null || CurrentJob == null)
					{
						_previewColorBandSet = value;
					}
					else
					{
						var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(value, CurrentJob.MapCalcSettings.TargetIterations);
						_previewColorBandSet = adjustedColorBandSet;
					}

					OnPropertyChanged(nameof(IProjectViewModel.CurrentColorBandSet));
				}
			}
		}

		#endregion

		#region Event Handlers

		private void CurrentProject_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Project.IsDirty))
			{
				OnPropertyChanged(nameof(IProjectViewModel.CurrentProjectIsDirty));
			}

			else if (e.PropertyName == nameof(Project.OnFile))
			{
				OnPropertyChanged(nameof(IProjectViewModel.CurrentProjectOnFile));
			}

			else if (e.PropertyName == nameof(Project.CurrentColorBandSet))
			{
				Debug.WriteLine("The ProjectViewModel is raising PropertyChanged: IProjectViewModel.CurrentColorBandSet as the Project's ColorBandSet is being updated.");
				OnPropertyChanged(nameof(IProjectViewModel.CurrentColorBandSet));
			}

			else if (e.PropertyName == nameof(Project.CurrentJob))
			{
				//OnPropertyChanged(nameof(IProjectViewModel.CanGoBack));
				//OnPropertyChanged(nameof(IProjectViewModel.CanGoForward));
				OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
			}
		}

		#endregion

		#region Public Methods -- Project

		public void ProjectStartNew(RRectangle coords, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
		{
			if (mapCalcSettings.TargetIterations != colorBandSet.HighCutoff)
			{
				Debug.WriteLine($"WARNING: Job's ColorMap HighCutoff doesn't match the TargetIterations. At ProjectStartNew.");
			}

			var mapAreaInfo = RMapConstants.BuildHomeArea();

			var job = _mapJobHelper.BuildHomeJob(mapAreaInfo, colorBandSet.Id, mapCalcSettings);
			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			CurrentProject = new Project("New", description: null, new List<Job> { job }, new List<ColorBandSet> { colorBandSet }, currentJobId: job.Id);
			job.ProjectId = CurrentProject.Id;
		}

		public bool ProjectOpen(string projectName)
		{
			bool result;

			if (_projectAdapter.TryGetProject(projectName, out var project))
			{
				CurrentProject = project;
				if (project.CurrentJob.IsEmpty)
				{
					Debug.WriteLine("Warning the current job is null or empty on Project Open.");
				}

				result = !project.CurrentJob.IsEmpty;
			}
			else
			{
				Debug.WriteLine($"Cannot find a project record for name = {projectName}.");
				result = false;
			}

			return result;
		}

		public bool ProjectSave()
		{
			var currentProject = CurrentProject;

			if (currentProject == null)
			{
				throw new InvalidOperationException("The project must be non-null.");
			}

			if (!currentProject.OnFile)
			{
				throw new InvalidOperationException("Cannot save a new project, use Save As instead.");
			}

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSave found the CurrentJob to be empty.");

			var result = JobOwnerHelper.Save(currentProject, _projectAdapter);

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSave has set the CurrentJob to be empty.");


			OnPropertyChanged(nameof(IProjectViewModel.CurrentProjectIsDirty));
			OnPropertyChanged(nameof(IProjectViewModel.CurrentProjectOnFile));

			return result;
		}

		public bool ProjectSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText)
		{
			var currentProject = CurrentProject;

			if (currentProject == null)
			{
				throw new InvalidOperationException("The project must be non-null.");
			}

			if (_projectAdapter.ProjectExists(name, out var projectId))
			{
				if (!ProjectAndMapSectionHelper.DeleteProject(projectId, _projectAdapter, _mapSectionAdapter, out var numberOfMapSectionsDeleted))
				{
					errorText = $"Could not delete existing project having name: {name}";
					return false;
				}
				else
				{
					Debug.WriteLine($"As new Project is being SavedAs, overwriting exiting project: {name}, {numberOfMapSectionsDeleted} Map Sections were deleted.");
				}
			}

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

			var project = (Project)JobOwnerHelper.CreateCopy(currentProject, name, description, _projectAdapter, _mapSectionAdapter);

			if (JobOwnerHelper.Save(project, _projectAdapter))
			{
				CurrentProject = project;
				errorText = null;
				return true;
			}
			else
			{
				errorText = "Could not save the new project record.";
				return false;
			}
		}

		public void ProjectClose()
		{
			CurrentProject = null;
		}

		public long DeleteMapSectionsForUnsavedJobs()
		{
			var currentProject = CurrentProject;

			if (currentProject is null)
			{
				return 0;
			}

			var result = JobOwnerHelper.DeleteMapSectionsForUnsavedJobs(currentProject, _mapSectionAdapter);

			return result;
		}

		#endregion

		#region Public Methods - Poster 

		public bool TryCreatePoster(string name, string? description, SizeInt posterSize, [MaybeNullWhen(false)] out Poster poster)
		{
			var curJob = CurrentJob;
			if (CurrentProject == null || curJob.IsEmpty)
			{
				throw new InvalidOperationException("Cannot create a poster, the current job is empty.");
			}

			//var coords = curJob.Coords;
			//var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(coords, CanvasSize);

			var colorBandSet = CurrentProject.CurrentColorBandSet;
			//var mapCalcSettings = curJob.MapCalcSettings;

			////var job = _mapJobHelper.BuildHomeJob(CanvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, _blockSize);
			//var job = _mapJobHelper.BuildHomeJob(mapAreaInfo, colorBandSet.Id, mapCalcSettings);

			var job = curJob.CreateNewCopy();

			Debug.WriteLine($"Starting job for new Poster with new coords: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			var sourceJobId = curJob.Id;

			poster = _projectAdapter.CreatePoster(name, description, sourceJobId, new List<Job> { job }, new List<ColorBandSet>{ colorBandSet });
			if (poster != null)
			{
				_ = JobOwnerHelper.Save(poster, _projectAdapter);
			}

			return poster != null;
		}

		#endregion

		#region Public Methods - Job

		public void UpdateMapView(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2? diagnosticAreaInfo)
		{
			Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");

			var currentProject = CurrentProject;

			if (currentProject == null)
			{
				return;
			}

			AddNewCoordinateUpdateJob(currentProject, transformType, panAmount, factor);
		}

		//// Currently Not Used.
		//public void UpdateMapCoordinates(RRectangle coords)
		//{
		//	if (CurrentProject == null)
		//	{
		//		return;
		//	}

		//	if (CurrentJob.Coords != coords)
		//	{
		//		LoadMap(CurrentProject, CurrentJob, coords, CurrentJob.ColorBandSetId, CurrentJob.MapCalcSettings, TransformType.CoordinatesUpdate, null);
		//	}
		//}

		//private bool UpdateColorBandSet(Project project, ColorBandSet colorBandSet)
		//{
		//	// Discard the Preview ColorBandSet. 
		//	_previewColorBandSet = null;

		//	var currentJob = project.CurrentJob;

		//	if (CurrentColorBandSet.Id != currentJob.ColorBandSetId)
		//	{
		//		Debug.WriteLine($"The project's CurrentColorBandSet and CurrentJob's ColorBandSet are out of sync. The CurrentColorBandSet has {CurrentColorBandSet.Count} bands. The CurrentJob IsEmpty = {CurrentJob.IsEmpty}.");
		//	}

		//	if (colorBandSet == CurrentColorBandSet)
		//	{
		//		Debug.WriteLine($"ProjectViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
		//		return false;
		//	}

		//	var targetIterations = colorBandSet.HighCutoff;

		//	if (targetIterations != currentJob.MapCalcSettings.TargetIterations)
		//	{
		//		project.Add(colorBandSet);

		//		Debug.WriteLine($"ProjectViewModel is updating the Target Iterations. Current ColorBandSetId = {project.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
		//		var mapCalcSettings = new MapCalcSettings(targetIterations, currentJob.MapCalcSettings.RequestsPerJob);
		//		LoadMap(project, currentJob, currentJob.Coords, colorBandSet.Id, mapCalcSettings, TransformType.IterationUpdate, null);
		//	}
		//	else
		//	{
		//		Debug.WriteLine($"ProjectViewModel is updating the ColorBandSet. Current ColorBandSetId = {project.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
		//		project.CurrentColorBandSet = colorBandSet;
		//	}

		//	return true;
		//}

		public MapAreaInfo2? GetUpdatedMapAreaInfo(TransformType transformType, RectangleInt screenArea, MapAreaInfo2 currentMapAreaInfo)
		{
			var currentJob = CurrentJob;

			if (currentJob.IsEmpty)
			{
				return null;
			}

			if (screenArea == new RectangleInt())
			{
				Debug.WriteLine("GetUpdatedJobInfo was given an empty newArea rectangle.");
				//return MapJobHelper.GetMapAreaInfo(curJob, CanvasSize);
				return currentJob.MapAreaInfo;
			}
			else
			{
 				var mapAreaInfo = BuildMapAreaInfo(currentMapAreaInfo, screenArea);
				return mapAreaInfo;
			}
		}

		public bool GoBack(bool skipPanJobs)
		{
			var result = CurrentProject?.GoBack(skipPanJobs) ?? false;
			return result;
		}

		public bool GoForward(bool skipPanJobs)
		{
			var result = CurrentProject?.GoForward(skipPanJobs) ?? false;
			return result;
		}

		public bool CanGoBack(bool skipPanJobs)
		{
			var result = CurrentProject?.CanGoBack(skipPanJobs) ?? false;
			return result;
		}

		public bool CanGoForward(bool skipPanJobs)
		{
			var result = CurrentProject?.CanGoForward(skipPanJobs) ?? false;
			return result;
		}

		#endregion

		#region Private Methods

		private void AddNewCoordinateUpdateJob(Project project, TransformType transformType, VectorInt panAmount, double factor)
		{
			var currentJob = project.CurrentJob;
			Debug.Assert(!currentJob.IsEmpty, "AddNewCoordinateUpdateJob was called while the current job is empty.");

			// Calculate the new Map Coordinates 
			var mapAreaInfo = currentJob.MapAreaInfo;

			MapAreaInfo2? newMapAreaInfo;

			if (transformType == TransformType.ZoomIn)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(mapAreaInfo, panAmount, factor);
			}
			else if (transformType == TransformType.Pan)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPan(mapAreaInfo, panAmount);
			}
			else if (transformType == TransformType.ZoomOut)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomCenter(mapAreaInfo, factor);
			}
			else
			{
				throw new InvalidOperationException($"AddNewCoordinateUpdateJob does not support a TransformType of {transformType}.");
			}

			var colorBandSetId = currentJob.ColorBandSetId;
			var mapCalcSettings = currentJob.MapCalcSettings;

			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, newMapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea: null);

			Debug.WriteLine($"Adding Project Job with new coords: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(job);

			OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
		}

		private void AddNewIterationUpdateJob(Project project, ColorBandSet colorBandSet)
		{
			var currentJob = project.CurrentJob;

			// Use the ColorBandSet's highCutoff to set the targetIterations of the current MapCalcSettings
			var targetIterations = colorBandSet.HighCutoff;
			var mapCalcSettings = MapCalcSettings.UpdateTargetIterations(currentJob.MapCalcSettings, targetIterations);

			// Use the current display size and Map Coordinates
			var mapAreaInfo = currentJob.MapAreaInfo;

			// This an iteration update with the same screen area
			var transformType = TransformType.IterationUpdate;
			var newScreenArea = new RectangleInt();

			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, mapAreaInfo, colorBandSet.Id, mapCalcSettings, transformType, newScreenArea);

			Debug.WriteLine($"Adding Project Job with new coords: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(job);

			OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
		}

		private MapAreaInfo2 BuildMapAreaInfo(MapAreaInfo2 currentMapAreaInfo, RectangleInt screenArea)
		{
			//var mapPosition = currentMapAreaInfo.Coords.Position;
			//var samplePointDelta = currentMapAreaInfo.Subdivision.SamplePointDelta;
			//var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);

			//// Use the specified canvasSize, instead of this job's current value for the CanvasSize :: Updated by DRM on 4/16
			//var mapSize = currentMapAreaInfo.CanvasSize;

			var zoomPoint = screenArea.GetCenter();

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(currentMapAreaInfo, zoomPoint, 3);

			return mapAreaInfo;
		}

		[Conditional("DEBUG")]
		private void CheckCurrentProject(IJobOwner jobOwner)
		{
			if (jobOwner.CurrentJob.IsEmpty)
			{
				Debug.WriteLine($"The CurrentJob IsEmpty = { CurrentJob.IsEmpty}.");
			}
			else
			{
				if (jobOwner.CurrentColorBandSetId != jobOwner.CurrentJob.ColorBandSetId)
				{
					Debug.WriteLine($"The JobOwner's CurrentColorBandSet and CurrentJob's ColorBandSet are out of sync. The CurrentColorBandSet has {CurrentColorBandSet.Count} bands.");
				}
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

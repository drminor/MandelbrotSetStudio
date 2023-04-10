using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

					if (value == CurrentColorBandSet)
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
				var currentProject = CurrentProject;

				if (currentProject != null)
				{
					var cbsBefore = CurrentColorBandSet;

					var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
					if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
					{
						//	Debug.WriteLine($"Finding-Or-Creating Job For New CanvasSize -- Current Job changing (ProjectViewModel).");
						//	FindOrCreateJobForNewCanvasSize(currentProject, CurrentJob, currentCanvasSizeInBlocks);

						// TODO: Check to make sure that simply setting the Current Job is sufficent to get a good display
						Debug.WriteLine($"The current Job's CanvasSizeInBlocks does not match the Current CanvasSizeInBlocks -- CurrentJob is changing -- (ProjectViewModel). NO ACTION TAKEN!");
					}

					if (CurrentColorBandSet != cbsBefore)
					{
						OnPropertyChanged(nameof(IProjectViewModel.CurrentColorBandSet));
					}

					//OnPropertyChanged(nameof(IProjectViewModel.CanGoBack));
					//OnPropertyChanged(nameof(IProjectViewModel.CanGoForward));
					OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
				}
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

			var job = _mapJobHelper.BuildHomeJob(CanvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, _blockSize);
			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			CurrentProject = new Project("New", description: null, new List<Job> { job }, new List<ColorBandSet> { colorBandSet }, currentJobId: job.Id);
			job.ProjectId = CurrentProject.Id;
		}

		public bool ProjectOpen(string projectName)
		{
			if (_projectAdapter.TryGetProject(projectName, out var project))
			{
				CurrentProject = project;

				if (project.CurrentJob.IsEmpty)
				{
					Debug.WriteLine("Warning the current job is null or empty on Project Open.");
					return false;
				}
				else
				{
					var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, project.CurrentJob.CanvasControlOffset, _blockSize);
					if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
					{
						//Debug.WriteLine($"Finding-Or-Creating Job For New CanvasSize -- Project Open (ProjectViewModel).");
						//FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);

						// TODO: Check to make sure that simply setting the Current Job is sufficent to get a good display
						Debug.WriteLine($"The current Job's CanvasSizeInBlocks does not match the Current CanvasSizeInBlocks -- Project Open (ProjectViewModel). NO ACTION TAKEN!");
					}

					return true;
				}
			}
			else
			{
				Debug.WriteLine($"Cannot find a project record for name = {projectName}.");
				return false;
			}
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

			var colorBandSet = CurrentProject.CurrentColorBandSet;
			var coords = curJob.Coords;
			var mapCalcSettings = curJob.MapCalcSettings;

			var job = _mapJobHelper.BuildHomeJob(CanvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, _blockSize);
			Debug.WriteLine($"Starting job for new Poster with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

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

		public void UpdateMapView(TransformType transformType, RectangleInt screenArea)
		{
			Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");

			var currentProject = CurrentProject;

			if (currentProject == null)
			{
				return;
			}

			//var mapPosition = curJob.Coords.Position;
			//var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			//var newCoords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);
			//LoadMap(CurrentProject, curJob, newCoords, curJob.ColorBandSetId, curJob.MapCalcSettings, transformType, screenArea);

			AddNewCoordinateUpdateJob(currentProject, transformType, screenArea);
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

		public MapAreaInfo? GetUpdatedMapAreaInfo(TransformType transformType, RectangleInt screenArea)
		{
			var curJob = CurrentJob;

			if (curJob.IsEmpty)
			{
				return null;
			}

			if (screenArea == new RectangleInt())
			{
				Debug.WriteLine("GetUpdatedJobInfo was given an empty newArea rectangle.");
				//return MapJobHelper.GetMapAreaInfo(curJob, CanvasSize);
				return curJob.MapAreaInfo;
			}
			else
			{
				var mapPosition = curJob.Coords.Position;
				var samplePointDelta = curJob.Subdivision.SamplePointDelta;
				var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);
 				var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(coords, CanvasSize, _blockSize);

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

		private void AddNewCoordinateUpdateJob(Project project, TransformType transformType, RectangleInt newScreenArea)
		{
			var currentJob = project.CurrentJob;
			Debug.Assert(!currentJob.IsEmpty, "AddNewCoordinateUpdateJob was called while the current job is empty.");

			// Calculate the new Map Coordinates from the newScreenArea, using the current Map's position and samplePointDelta.
			var mapPosition = currentJob.Coords.Position;
			var samplePointDelta = currentJob.Subdivision.SamplePointDelta;
			var newCoords = RMapHelper.GetMapCoords(newScreenArea, mapPosition, samplePointDelta);

			// Use the current display size, colors and iterations.
			var mapSize = currentJob.CanvasSize;
			var colorBandSetId = currentJob.ColorBandSetId;
			var mapCalcSettings = currentJob.MapCalcSettings;

			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, mapSize, newCoords, colorBandSetId, mapCalcSettings, transformType, newScreenArea, _blockSize);

			Debug.WriteLine($"Adding Project Job with new coords: {newCoords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

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
			var mapSize = currentJob.CanvasSize;
			var coords = currentJob.MapAreaInfo.Coords;

			// This an iteration update with the same screen area
			var transformType = TransformType.IterationUpdate;
			var newScreenArea = new RectangleInt();

			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, mapSize, coords, colorBandSet.Id, mapCalcSettings, transformType, newScreenArea, _blockSize);

			Debug.WriteLine($"Adding Project Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(job);

			OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
		}

		//private void LoadMap(Project project, Job currentJob, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newArea)
		//{
		//	var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, CanvasSize, coords, colorBandSetId, mapCalcSettings, transformType, newArea, _blockSize);

		//	Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

		//	project.Add(job);

		//	OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
		//}

		private void RerunWithNewDisplaySize(Project project)
		{
			var wasUpdated = false;

			var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
			if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
			{
				//Debug.WriteLine($"Finding-Or-Creating Job For New CanvasSize -- MapControl Size is changing (ProjectViewModel).");
				//FindOrCreateJobForNewCanvasSize(project, CurrentJob, currentCanvasSizeInBlocks);
				//wasUpdated = true;

				// TODO: Check to make sure that simply setting the Current Job is sufficent to get a good display
				Debug.WriteLine($"The current Job's CanvasSizeInBlocks does not match the Current CanvasSizeInBlocks -- CanvasSize is changing -- (ProjectViewModel) NO ACTION TAKEN!");

				wasUpdated = false;

			}

			if (wasUpdated)
			{
				OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
			}
		}

		private void FindOrCreateJobForNewCanvasSize(Project project, Job job, SizeInt newCanvasSizeInBlocks)
		{
			// Note if this job is itself a CanvasSizeUpdate Proxy Job, then its parent is used to conduct the search.
			if (project.TryGetCanvasSizeUpdateProxy(job, newCanvasSizeInBlocks, out var matchingProxy))
			{
				Debug.WriteLine("Found existing CanvasSizeUpdate Job.");
				project.CurrentJob = matchingProxy;
				return;
			}

			// Make sure we use the original job and not a 'CanvasSizeUpdate Proxy Job'.
			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				var preferredJob = project.GetParent(job);

				if (preferredJob is null)
				{
					throw new InvalidOperationException("Could not get the preferred job as we create a new job for the updated canvas size.");
				}

				job = preferredJob;
			}

			var newCoords = RMapHelper.GetNewCoordsForNewCanvasSize(job.Coords, job.CanvasSizeInBlocks, newCanvasSizeInBlocks, job.Subdivision);

			var transformType = TransformType.CanvasSizeUpdate;
			RectangleInt? newArea = null;

			var newJob = _mapJobHelper.BuildJob(job.Id, project.Id, CanvasSize, newCoords, job.ColorBandSetId, job.MapCalcSettings, transformType, newArea, _blockSize);

			Debug.WriteLine($"Creating CanvasSizeUpdate Job. Current CanvasSize: {job.CanvasSizeInBlocks}, new CanvasSize: {newCanvasSizeInBlocks}.");
			Debug.WriteLine($"Starting Job with new coords: {newCoords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(newJob);
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

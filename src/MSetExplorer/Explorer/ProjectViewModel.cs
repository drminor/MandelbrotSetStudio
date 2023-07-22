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

		private Project? _currentProject;

		private ColorBandSet? _previewColorBandSet;

		#region Constructor

		public ProjectViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;

			_currentProject = null;
			_previewColorBandSet = null;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

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

			var job = _mapJobHelper.BuildHomeJob(OwnerType.Project, mapAreaInfo, colorBandSet.Id, mapCalcSettings);
			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			CurrentProject = new Project("New", description: null, new List<Job> { job }, new List<ColorBandSet> { colorBandSet }, currentJobId: job.Id);
			job.OwnerId = CurrentProject.Id;
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

			var result = JobOwnerHelper.SaveProject(currentProject, _projectAdapter);

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

			if (JobOwnerHelper.SaveProject(project, _projectAdapter))
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


			// TODO: Make sure that the Project's Current Job (as it exists on the repository) points to an existing job
			var result = JobOwnerHelper.DeleteMapSectionsForUnsavedJobs(currentProject, _mapSectionAdapter);

			return result;
		}

		#endregion

		#region Public Methods - Poster 

		public bool TryCreatePoster(string name, string? description, SizeDbl posterSize, [NotNullWhen(true)] out Poster? poster)
		{
			var curJob = CurrentJob;
			if (CurrentProject == null || curJob.IsEmpty)
			{
				throw new InvalidOperationException("Cannot create a poster, the current job is empty.");
			}

			var colorBandSet = CurrentProject.CurrentColorBandSet;

			var sourceJobId = curJob.Id;

			// Create a copy of the current job, commit it to the repo and get the new job with the updated Id on file.
			var newCopy = curJob.CreateNewCopy();
			newCopy.JobOwnerType = OwnerType.Poster;
			var newJobId = _projectAdapter.InsertJob(newCopy);
			var job = _projectAdapter.GetJob(newJobId);

			Debug.WriteLine($"Starting job for new Poster: SourceJobId: {sourceJobId} with Position&Delta: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			var newPoster = _projectAdapter.CreatePoster(name, description, posterSize, sourceJobId, new List<Job> { job }, new List<ColorBandSet>{ colorBandSet });

			if (newPoster == null)
			{
				poster = null;
				return false;
			}
			else
			{
				poster = newPoster;

				// This will update the OwnerId of the new Job and ColorBandSet and commit the updates to the repo.
				_ = JobOwnerHelper.SavePoster(poster, _projectAdapter);
				
				return true;
			}
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

		//public MapAreaInfo2? GetUpdatedMapAreaInfo(TransformType transformType, RectangleInt screenArea, MapAreaInfo2 currentMapAreaInfo)
		//{
		//	var currentJob = CurrentJob;

		//	if (currentJob.IsEmpty)
		//	{
		//		return null;
		//	}

		//	if (screenArea == new RectangleInt())
		//	{
		//		Debug.WriteLine("GetUpdatedJobInfo was given an empty newArea rectangle.");
		//		//return MapJobHelper.GetMapAreaInfo(curJob, CanvasSize);
		//		return currentJob.MapAreaInfo;
		//	}
		//	else
		//	{
		//		//var mapAreaInfo = BuildMapAreaInfo(currentMapAreaInfo, screenArea);
		//		//return mapAreaInfo;

		//		var zoomPoint = screenArea.GetCenter();
		//		var mapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(currentMapAreaInfo, zoomPoint, 3);
		//		return mapAreaInfo;
		//	}
		//}

		public MapAreaInfo2 GetUpdatedMapAreaInfo(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2 currentMapAreaInfo)
		{
			Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "GetUpdatedMapAreaInfo received a TransformType other than ZoomIn, Pan or ZoomOut.");

			CheckProvidedMapAreaInfo(currentMapAreaInfo);

			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPanThenZoom(currentMapAreaInfo, panAmount, factor, out var diaReciprocal);

			return newMapAreaInfo;
		}

		[Conditional("DEBUG2")]
		private void CheckProvidedMapAreaInfo(MapAreaInfo2 currentMapAreaInfo)
		{
			var currentJob = CurrentProject?.CurrentJob;

			Debug.Assert(currentJob != null && !currentJob.IsEmpty, "AddNewCoordinateUpdateJob was called while the current job is empty.");

			var mapAreaInfo = currentJob.MapAreaInfo;

			Debug.Assert(currentMapAreaInfo.Equals(mapAreaInfo), "The provided MapAreaInfo is not the same as the Current Project's Curernt Job's MapAreaInfo.");
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

			MapAreaInfo2 newMapAreaInfo;

			if (transformType == TransformType.ZoomIn)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPanThenZoom(mapAreaInfo, panAmount, factor, out var diaReciprocal);
			}
			else if (transformType == TransformType.Pan)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPan(mapAreaInfo, panAmount);
			}
			else if (transformType == TransformType.ZoomOut)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoom(mapAreaInfo, factor, out var diaReciprocal);
			}
			else
			{
				throw new InvalidOperationException($"AddNewCoordinateUpdateJob does not support a TransformType of {transformType}.");
			}

			var colorBandSetId = currentJob.ColorBandSetId;
			var mapCalcSettings = currentJob.MapCalcSettings;

			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, OwnerType.Project, newMapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea: null);

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

			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, OwnerType.Project, mapAreaInfo, colorBandSet.Id, mapCalcSettings, transformType, newScreenArea);

			Debug.WriteLine($"Adding Project Job with new coords: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(job);

			OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
		}

		[Conditional("DEBUG2")]
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

using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSetExplorer
{
	internal class MapProjectViewModel : ViewModelBase, IMapProjectViewModel, IDisposable
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly SizeInt _blockSize;

		private SizeInt _canvasSize;
		private Project? _currentProject;

		#region Constructor

		public MapProjectViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;
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
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
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

		public Job CurrentJob
		{
			get => CurrentProject?.CurrentJob ?? Job.Empty;
			private set 
			{
				//if (CurrentProject != null)
				//{
				//	CurrentProject.CurrentJob = value;
				//}
			}
		}

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

			var job = _mapJobHelper.BuildJob(null, projectId, CanvasSize, coords, colorBandSet.Id, mapCalcSettings, TransformType.Home, null, _blockSize);
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
						FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
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

			if (!CurrentProjectOnFile)
			{
				throw new InvalidOperationException("Cannot save a new project, use Save As instead.");
			}

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSave found the CurrentJob to be empty.");

			var result = currentProject.Save(_projectAdapter);

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectIsDirty));
			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectOnFile));

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));

			return result;
		}

		public void ProjectSaveAs(string name, string? description)
		{
			var currentProject = CurrentProject;

			if (currentProject == null)
			{
				throw new InvalidOperationException("The project must be non-null.");
			}

			ProjectAndMapSectionHelper.DeleteProject(name, _projectAdapter, _mapSectionAdapter);

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

			// TODO: Update the JobTree with a new Clone or Copy method. 
			var jobPairs = currentProject.GetJobs().Select(x => new Tuple<ObjectId, Job>(x.Id, x.CreateNewCopy())).ToArray();
			var jobs = jobPairs.Select(x => x.Item2).ToArray();

			foreach(var oldIdAndNewJob in jobPairs)
			{
				var formerJobId = oldIdAndNewJob.Item1;
				var newJobId = oldIdAndNewJob.Item2.Id;
				UpdateJobParents(formerJobId, newJobId, jobs);

				var numberJobMapSectionRefsCreated = _mapSectionAdapter.DuplicateJobMapSections(formerJobId, JobOwnerType.Project, newJobId);
				Debug.WriteLine($"{numberJobMapSectionRefsCreated} new JobMapSectionRecords were created as Job: {formerJobId} was duplicated.");
			}

			var colorBandSetPairs = currentProject.GetColorBandSets().Select(x => new Tuple<ObjectId, ColorBandSet>(x.Id, x.CreateNewCopy())).ToArray();
			var colorBandSets = colorBandSetPairs.Select(x => x.Item2).ToArray();

			foreach (var oldIdAndNewCbs in colorBandSetPairs)
			{
				UpdateCbsParentIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, colorBandSets);
				UpdateJobCbsIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, jobs);
			}

			var project = _projectAdapter.CreateProject(name, description, jobs, colorBandSets);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new project.");
			}

			var firstOldIdAndNewJob = jobPairs.FirstOrDefault(x => x.Item1 == currentProject.CurrentJobId);
			var newCurJob = firstOldIdAndNewJob?.Item2;
			project.CurrentJob = newCurJob ?? Job.Empty;

			var firstOldIdAndNewCbs = colorBandSetPairs.FirstOrDefault(x => x.Item1 == currentProject.CurrentColorBandSet.Id);
			var newCurCbs = firstOldIdAndNewCbs?.Item2;

			project.CurrentColorBandSet = newCurCbs ?? new ColorBandSet();

			_ = project.Save(_projectAdapter);
			CurrentProject = project;
		}

		private void UpdateJobParents(ObjectId oldParentId, ObjectId newParentId, Job[] jobs)
		{
			foreach(var job in jobs)
			{
				if (job.ParentJobId == oldParentId)
				{
					job.ParentJobId = newParentId;
				}
			}
		}

		private void UpdateCbsParentIds(ObjectId oldParentId, ObjectId newParentId, ColorBandSet[] colorBandSets)
		{
			foreach (var cbs in colorBandSets)
			{
				if (cbs.ParentId == oldParentId)
				{
					cbs.ParentId = newParentId;
				}
			}
		}

		private void UpdateJobCbsIds(ObjectId oldCbsId, ObjectId newCbsId, Job[] jobs)
		{
			foreach (var job in jobs)
			{
				if (job.ColorBandSetId == oldCbsId)
				{
					job.ColorBandSetId = newCbsId;
				}
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

			var result = 0L;

			var jobs = currentProject.GetJobs().Where(x => !x.OnFile).ToList();

			foreach(var job in jobs)
			{
				var numberDeleted = _mapSectionAdapter.DeleteMapSectionsForJob(job.Id, JobOwnerType.Project);
				if (numberDeleted.HasValue)
				{
					result += numberDeleted.Value;
				}
			}

			return result;
		}

		#endregion

		#region Public Methods - Poster 

		public Poster PosterCreate(string name, string? description, SizeInt posterSize)
		{
			var curJob = CurrentJob;
			if (CurrentProject == null || curJob.IsEmpty)
			{
				throw new InvalidOperationException("Cannot create a poster, the current job is empty.");
			}

			var colorBandSet = CurrentProject.CurrentColorBandSet;
			var blockSize = curJob.Subdivision.BlockSize;

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(curJob.Coords, posterSize, blockSize);
			var poster = new Poster(name, description, curJob.Id, mapAreaInfo, colorBandSet, curJob.MapCalcSettings);

			_projectAdapter.CreatePoster(poster);

			return poster;
		}

		#endregion

		#region Public Methods - Job

		public void UpdateMapView(TransformType transformType, RectangleInt screenArea)
		{
			Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");
			if (CurrentProject == null)
			{
				return;
			}

			var curJob = CurrentJob;

			var mapPosition = curJob.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);
			LoadMap(CurrentProject, curJob, coords, curJob.ColorBandSetId, curJob.MapCalcSettings, transformType, screenArea);
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

		public void UpdateColorBandSet(ColorBandSet colorBandSet)
		{
			if (CurrentProject == null)
			{
				return;
			}

			if (CurrentColorBandSet.Id != CurrentJob.ColorBandSetId)
			{
				Debug.WriteLine($"The project's CurrentColorBandSet and CurrentJob's ColorBandSet are out of sync. The CurrentColorBandSet has {CurrentColorBandSet.Count} bands. The CurrentJob IsEmpty = {CurrentJob.IsEmpty}.");
			}

			if (CurrentColorBandSet == colorBandSet)
			{
				Debug.WriteLine($"MapProjectViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
				return;
			}

			var targetIterations = colorBandSet.HighCutoff;

			if (targetIterations != CurrentJob.MapCalcSettings.TargetIterations)
			{
				CurrentProject.Add(colorBandSet);

				Debug.WriteLine($"MapProjectViewModel is updating the Target Iterations. Current ColorBandSetId = {CurrentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				var mapCalcSettings = new MapCalcSettings(targetIterations, CurrentJob.MapCalcSettings.RequestsPerJob);
				LoadMap(CurrentProject, CurrentJob, CurrentJob.Coords, colorBandSet.Id, mapCalcSettings, TransformType.IterationUpdate, null);
			}
			else
			{
				Debug.WriteLine($"MapProjectViewModel is updating the ColorBandSet. Current ColorBandSetId = {CurrentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				CurrentProject.CurrentColorBandSet = colorBandSet;

				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
			}
		}

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
			if (CurrentProject == null)
			{
				return false;
			}

			var cbsBefore = CurrentColorBandSet;

			if (CurrentProject.GoBack(skipPanJobs))
			{
				var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
				if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
				{
					FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
				}

				if (CurrentColorBandSet != cbsBefore)
				{
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
				}

				//OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));

				return true;
			}
			else
			{
				return false;
			}
		}

		public bool GoForward(bool skipPanJobs)
		{
			if (CurrentProject == null)
			{
				return false;
			}

			var cbsBefore = CurrentColorBandSet;

			if (CurrentProject.GoForward(skipPanJobs))
			{
				var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
				if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
				{
					FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
				}

				if (CurrentColorBandSet != cbsBefore)
				{
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
				}

				//OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
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

			var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
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

		private void LoadMap(Project project, Job currentJob, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newArea)
		{
			// THE JOB TREE now contains the logic for determining if the new job will be a child or a sibling of the source job.
			// TODO: Verify that the currentJob is indeed the current job that should be used for the new Job's source.

			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, CanvasSize, coords, colorBandSetId, mapCalcSettings, transformType, newArea, _blockSize);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			project.Add(job);
			//ProjectJobAdded?.Invoke(this, new ProjectJobAddedEventArgs(job));

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

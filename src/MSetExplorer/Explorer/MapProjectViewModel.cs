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

		private ColorBandSet? _previewColorBandSet;

		#region Constructor

		public MapProjectViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;
			_blockSize = blockSize;

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
					OnPropertyChanged(nameof(IMapProjectViewModel.ColorBandSet));
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
				OnPropertyChanged(nameof(IMapProjectViewModel.ColorBandSet));
			}

			if (e.PropertyName == nameof(Project.CurrentJob))
			{
				var currentProject = CurrentProject;

				if (currentProject != null)
				{
					var cbsBefore = ColorBandSet;
					var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
					if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
					{
						FindOrCreateJobForNewCanvasSize(currentProject, CurrentJob, currentCanvasSizeInBlocks);
					}

					if (ColorBandSet != cbsBefore)
					{
						OnPropertyChanged(nameof(IMapProjectViewModel.ColorBandSet));
					}

					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				}
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

		public ColorBandSet ColorBandSet
		{
			get => PreviewColorBandSet ?? CurrentProject?.CurrentColorBandSet ?? new ColorBandSet();
			set
			{
				var currentProject = CurrentProject;
				if (currentProject != null)
				{
					if (UpdateColorBandSet(currentProject, value))
					{
						OnPropertyChanged(nameof(IMapProjectViewModel.ColorBandSet));
					}
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

					OnPropertyChanged(nameof(IMapProjectViewModel.ColorBandSet));
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

			if (!currentProject.OnFile)
			{
				throw new InvalidOperationException("Cannot save a new project, use Save As instead.");
			}

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSave found the CurrentJob to be empty.");

			var result = Save(currentProject, _projectAdapter);

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

			var project = CreateCopy(currentProject, name, description, _projectAdapter, _mapSectionAdapter);

			Save(currentProject, _projectAdapter);

			CurrentProject = project;
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

			var result = DeleteMapSectionsForUnsavedJobs(currentProject, _mapSectionAdapter);

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

		private bool UpdateColorBandSet(Project project, ColorBandSet colorBandSet)
		{
			// Discard the Preview ColorBandSet. 
			_previewColorBandSet = null;

			var currentJob = project.CurrentJob;

			if (ColorBandSet.Id != currentJob.ColorBandSetId)
			{
				Debug.WriteLine($"The project's CurrentColorBandSet and CurrentJob's ColorBandSet are out of sync. The CurrentColorBandSet has {ColorBandSet.Count} bands. The CurrentJob IsEmpty = {CurrentJob.IsEmpty}.");
			}

			if (ColorBandSet == colorBandSet)
			{
				Debug.WriteLine($"MapProjectViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
				return false;
			}

			var targetIterations = colorBandSet.HighCutoff;

			if (targetIterations != currentJob.MapCalcSettings.TargetIterations)
			{
				project.Add(colorBandSet);

				Debug.WriteLine($"MapProjectViewModel is updating the Target Iterations. Current ColorBandSetId = {project.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				var mapCalcSettings = new MapCalcSettings(targetIterations, currentJob.MapCalcSettings.RequestsPerJob);
				LoadMap(project, currentJob, currentJob.Coords, colorBandSet.Id, mapCalcSettings, TransformType.IterationUpdate, null);
			}
			else
			{
				Debug.WriteLine($"MapProjectViewModel is updating the ColorBandSet. Current ColorBandSetId = {project.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				project.CurrentColorBandSet = colorBandSet;
			}

			return true;
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

			var result = CurrentProject.GoBack(skipPanJobs);
			return result;
			//var cbsBefore = ColorBandSet;

			//if (CurrentProject.GoBack(skipPanJobs))
			//{
			//	var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
			//	if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
			//	{
			//		FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
			//	}

			//	if (ColorBandSet != cbsBefore)
			//	{
			//		OnPropertyChanged(nameof(IMapProjectViewModel.ColorBandSet));
			//	}

			//	//OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			//	OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
			//	OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));

			//	return true;
			//}
			//else
			//{
			//	return false;
			//}
		}

		public bool GoForward(bool skipPanJobs)
		{
			if (CurrentProject == null)
			{
				return false;
			}

			var result = CurrentProject.GoForward(skipPanJobs);
			return result;
			//var cbsBefore = ColorBandSet;

			//if (CurrentProject.GoForward(skipPanJobs))
			//{
			//	var currentCanvasSizeInBlocks = RMapHelper.GetMapExtentInBlocks(CanvasSize, CurrentJob.CanvasControlOffset, _blockSize);
			//	if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
			//	{
			//		FindOrCreateJobForNewCanvasSize(CurrentProject, CurrentJob, currentCanvasSizeInBlocks);
			//	}

			//	if (ColorBandSet != cbsBefore)
			//	{
			//		OnPropertyChanged(nameof(IMapProjectViewModel.ColorBandSet));
			//	}

			//	//OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			//	OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
			//	OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));

			//	return true;
			//}
			//else
			//{
			//	return false;
			//}
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
			var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, CanvasSize, coords, colorBandSetId, mapCalcSettings, transformType, newArea, _blockSize);

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

		#region Private Methods -- Saving

		private bool Save(Project project, IProjectAdapter projectAdapter)
		{
			if (!(project.IsCurrentJobIdChanged || project.IsDirty))
			{
				Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
				return false;
			}

			if (IsCurrentJobIdChanged)
			{
				projectAdapter.UpdateProjectCurrentJobId(project.Id, project.CurrentJobId);
			}

			if (project.IsDirty)
			{
				projectAdapter.UpdateProjectName(project.Id, project.Name);
				projectAdapter.UpdateProjectDescription(project.Id, project.Description);
				SaveColorBandSets(project, projectAdapter);
				SaveJobs(project, projectAdapter);

				project.MarkAsSaved();
			}

			return true;
		}

		public Project CreateCopy(Project sourceProject, string name, string? description, IProjectAdapter projectAdapter, IMapSectionDuplicator mapSectionDuplicator)
		{
			// TODO: Update the JobTree with a new Clone or Copy method. 
			var jobPairs = sourceProject.GetJobs().Select(x => new Tuple<ObjectId, Job>(x.Id, x.CreateNewCopy())).ToArray();
			var jobs = jobPairs.Select(x => x.Item2).ToArray();

			foreach (var oldIdAndNewJob in jobPairs)
			{
				var formerJobId = oldIdAndNewJob.Item1;
				var newJobId = oldIdAndNewJob.Item2.Id;
				UpdateJobParents(formerJobId, newJobId, jobs);

				var numberJobMapSectionRefsCreated = mapSectionDuplicator.DuplicateJobMapSections(formerJobId, JobOwnerType.Project, newJobId);
				Debug.WriteLine($"{numberJobMapSectionRefsCreated} new JobMapSectionRecords were created as Job: {formerJobId} was duplicated.");
			}

			var colorBandSetPairs = sourceProject.GetColorBandSets().Select(x => new Tuple<ObjectId, ColorBandSet>(x.Id, x.CreateNewCopy())).ToArray();
			var colorBandSets = colorBandSetPairs.Select(x => x.Item2).ToArray();

			foreach (var oldIdAndNewCbs in colorBandSetPairs)
			{
				UpdateCbsParentIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, colorBandSets);
				UpdateJobCbsIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, jobs);
			}

			var project = projectAdapter.CreateProject(name, description, jobs, colorBandSets);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new project.");
			}

			var firstOldIdAndNewJob = jobPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentJobId);
			var newCurJob = firstOldIdAndNewJob?.Item2;
			project.CurrentJob = newCurJob ?? Job.Empty;

			var firstOldIdAndNewCbs = colorBandSetPairs.FirstOrDefault(x => x.Item1 == ColorBandSet.Id);
			var newCurCbs = firstOldIdAndNewCbs?.Item2;

			project.CurrentColorBandSet = newCurCbs ?? new ColorBandSet();

			return project;
		}

		private long DeleteMapSectionsForUnsavedJobs(Project project, IMapSectionDeleter mapSectionDeleter)
		{
			var result = 0L;

			var jobs = project.GetJobs().Where(x => !x.OnFile).ToList();

			foreach (var job in jobs)
			{
				var numberDeleted = mapSectionDeleter.DeleteMapSectionsForJob(job.Id, JobOwnerType.Project);
				if (numberDeleted.HasValue)
				{
					result += numberDeleted.Value;
				}
			}

			return result;
		}

		private void SaveColorBandSets(Project project, IProjectAdapter projectAdapter)
		{
			var colorBandSets = project.GetColorBandSets();
			var unsavedColorBandSets = colorBandSets.Where(x => x.OnFile).ToList();

			foreach (var cbs in unsavedColorBandSets)
			{
				if (cbs.ProjectId != project.Id)
				{
					Debug.WriteLine($"WARNING: ColorBandSet has a different projectId than the current projects. ColorBandSetId: {cbs.ProjectId}, current Project: {project.Id}.");
					var newCbs = cbs.Clone();
					newCbs.ProjectId = project.Id;
					projectAdapter.InsertColorBandSet(newCbs);
				}
				else
				{
					projectAdapter.InsertColorBandSet(cbs);
				}
			}

			var dirtyColorBandSets = colorBandSets.Where(x => x.IsDirty).ToList();

			foreach (var cbs in dirtyColorBandSets)
			{
				projectAdapter.UpdateColorBandSetDetails(cbs);
			}
		}

		private void SaveJobs(Project project, IProjectAdapter projectAdapter)
		{
			//_jobTree.SaveJobs(projectId, projectAdapter);

			var jobs = project.GetJobs();

			var unSavedJobs = jobs.Where(x => !x.OnFile).ToList();

			foreach (var job in unSavedJobs)
			{
				job.ProjectId = project.Id;
				projectAdapter.InsertJob(job);
			}

			var dirtyJobs = jobs.Where(x => x.IsDirty).ToList();

			foreach (var job in dirtyJobs)
			{
				projectAdapter.UpdateJobDetails(job);
			}
		}

		private void UpdateJobParents(ObjectId oldParentId, ObjectId newParentId, Job[] jobs)
		{
			foreach (var job in jobs)
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

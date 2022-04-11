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

			_jobsCollection = new JobCollection(/*projectAdapter*/);
			_colorBandSetCollection = new ColorBandSetCollection(/*projectAdapter*/);
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

		public Job? CurrentJob => _jobsCollection.CurrentJob;

		public bool CanGoBack => _jobsCollection.CanGoBack;
		public bool CanGoForward => _jobsCollection.CanGoForward;

		public ColorBandSet CurrentColorBandSet
		{
			get => _colorBandSetCollection.CurrentColorBandSet;
			set
			{
				if (value != _colorBandSetCollection.CurrentColorBandSet)
				{
					Debug.WriteLine($"MapProjectViewModel is having its ColorBandSet value updated. Old = {_colorBandSetCollection.CurrentColorBandSet?.Id}, New = {value.Id}.");
					_colorBandSetCollection.Push(value);
					CurrentProjectIsDirty = true;
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
				}
			}
		}

		#endregion

		#region Public Methods -- Project

		public void ProjectStartNew(MSetInfo mSetInfo, ColorBandSet colorBandSet)
		{
			CurrentProject = new Project("New", description: null, currentJobId: null, colorBandSet.Id);

			_jobsCollection.Clear();

			_colorBandSetCollection.Load(new List<ColorBandSet>() { colorBandSet }, null);
			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			LoadMap(mSetInfo, TransformType.None);

			CurrentProjectIsDirty = false;
		}

		public void ProjectCreate(string name, string description, ObjectId currentColorBandSetId)
		{
			if (_projectAdapter.TryGetProject(name, out var _))
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}

			var project = _projectAdapter.CreateProject(name, description, currentJobId: null, currentColorBandSetId);
			LoadProject(project);
			CurrentProjectIsDirty = true;
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
			_colorBandSetCollection.Load(colorBandSets, project.CurrentColorBandSetId);
			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			var jobs = _projectAdapter.GetAllJobs(CurrentProject.Id);
			_jobsCollection.Load(jobs, currentId: project.CurrentJobId);

			var curJob = CurrentJob;
			if (curJob != null)
			{
				DoWithWriteLock(() => 
				{
					_ = UpdateTheJobsCanvasSize(curJob);
				});
			}

			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
			OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
		}

		public void ProjectSaveAs(string name, string? description, ObjectId? currentJobId, ObjectId currentColorBandSetId)
		{
			DoWithWriteLock(() =>
			{
				if (_projectAdapter.TryGetProject(name, out var existingProject))
				{
					_projectAdapter.DeleteProject(existingProject.Id);
				}

				var project = _projectAdapter.CreateProject(name, description, currentJobId, currentColorBandSetId);

				SaveColorBandSetsForProject(project.Id, updateAll: true);
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

				var curCbsId = _colorBandSetCollection.CurrentColorBandSet?.Id;
				if (curCbsId != null)
				{
					_projectAdapter.UpdateProjectCurrentCbsId(project.Id, curCbsId.Value);
				}

				SaveJobs(project.Id, updateAll: true);

				var curJobId = _jobsCollection.CurrentJob?.Id;
				if (curJobId != null)
				{
					_projectAdapter.UpdateProjectCurrentJobId(project.Id, curJobId.Value);
				}

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

					var curCbsIndex = _colorBandSetCollection.CurrentIndex;
					SaveColorBandSetsForProject(project.Id, updateAll: false);

					project.CurrentColorBandSetId = _colorBandSetCollection[curCbsIndex].Id;

					_projectAdapter.UpdateProjectCurrentCbsId(project.Id, project.CurrentColorBandSetId);

					SaveJobs(project.Id, updateAll: false);

					var curJobId = _jobsCollection.CurrentJob?.Id;
					if (curJobId != null)
					{
						_projectAdapter.UpdateProjectCurrentJobId(project.Id, curJobId.Value);
					}

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

				if (job.Id.CreationTime > lastSavedTime)
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
						_projectAdapter.UpdateJobDetalis(job);
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
				if (cbs.Id.CreationTime > lastSavedTime)
				{
					cbs.ProjectId = projectId;
					var updatedCbs = _projectAdapter.CreateColorBandSet(cbs);
					_colorBandSetCollection[i] = updatedCbs;
					UpdateCbsParentIds(cbs.Id, updatedCbs.Id);
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

		#endregion

		#region Public Methods -- Colors

		public bool ColorBandSetOpen(string id)
		{
			var colorBandSet = _projectAdapter.GetColorBandSet(id);

			if (colorBandSet != null)
			{
				CurrentColorBandSet = colorBandSet;
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Public Methods - Job

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			var curJob = CurrentJob;
			if (curJob == null)
			{
				return;
			}

			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
			var updatedMSetInfo = MSetInfo.UpdateWithNewCoords(curJob.MSetInfo, coords);
			LoadMap(updatedMSetInfo, transformType, newArea);
		}

		public void UpdateMapCoordinates(RRectangle coords)
		{
			var curJob = CurrentJob;
			if (curJob == null)
			{
				return;
			}

			var mSetInfo = curJob.MSetInfo;

			if (mSetInfo.Coords != coords)
			{
				var updatedMSetInfo = MSetInfo.UpdateWithNewCoords(mSetInfo, coords);
				LoadMap(updatedMSetInfo, TransformType.CoordinatesUpdate);
			}
		}

		public void UpdateTargetInterations(int targetIterations)
		{
			var curJob = CurrentJob;
			if (curJob == null)
			{
				return;
			}

			var mSetInfo = curJob.MSetInfo;

			if (mSetInfo.MapCalcSettings.TargetIterations != targetIterations)
			{
				var updatedMSetInfo = MSetInfo.UpdateWithNewIterations(mSetInfo, targetIterations);
				LoadMap(updatedMSetInfo, TransformType.IterationUpdate);
			}
		}

		public bool GoBack()
		{
			if (_jobsCollection.GoBack())
			{
				var curJob = CurrentJob;
				if (curJob != null)
				{
					DoWithWriteLock(() =>
					{
						UpdateTheJobsCanvasSize(curJob);
					});
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
			if (_jobsCollection.GoForward())
			{
				var curJob = CurrentJob;
				if (curJob != null)
				{
					DoWithWriteLock(() =>
					{
						UpdateTheJobsCanvasSize(curJob);
					});
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

			var parentJobId = CurrentJob?.Id;
			var jobName = MapJobHelper.GetJobName(transformType);
			var job = MapJobHelper.BuildJob(parentJobId, curProject.Id, jobName, CanvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);

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

		private void RerunWithNewDisplaySize()
		{
			_stateLock.EnterUpgradeableReadLock();
			try
			{
				var curJob = CurrentJob;
				if (curJob != null)
				{
					DoWithWriteLock(() =>
					{
						_ = UpdateTheJobsCanvasSize(curJob);
					});

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

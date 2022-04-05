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
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	internal class MapProjectViewModel : ViewModelBase, IMapProjectViewModel, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly ColorBandSetCollection _colorBandSetCollection;
		private readonly ObservableCollection<Job> _jobsCollection;
		private readonly ReaderWriterLockSlim _jobsLock;

		private int _jobsPointer;
		private SizeInt _canvasSize;

		private Project? _currentProject;
		private bool _currentProjectIsDirty;


		#region Constructor

		public MapProjectViewModel(ProjectAdapter projectAdapter, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_colorBandSetCollection = new ColorBandSetCollection(projectAdapter);
			BlockSize = blockSize;

			_jobsCollection = new ObservableCollection<Job>();
			_jobsPointer = -1;

			_canvasSize = new SizeInt();
			_currentProject = null;
			_currentProjectIsDirty = false;

			_jobsLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
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
					CurrentProjectIsDirty = false;

					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProject));
					OnPropertyChanged(nameof(IMapProjectViewModel.CanSaveProject));
				}
			}
		}

		public string? CurrentProjectName => CurrentProject?.Name;
		public bool CurrentProjectOnFile => CurrentProject?.OnFile ?? false;
		public bool CanSaveProject => CurrentProjectOnFile && CurrentProjectIsDirty;

		public bool CurrentColorBandSetOnFile => CurrentColorBandSet?.OnFile ?? false;
		public bool CanSaveColorBandSet => CurrentColorBandSetOnFile;

		public bool CurrentProjectIsDirty
		{
			get => _currentProjectIsDirty;
			private set
			{
				if (value != _currentProjectIsDirty)
				{
					_currentProjectIsDirty = value;
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectIsDirty));
				}
			}
		}

		public ColorBandSet? CurrentColorBandSet
		{
			get => _colorBandSetCollection.CurrentColorBandSet;
			set
			{
				if (value != _colorBandSetCollection.CurrentColorBandSet)
				{
					Debug.WriteLine($"MapProjectViewModel is having its ColorBandSet value updated. Old = {_colorBandSetCollection.CurrentColorBandSet?.Id}, New = {value?.Id ?? ObjectId.Empty}.");
					_colorBandSetCollection.Push(value);
					CurrentProjectIsDirty = true;
					OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
				}
			}
		}

		public Job? CurrentJob => DoWithReadLock(() => { return _jobsPointer == -1 ? null : _jobsCollection[_jobsPointer]; });
		public bool CanGoBack => !(CurrentJob?.ParentJob is null);
		public bool CanGoForward => DoWithReadLock(() => { return TryGetNextJobInStack(_jobsPointer, out var _); });

		#endregion

		#region Public Methods -- Project

		public void ProjectStartNew(MSetInfo mSetInfo, ColorBandSet colorBandSet)
		{
			CurrentProject = new Project("New", description: null, colorBandSet.Id);

			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();
				_jobsPointer = -1;
			});

			_colorBandSetCollection.Clear();
			_colorBandSetCollection.Push(colorBandSet);
			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			var newArea = new RectangleInt(new PointInt(), CanvasSize);
			LoadMap(mSetInfo, TransformType.None, newArea);
			CurrentProjectIsDirty = false;
		}

		public void ProjectCreate(string name, string description, ObjectId currentColorBandSetId)
		{
			if (_projectAdapter.TryGetProject(name, out var _))
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}

			var project = _projectAdapter.CreateProject(name, description, currentColorBandSetId);
			LoadProject(project);
		}

		public bool ProjectOpen(string projectName)
		{
			if (_projectAdapter.TryGetProject(projectName, out var project))
			{
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

			var colorBandSets = _projectAdapter.GetColorBandSetsForProject(CurrentProject.Id);
			_colorBandSetCollection.Load(colorBandSets, project.CurrentColorBandSetId);
			OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

			var jobs = _projectAdapter.GetAllJobs(CurrentProject.Id);

			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();

				foreach (var job in jobs)
				{
					_jobsCollection.Add(job);
				}

				_jobsPointer = -1;

				Rerun(_jobsCollection.Count - 1);
			});

			CurrentProjectIsDirty = false;
		}

		public void ProjectSaveAs(string name, string? description, ObjectId currentColorBandSetId)
		{
			DoWithWriteLock(() =>
			{
				if (_projectAdapter.TryGetProject(name, out var existingProject))
				{
					_projectAdapter.DeleteProject(existingProject.Id);
				}

				var project = _projectAdapter.CreateProject(name, description, currentColorBandSetId);
				_colorBandSetCollection.Save(project.Id);
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));

				var curCbsId = _colorBandSetCollection.CurrentColorBandSet?.Id;

				if (curCbsId != null)
				{
					_projectAdapter.UpdateProjectColorBandSetId(project.Id, curCbsId.Value);
				}

				for (var i = 0; i < _jobsCollection.Count; i++)
				{
					var job = _jobsCollection[i];
					job.Project = project;
					var updatedJob = _projectAdapter.InsertJob(job);
					_jobsCollection[i] = updatedJob;
					UpdateJob(job, updatedJob);
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

					var curCbsId = project.CurrentColorBandSetId;
					_colorBandSetCollection.Save(project.Id);

					if (curCbsId == ObjectId.Empty)
					{
						_projectAdapter.UpdateProjectColorBandSetId(project.Id, project.CurrentColorBandSetId);
					}

					var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(project.Id);

					for (var i = 0; i < _jobsCollection.Count; i++)
					{
						var job = _jobsCollection[i];
						if (job.Id.CreationTime > lastSavedTime)
						{
							var updatedJob = _projectAdapter.InsertJob(job);
							_jobsCollection[i] = updatedJob;
							UpdateJob(job, updatedJob);
						}
					}

					CurrentProjectIsDirty = false;
				}
			});
		}

		public void ProjectUpdateName(string name)
		{
			var project = CurrentProject;

			if (project != null)
			{
				if (project.OnFile)
				{
					_projectAdapter.UpdateProjectName(project.Id, name);
				}

				project.Name = name;
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentProjectName));
			}
		}

		public void ProjectUpdateDescription(string description)
		{
			var project = CurrentProject;

			if (project != null)
			{
				if (project.OnFile)
				{
					_projectAdapter.UpdateProjectDescription(project.Id, description);
				}

				project.Description = description;
			}
		}

		#endregion

		#region Public Methods -- Colors

		public bool ColorBandSetOpen(string id)
		{
			var colorBandSet = GetColorBandSet(id);

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

		public void ColorBandSetSave()
		{
			// TODO: Fix Me
			//var curProject = CurrentProject;

			//if (curProject != null)
			//{
			//	var colorBandSet = curProject.CurrentColorBandSet;
			//	if (colorBandSet != null)
			//	{
			//		if (colorBandSet.OnFile)
			//		{
			//			_projectAdapter.UpdateColorBandSet(colorBandSet);
			//		}
			//		else
			//		{
			//			var updatedColorBandSet = _projectAdapter.CreateColorBandSet(colorBandSet);
			//			curProject.CurrentColorBandSet = updatedColorBandSet;
			//		}
			//	}
			//}
		}
		
		public void ColorBandSetSaveAs(string name, string? description, int? versionNumber)
		{
			// TODO: Fix Me
			//var curProject = CurrentProject;

			//if (curProject != null && curProject.CurrentColorBandSet != null)
			//{
			//	var colorBandSet = curProject.CurrentColorBandSet.CreateNewCopy();
			//	var updatedcolorBandSet = _projectAdapter.CreateColorBandSet(colorBandSet);

			//	curProject.CurrentColorBandSet = updatedcolorBandSet;
			//}
		}

		public ColorBandSet? GetColorBandSet(string id)
		{
			var result = _projectAdapter.GetColorBandSet(id);
			return result;
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
			var updatedInfo = MSetInfo.UpdateWithNewCoords(curJob.MSetInfo, coords);

			LoadMap(updatedInfo, transformType, newArea);
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
				var updatedInfo = MSetInfo.UpdateWithNewIterations(mSetInfo, targetIterations);

				var newArea = new RectangleInt(new PointInt(), CanvasSize);
				LoadMap(updatedInfo, TransformType.IterationUpdate, newArea);
			}
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

		#endregion

		#region Private Methods

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea)
		{
			var curProject = CurrentProject;

			if (curProject == null)
			{
				return;
			}

			var parentJob = CurrentJob;
			var jobName = MapJobHelper.GetJobName(transformType);
			var job = MapJobHelper.BuildJob(parentJob, curProject, jobName, CanvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);

			Debug.WriteLine($"Starting Job with new coords: {mSetInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			DoWithWriteLock(() =>
			{
				_jobsCollection.Add(job);
				_jobsPointer = _jobsCollection.Count - 1;

				CurrentProjectIsDirty = true;

				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
			});
		}

		private void RerunWithNewDisplaySize()
		{
			_jobsLock.EnterUpgradeableReadLock();
			try
			{
				if (!(_jobsPointer < 0 || _jobsPointer > _jobsCollection.Count - 1))
				{
					DoWithWriteLock(() => Rerun(_jobsPointer));
					CurrentProjectIsDirty = true;
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

			var curJob = _jobsCollection[newJobIndex];

			if (curJob != null)
			{
				if (UpdateTheJobsCanvasSize(curJob, out var updatedJob))
				{
					_jobsCollection[newJobIndex] = updatedJob;
				}
			}

			if (_jobsPointer == newJobIndex)
			{
				// Force a redraw
				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
			}
			else
			{
				_jobsPointer = newJobIndex;

				OnPropertyChanged(nameof(IMapProjectViewModel.CurrentJob));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoBack));
				OnPropertyChanged(nameof(IMapProjectViewModel.CanGoForward));
			}
		}
		
		private bool UpdateTheJobsCanvasSize(Job job, out Job newJob)
		{
			var newCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, BlockSize);

			MapJobHelper.CheckCanvasSize(CanvasSize, BlockSize);

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

				newJob = job.Clone();

				newJob.MSetInfo = newMsetInfo;
				newJob.MapBlockOffset = mapBlockOffset;
				newJob.CanvasControlOffset = canvasControlOffset;
				newJob.CanvasSizeInBlocks = newCanvasSizeInBlocks;

				return true;
			}
			else
			{
				newJob = job;
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

		private void UpdateJob(Job oldJob, Job newJob)
		{
			foreach (var job in _jobsCollection)
			{
				if (job?.ParentJob?.Id == oldJob.Id)
				{
					job.ParentJob = newJob;
					_projectAdapter.UpdateJob(job, newJob);
				}
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

		private bool TryGetJobFromStack(int jobIndex, [MaybeNullWhen(false)] out Job job)
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

		private bool TryFindByJobId(ObjectId id, [MaybeNullWhen(false)] out Job job)
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

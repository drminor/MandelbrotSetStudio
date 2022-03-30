﻿using MongoDB.Bson;
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

					// TODO: Update the Job's CanvasSizeInBlocks (and NewArea?) property.
					Reload();
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
		public bool CurrentColorBandSetIsDirty => CurrentProject?.ColorBandSetIsDirty ?? false;
		public bool CanSaveColorBandSet => CurrentColorBandSetOnFile && CurrentColorBandSetIsDirty;

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
			get => CurrentProject?.CurrentColorBandSet;
			set
			{
				var project = CurrentProject;
				if (project != null)
				{
					if (value != project.CurrentColorBandSet)
					{
						Debug.WriteLine($"MapProjectViewModel is having its ColorBandSet value updated. Old = {project.CurrentColorBandSet.SerialNumber}, New = {value?.SerialNumber ?? Guid.Empty}.");
						project.CurrentColorBandSet = value;
						CurrentProjectIsDirty = true;
						OnPropertyChanged(nameof(IMapProjectViewModel.CurrentColorBandSet));
					}
					else
					{
						Debug.WriteLine($"MapProjectViewModel is ignoring the ColorBandSet value update. The Current value is already: {project.CurrentColorBandSet.SerialNumber}.");
					}
				}
				else
				{
					Debug.WriteLine($"MapProjectViewModel is having its ColorBandSet value updated. Its CurrentProject is null so no update is being made.");
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
			CurrentProject = new Project("New", description: null, colorBandSet);

			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();
				_jobsPointer = -1;
			});

			LoadMap(mSetInfo, TransformType.None);
			CurrentProjectIsDirty = false;
		}

		public void ProjectCreate(string name, string description, IEnumerable<Guid> colorBandSetIds, ColorBandSet currentColorBandSet)
		{
			if (_projectAdapter.TryGetProject(name, out var _))
			{
				throw new InvalidOperationException($"Cannot create project with name: {name}, a project with that name already exists.");
			}

			var project = _projectAdapter.CreateProject(name, description, colorBandSetIds, currentColorBandSet);
			LoadProject(project);
		}

		public bool ProjectOpen(string projectName)
		{
			var result = _projectAdapter.TryGetProject(projectName, out var project);
			if (result && project != null)
			{
				LoadProject(project);
			}

			return result;
		}

		// TODO: Check how the ColorBandSet is being handled upon ProjectSaveAs
		public void ProjectSaveAs(string name, string? description, IEnumerable<Guid> colorBandSetIds, ColorBandSet currentColorBandSet)
		{
			if( _projectAdapter.TryGetProject(name, out var existingProject))
			{
				if (existingProject != null)
				{
					_projectAdapter.DeleteProject(existingProject.Id);
				}
			}

			var project = _projectAdapter.CreateProject(name, description, colorBandSetIds, currentColorBandSet);

			var jobsList = Jobs.ToList();

			for (var i = 0; i < jobsList.Count; i++)
			{
				var job = jobsList[i];
				job.Project = project;
				var updatedJob = _projectAdapter.InsertJob(job);
				UpdateJob(job, updatedJob);
			}

			CurrentProject = project;

			CurrentProjectIsDirty = false;
		}

		private void LoadProject(Project project)
		{
			CurrentProject = project;
			var jobs = _projectAdapter.GetAllJobs(CurrentProject.Id);

			DoWithWriteLock(() =>
			{
				_jobsCollection.Clear();

				foreach (var job in jobs)
				{
					_jobsCollection.Add(job);
				}

				_jobsPointer = - 1;

				Rerun(_jobsCollection.Count - 1);
			});

			CurrentProjectIsDirty = false;
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

				var colorBandSet = project.CurrentColorBandSet;

				if (colorBandSet != null && project.ColorBandSetIsDirty)
				{
					if (colorBandSet.OnFile)
					{
						_projectAdapter.UpdateProjectColorBands(project.Id, project.ColorBandSetSNs, colorBandSet);
						project.ColorBandSetIsDirty = false;
					}
					else
					{
						colorBandSet.Name = project.Name;
						var updatedColorBandSet = _projectAdapter.CreateColorBandSet(colorBandSet);
						project.CurrentColorBandSet = updatedColorBandSet;

						_projectAdapter.UpdateProjectColorBands(project.Id, project.ColorBandSetSNs, project.CurrentColorBandSet);
						project.ColorBandSetIsDirty = false;
					}
				}

				var lastSavedTime = _projectAdapter.GetProjectLastSaveTime(project.Id);

				var jobsList = Jobs.ToList();

				for (var i = 0; i < jobsList.Count; i++)
				{
					var job = jobsList[i];
					if (job.Id.CreationTime > lastSavedTime)
					{
						var updatedJob = _projectAdapter.InsertJob(job);
						UpdateJob(job, updatedJob);
					}
				}

				CurrentProjectIsDirty = false;
			}
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

		public bool ColorBandSetOpen(Guid serialNumber)
		{
			var colorBandSet = GetColorBandSet(serialNumber);

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
			var curProject = CurrentProject;

			if (curProject != null)
			{
				var colorBandSet = curProject.CurrentColorBandSet;
				if (colorBandSet != null)
				{
					if (colorBandSet.OnFile)
					{
						_projectAdapter.UpdateColorBandSet(colorBandSet);
						curProject.ColorBandSetIsDirty = false;
					}
					else
					{
						var updatedColorBandSet = _projectAdapter.CreateColorBandSet(colorBandSet);
						curProject.CurrentColorBandSet = updatedColorBandSet;
					}
				}
			}
		}
		
		public void ColorBandSetSaveAs(string name, string? description, int? versionNumber)
		{
			var curProject = CurrentProject;

			if (curProject != null && curProject.CurrentColorBandSet != null)
			{
				var colorBandSet = curProject.CurrentColorBandSet.CreateNewCopy();

				colorBandSet.Name = name;
				colorBandSet.Description = description;
				colorBandSet.VersionNumber = versionNumber ?? 0;

				var updatedcolorBandSet = _projectAdapter.CreateColorBandSet(colorBandSet);

				curProject.CurrentColorBandSet = updatedcolorBandSet;
			}
		}

		public ColorBandSet? GetColorBandSet(Guid serialNumber)
		{
			var result = _projectAdapter.GetColorBandSet(serialNumber);
			return result;
		}

		#endregion

		#region Public Methods - Job

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			var curJob = CurrentJob;
			var position = curJob.MSetInfo.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
			var updatedInfo = MSetInfo.UpdateWithNewCoords(curJob.MSetInfo, coords);

			LoadMap(updatedInfo, transformType, newArea);
		}

		public void UpdateTargetInterations(int targetIterations, int iterationsPerRequest)
		{
			var curJob = CurrentJob;
			var mSetInfo = curJob.MSetInfo;
			var updatedInfo = MSetInfo.UpdateWithNewIterations(mSetInfo, targetIterations, iterationsPerRequest);

			LoadMap(updatedInfo, TransformType.IterationUpdate);
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

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType)
		{
			var newArea = new RectangleInt(new PointInt(), CanvasSize);
			LoadMap(mSetInfo, transformType, newArea);
		}

		private void LoadMap(MSetInfo mSetInfo, TransformType transformType, RectangleInt newArea)
		{
			var parentJob = CurrentJob;
			var jobName = MapJobHelper.GetJobName(transformType);
			var job = MapJobHelper.BuildJob(parentJob, CurrentProject, jobName, CanvasSize, mSetInfo, transformType, newArea, BlockSize, _projectAdapter);

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

		private void Reload()
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

		private void UpdateJob(Job oldJob, Job newJob)
		{
			DoWithWriteLock(() =>
			{
				if (TryFindByJobId(oldJob.Id, out var foundJob))
				{
					var idx = _jobsCollection.IndexOf(foundJob);
					_jobsCollection[idx] = newJob;

					foreach (var job in _jobsCollection)
					{
						if (job?.ParentJob?.Id == oldJob.Id)
						{
							job.ParentJob = newJob;
							_projectAdapter.UpdateJob(job, newJob);
						}
					}
				}
				else
				{
					throw new KeyNotFoundException("The old job could not be found.");
				}
			});
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

		private bool TryFindByJobId(ObjectId id, out Job? job)
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

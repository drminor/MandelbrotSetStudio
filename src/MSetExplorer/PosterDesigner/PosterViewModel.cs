using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly SizeInt _blockSize;

		private SizeInt _canvasSize;
		private Poster? _currentPoster;

		#region Constructor

		public PosterViewModel(ProjectAdapter projectAdapter, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_blockSize = blockSize;

			_canvasSize = new SizeInt();
			_currentPoster = null;
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
					OnPropertyChanged(nameof(IPosterViewModel.CanvasSize));

					//if (CurrentPoster != null)
					//{
					//	RerunWithNewDisplaySize(CurrentPoster);
					//}
				}
			}
		}

		public Poster? CurrentPoster
		{
			get => _currentPoster;
			private set
			{
				if(value != _currentPoster)
				{
					if (_currentPoster != null)
					{
						_currentPoster.PropertyChanged -= CurrentPoster_PropertyChanged;
					}
					_currentPoster = value;
					if (_currentPoster != null)
					{
						_currentPoster.PropertyChanged += CurrentPoster_PropertyChanged;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentPoster));
					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));

				}
			}
		}

		private void CurrentPoster_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Project.IsDirty))
			{
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
			}

			if (e.PropertyName == nameof(Project.OnFile))
			{
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));
			}

			if (e.PropertyName == nameof(Project.CurrentColorBandSet))
			{
				Debug.WriteLine("The MapProjectViewModel is raising PropertyChanged: IPosterViewModel.CurrentColorBandSet as the Project's ColorBandSet is being updated.");
				OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
			}
		}

		public bool CurrentPosterIsDirty => CurrentPoster?.IsDirty ?? false;


		public string? CurrentPosterName => CurrentPoster?.Name;
		public bool CurrentPosterOnFile => CurrentPoster?.OnFile ?? false;
		//public bool CanSaveProject => CurrentPosterOnFile;

		//public Job CurrentJob => CurrentPoster?.CurrentJob ?? Job.Empty;

		public ColorBandSet? CurrentColorBandSet => CurrentPoster?.ColorBandSet;

		#endregion

		#region Public Methods -- Project

		public bool PosterOpen(string projectName)
		{
			if (_projectAdapter.TryGetPoster(projectName, out var poster))
			{
				CurrentPoster = poster;
				return true;
			}
			else
			{
				return false;
			}
		}

		public void PosterSave()
		{
			var poster = CurrentPoster;

			if (poster != null)
			{
				if (!CurrentPosterOnFile)
				{
					throw new InvalidOperationException("Cannot save an unloaded project, use SaveProject instead.");
				}

				poster.Save(_projectAdapter);

				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));

				//OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
			}
		}

		public bool PosterSaveAs(string name, string? description)
		{
			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				return false;
			}

			if (_projectAdapter.TryGetPoster(name, out var existingPoster))
			{
				_projectAdapter.DeletePoster(existingPoster.Id);
			}

			// TOOD: Have the poster class implement ICloneable
			var poster = new Poster(name, description, currentPoster.SourceJobId, currentPoster.SubdivisionId, currentPoster.JobAreaInfo, currentPoster.ColorBandSet, currentPoster.MapCalcSettings);

			if (poster is null)
			{
				return false;
			}
			else
			{
				poster.Save(_projectAdapter);
				CurrentPoster = poster;

				return true;
			}
		}

		public void PosterClose()
		{
			CurrentPoster = null;
		}

		#endregion

		#region Public Methods - Poster 

		public void PosterCreate(Poster poster)
		{
			_projectAdapter.CreatePoster(poster);
		}

		#endregion

		#region Public Methods - Job

		// Currently Not Used.
		public void UpdateMapCoordinates(RRectangle coords)
		{
			if (CurrentPoster == null)
			{
				return;
			}

			if (CurrentPoster.JobAreaInfo.Coords != coords)
			{
				LoadMap(CurrentPoster, CurrentPoster.ColorBandSet.Id, coords, CurrentPoster.MapCalcSettings);
			}
		}

		public void UpdateColorBandSet(ColorBandSet colorBandSet)
		{
			if (CurrentPoster == null)
			{
				return;
			}

			if (CurrentColorBandSet == colorBandSet)
			{
				Debug.WriteLine($"MapProjectViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
				return;
			}

			var targetIterations = colorBandSet.HighCutoff;

			if (targetIterations != CurrentPoster.MapCalcSettings.TargetIterations)
			{
				//CurrentPoster.Add(colorBandSet);

				Debug.WriteLine($"MapProjectViewModel is updating the Target Iterations. Current ColorBandSetId = {CurrentPoster.ColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				var mapCalcSettings = new MapCalcSettings(targetIterations, CurrentPoster.MapCalcSettings.RequestsPerJob);
				LoadMap(CurrentPoster, colorBandSet.Id, CurrentPoster.JobAreaInfo.Coords, mapCalcSettings);
			}
			else
			{
				Debug.WriteLine($"MapProjectViewModel is updating the ColorBandSet. Current ColorBandSetId = {CurrentPoster.ColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				CurrentPoster.ColorBandSet = colorBandSet;

				OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
			}
		}

		#endregion

		#region Private Methods

		//private void RerunWithNewDisplaySize(Project project)
		//{
		//	var wasUpdated = false;

		//	var currentCanvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(CanvasSize, _blockSize);
		//	if (CurrentJob.CanvasSizeInBlocks != currentCanvasSizeInBlocks)
		//	{
		//		FindOrCreateJobForNewCanvasSize(project, CurrentJob, currentCanvasSizeInBlocks);
		//		wasUpdated = true;
		//	}

		//	if (wasUpdated)
		//	{
		//		OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
		//	}
		//}

		private void LoadMap(Poster poster, ObjectId colorBandSetId, RRectangle coords, MapCalcSettings mapCalcSettings)
		{

			var job = MapJobHelper.BuildJob(parentJobId: null, poster.SourceJobId ?? ObjectId.Empty, CanvasSize, coords, colorBandSetId, mapCalcSettings, TransformType.None, newArea: null, _blockSize, _projectAdapter);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			//OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
		}

		//private void FindOrCreateJobForNewCanvasSize(Project project, Job job, SizeInt newCanvasSizeInBlocks)
		//{
		//	// Note if this job is itself a CanvasSizeUpdate Proxy Job, then its parent is used to conduct the search.
		//	if (project.TryGetCanvasSizeUpdateProxy(job, newCanvasSizeInBlocks, out var matchingProxy))
		//	{
		//		project.CurrentJob = matchingProxy;
		//		return;
		//	}

		//	// Make sure we use the original job and not a 'CanvasSizeUpdate Proxy Job'.
		//	job = project.GetPreferredSibling(job);

		//	var newCoords = RMapHelper.GetNewCoordsForNewCanvasSize(job.Coords, job.CanvasSizeInBlocks, newCanvasSizeInBlocks, job.Subdivision.SamplePointDelta, _blockSize);
		//	//var newMSetInfo = MSetInfo.UpdateWithNewCoords(job.MSetInfo, newCoords);

		//	var transformType = TransformType.CanvasSizeUpdate;
		//	RectangleInt? newArea = null;

		//	var newJob = MapJobHelper.BuildJob(job.ParentJobId, project.Id, CanvasSize, newCoords, job.ColorBandSetId, job.MapCalcSettings, transformType, newArea, _blockSize, _projectAdapter);

		//	Debug.WriteLine($"Re-runing job. Current CanvasSize: {job.CanvasSizeInBlocks}, new CanvasSize: {newCanvasSizeInBlocks}.");
		//	Debug.WriteLine($"Starting Job with new coords: {newCoords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

		//	project.Add(newJob);
		//}

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

					//if (CurrentPoster != null)
					//{
					//	CurrentPoster.Dispose();
					//	CurrentPoster = null;
					//}
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

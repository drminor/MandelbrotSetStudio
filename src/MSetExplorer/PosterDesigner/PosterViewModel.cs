using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly SizeInt _blockSize;

		private SizeInt _canvasSize;
		private double _displayZoom;
		private Poster? _currentPoster;

		JobAreaAndCalcSettings _jobAreaAndCalcSettings;

		#region Constructor

		public PosterViewModel(ProjectAdapter projectAdapter, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_blockSize = blockSize;

			_canvasSize = new SizeInt();
			_displayZoom = 1.0;
			_currentPoster = null;
			_jobAreaAndCalcSettings = JobAreaAndCalcSettings.Empty;
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
					MinimumDisplayZoom = GetMinimumDisplayZoom(CurrentPoster?.JobAreaInfo.CanvasSize, CanvasSize);

					OnPropertyChanged(nameof(IPosterViewModel.CanvasSize));

					//if (CurrentPoster != null)
					//{
					//	RerunWithNewDisplaySize(CurrentPoster);
					//}
				}
			}
		}

		/// <summary>
		/// Value between 0.0 and 1.0
		/// 1.0 presents 1 map "pixel" to 1 screen pixel
		/// 0.5 presents 2 map "pixels" to 1 screen pixel
		/// </summary>
		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value - _displayZoom) > 0.001)
				{
					_displayZoom = Math.Max(MinimumDisplayZoom,  value);
					Debug.WriteLine($"The DispZoom is {DisplayZoom}.");
					OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
				}
			}
		}

		private double _minimumDisplayZoom;

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			set
			{
				if (Math.Abs(value - _minimumDisplayZoom) > 0.001)
				{
					_minimumDisplayZoom = value;
					Debug.WriteLine($"The MinDispZoom is {MinimumDisplayZoom}.");
					OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
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
					MinimumDisplayZoom = GetMinimumDisplayZoom(_currentPoster?.JobAreaInfo.CanvasSize, CanvasSize);

					if (_currentPoster != null)
					{
						var viewPortArea = GetNewViewPort(_currentPoster.JobAreaInfo, _currentPoster.DisplayPosition, CanvasSize, DisplayZoom);
						JobAreaAndCalcSettings = new JobAreaAndCalcSettings(viewPortArea, _currentPoster.MapCalcSettings);
						_currentPoster.PropertyChanged += CurrentPoster_PropertyChanged;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentPoster));
					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				}
			}
		}

		public JobAreaAndCalcSettings JobAreaAndCalcSettings
		{
			get => _jobAreaAndCalcSettings;

			set
			{
				if (value != _jobAreaAndCalcSettings)
				{
					_jobAreaAndCalcSettings = value;

					MinimumDisplayZoom = GetMinimumDisplayZoom(CurrentPoster?.JobAreaInfo.CanvasSize, CanvasSize);

					OnPropertyChanged(nameof(IPosterViewModel.JobAreaAndCalcSettings));
				}
			}
		}

		private void CurrentPoster_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Poster.IsDirty))
			{
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
			}

			else if (e.PropertyName == nameof(Poster.OnFile))
			{
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));
			}

			else if (e.PropertyName == nameof(Poster.ColorBandSet))
			{
				Debug.WriteLine("The PosterViewModel is raising PropertyChanged: IPosterViewModel.CurrentColorBandSet as the Poster's ColorBandSet is being updated.");
				OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
			}

			else if (e.PropertyName == nameof(Poster.DisplayPosition))
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					var viewPortArea = GetNewViewPort(curPoster.JobAreaInfo, curPoster.DisplayPosition, CanvasSize, DisplayZoom);
					JobAreaAndCalcSettings = new JobAreaAndCalcSettings(viewPortArea, curPoster.MapCalcSettings);
				}
				else
				{
					JobAreaAndCalcSettings = JobAreaAndCalcSettings.Empty;
				}
			}

			else if (e.PropertyName == nameof(Poster.DisplayZoom))
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					DisplayZoom = curPoster.DisplayZoom;

					var viewPortArea = GetNewViewPort(curPoster.JobAreaInfo, curPoster.DisplayPosition, CanvasSize, DisplayZoom);
					JobAreaAndCalcSettings = new JobAreaAndCalcSettings(viewPortArea, curPoster.MapCalcSettings);
				}
			}

			else if (e.PropertyName == nameof(Poster.JobAreaInfo))
			{
				// TODO: Handle Poster Canvas Size changes.
			}
		}

		public bool CurrentPosterIsDirty => CurrentPoster?.IsDirty ?? false;

		public string? CurrentPosterName => CurrentPoster?.Name;
		public bool CurrentPosterOnFile => CurrentPoster?.OnFile ?? false;

		public ColorBandSet? CurrentColorBandSet => CurrentPoster?.ColorBandSet;

		#endregion

		#region Public Methods -- Poster

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

			var poster = new Poster(name, description, currentPoster.SourceJobId, currentPoster.JobAreaInfo, currentPoster.ColorBandSet, currentPoster.MapCalcSettings);

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

		#region Public Methods - Job

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			// TOOD: Implement UpdateMapView
			//Debug.Assert(transformType == TransformType.ZoomIn || transformType == TransformType.Pan, "UpdateMapView received a TransformType other than ZoomIn or Pan.");
			//if (CurrentProject == null)
			//{
			//	return;
			//}

			//var curJob = CurrentJob;

			//var position = curJob.Coords.Position;
			//var samplePointDelta = curJob.Subdivision.SamplePointDelta;

			//var coords = RMapHelper.GetMapCoords(newArea, position, samplePointDelta);
			//LoadMap(CurrentProject, curJob, curJob.ColorBandSetId, coords, curJob.MapCalcSettings, transformType, newArea);
		}

		// Currently Not Used.
		public void UpdateMapCoordinates(RRectangle coords)
		{
			//if (CurrentPoster == null)
			//{
			//	return;
			//}

			//if (CurrentPoster.JobAreaInfo.Coords != coords)
			//{
			//	LoadMap(CurrentPoster, coords, CurrentPoster.ColorBandSet.Id, CurrentPoster.MapCalcSettings);
			//}
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
				CurrentPoster.ColorBandSet = colorBandSet;

				Debug.WriteLine($"MapProjectViewModel is updating the Target Iterations. Current ColorBandSetId = {CurrentPoster.ColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				var mapCalcSettings = new MapCalcSettings(targetIterations, CurrentPoster.MapCalcSettings.RequestsPerJob);

				//LoadMap(CurrentPoster, CurrentPoster.JobAreaInfo.Coords, colorBandSet.Id, mapCalcSettings);
				JobAreaAndCalcSettings = new JobAreaAndCalcSettings(JobAreaAndCalcSettings.JobAreaInfo, mapCalcSettings);
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

		private JobAreaInfo GetNewViewPort(JobAreaInfo currentAreaInfo, VectorInt displayPosition, SizeInt displaySize, double displayZoom)
		{
			var logicalDispSize = displaySize.Scale(1 / displayZoom);

			var screenArea = new RectangleInt(new PointInt(displayPosition), logicalDispSize);
			var mapPosition = currentAreaInfo.Coords.Position;
			var subdivision = currentAreaInfo.Subdivision;

			var newCoords = RMapHelper.GetMapCoords(screenArea, mapPosition, subdivision.SamplePointDelta);
			var newMapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, subdivision, out var newCanvasControlOffset);

			var result = new JobAreaInfo(newCoords, logicalDispSize, subdivision, newMapBlockOffset, newCanvasControlOffset);

			return result;
		}

		//private void LoadMap(Poster poster, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings)
		//{
		//	var projectId = poster.SourceJobId ?? ObjectId.Empty;


		//	var job = MapJobHelper.BuildJob(parentJobId: null, projectId, CanvasSize, coords, colorBandSetId, mapCalcSettings, TransformType.None, newArea: null, _blockSize, _projectAdapter);

		//	Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

		//	//OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
		//}

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

		private double GetMinimumDisplayZoom(SizeInt? posterSize, SizeInt displaySize)
		{
			double result;

			if (posterSize != null)
			{
				result = displaySize.Width / (double)posterSize.Value.Width;
			}
			else
			{
				result = 0.9;
			}

			return result;
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

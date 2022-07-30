using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly SizeInt _blockSize;

		private SizeDbl _canvasSize;
		private SizeDbl _logicalDisplaySize;

		private Poster? _currentPoster;

		AreaColorAndCalcSettings _areaColorAndCalcSettings;
		private ColorBandSet? _previewColorBandSet;

		#region Constructor

		public PosterViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;
			_blockSize = blockSize;

			_canvasSize = new SizeDbl();
			_currentPoster = null;
			_areaColorAndCalcSettings = AreaColorAndCalcSettings.Empty;
			_previewColorBandSet = null;
		}

		#endregion

		#region Public Properties - Derived

		public new bool InDesignMode => base.InDesignMode;

		#endregion

		#region Public Properties

		public SizeDbl CanvasSize
		{
			get => _canvasSize;
			set
			{
				if(value != _canvasSize)
				{
					_canvasSize = value;
					OnPropertyChanged(nameof(IPosterViewModel.CanvasSize));

				}
			}
		}

		public SizeDbl LogicalDisplaySize
		{
			get => _logicalDisplaySize;
			set
			{
				if (value != _logicalDisplaySize)
				{
					_logicalDisplaySize = value;
					if (CurrentPoster != null)
					{
						UpdateMapView(CurrentPoster);
					}

					OnPropertyChanged(nameof(IPosterViewModel.LogicalDisplaySize));
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
						var dispPos = _currentPoster.DisplayPosition;
						OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
						OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));

						// Setting the PosterSize and DisplayZoom can update the DisplayPosition. Use the value read from file.
						_currentPoster.DisplayPosition = dispPos;
						OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));

						var currentJob = _currentPoster.CurrentJob;
						var currentColorBandSet = _currentPoster.CurrentColorBandSet;
						CurrentAreaColorAndCalcSettings = CreateDisplayJob(currentJob, currentColorBandSet, DisplayPosition, LogicalDisplaySize.Round());
						_currentPoster.PropertyChanged += CurrentPoster_PropertyChanged;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentPoster));
					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				}
			}
		}

		public bool CurrentPosterIsDirty => CurrentPoster?.IsDirty ?? false;

		public string? CurrentPosterName => CurrentPoster?.Name;
		public bool CurrentPosterOnFile => CurrentPoster?.OnFile ?? false;

		public Job CurrentJob
		{
			get => CurrentPoster?.CurrentJob ?? Job.Empty;
			private set { }
		}

		public MapAreaInfo PosterAreaInfo => CurrentPoster?.CurrentJob.MapAreaInfo ?? MapAreaInfo.Empty;

		public SizeInt PosterSize => PosterAreaInfo.CanvasSize;

		public ColorBandSet CurrentColorBandSet
		{
			get => PreviewColorBandSet ?? CurrentPoster?.CurrentColorBandSet ?? new ColorBandSet();
			set
			{
				var currentPoster = CurrentPoster;
				if (currentPoster != null)
				{
					if (UpdateColorBandSet(value))
					{
						OnPropertyChanged(nameof(IProjectViewModel.CurrentColorBandSet));
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
					if (value == null || CurrentPoster == null)
					{
						_previewColorBandSet = value;
					}
					else
					{
						var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(value, CurrentPoster.CurrentJob.MapCalcSettings.TargetIterations);
						_previewColorBandSet = adjustedColorBandSet;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				}
			}
		}

		public VectorInt DisplayPosition
		{
			get => CurrentPoster?.DisplayPosition ?? new VectorInt();
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (value != DisplayPosition)
					{
						curPoster.DisplayPosition = value;

						var currentJob = curPoster.CurrentJob;
						var currentColorBandSet = curPoster.CurrentColorBandSet;
						CurrentAreaColorAndCalcSettings = CreateDisplayJob(currentJob, currentColorBandSet, value, LogicalDisplaySize.Round());

						OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));
					}
				}
				else
				{
					CurrentAreaColorAndCalcSettings = AreaColorAndCalcSettings.Empty;
				}
			}
		}

		public double DisplayZoom
		{
			get => CurrentPoster?.DisplayZoom ?? 1;
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (Math.Abs(value - DisplayZoom) > 0.001)
					{
						curPoster.DisplayZoom = value;
						Debug.WriteLine($"The PosterViewModel's DispZoom is being updated to {value}.");
						OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
					}
				}
			}
		}

		/// <summary>
		/// Job Area for what is currently being displayed.
		/// </summary>
		public AreaColorAndCalcSettings CurrentAreaColorAndCalcSettings
		{
			get => _areaColorAndCalcSettings;

			set
			{
				if (value != _areaColorAndCalcSettings)
				{
					_areaColorAndCalcSettings = value;
					OnPropertyChanged(nameof(IPosterViewModel.CurrentAreaColorAndCalcSettings));
				}
			}
		}

		#endregion

		#region Event Handlers

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

			else if (e.PropertyName == nameof(IPosterViewModel.CurrentColorBandSet))
			{
				Debug.WriteLine("The PosterViewModel is raising PropertyChanged: IPosterViewModel.CurrentColorBandSet as the Project's ColorBandSet is being updated.");
				OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
			}

			else if (e.PropertyName == nameof(Project.CurrentJob))
			{
				if (CurrentPoster != null)
				{
					OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
				}
			}
			else if (e.PropertyName == nameof(DisplayZoom))
			{
				OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
			}
		}

		#endregion

		#region Public Methods - Poster

		public bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster)
		{
			return _projectAdapter.TryGetPoster(name, out poster);
		}

		public bool PosterOpen(string name)
		{
			if (_projectAdapter.TryGetPoster(name, out var poster))
			{
				CurrentPoster = poster;
				return true;
			}
			else
			{
				return false;
			}
		}

		public void Load(Poster poster, MapAreaInfo? newMmapAreaInfo)
		{
			if (newMmapAreaInfo != null)
			{
				UpdateMapSpecs(poster, newMmapAreaInfo);
			}
			else
			{
				CurrentPoster = poster;
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

				JobOwnerHelper.Save(poster, _projectAdapter);

				//poster.Save(_projectAdapter);

				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));
			}
		}

		public void PosterSaveAs(string name, string? description)
		{
			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				throw new InvalidOperationException("The poster must be non-null.");
			}

			_ = ProjectAndMapSectionHelper.DeletePoster(name, _projectAdapter, _mapSectionAdapter);

			Debug.Assert(!CurrentJob.IsEmpty, "PosterSaveAs found the CurrentJob to be empty.");

			var poster = JobOwnerHelper.CreateCopyOfPoster(currentPoster, name, description, _projectAdapter, _mapSectionAdapter);

			_ = JobOwnerHelper.Save(poster, _projectAdapter);

			CurrentPoster = poster;
		}

		public void Close()
		{
			CurrentPoster = null;
		}

		#endregion

		#region Public Methods - MapView

		public bool UpdateColorBandSet(ColorBandSet colorBandSet)
		{
			var poster = CurrentPoster;

			if (poster == null)
			{
				return false;
			}

			// Discard the Preview ColorBandSet. 
			_previewColorBandSet = null;

			var currentJob = poster.CurrentJob;

			if (CurrentColorBandSet.Id != currentJob.ColorBandSetId)
			{
				Debug.WriteLine($"The project's CurrentColorBandSet and CurrentJob's ColorBandSet are out of sync. The CurrentColorBandSet has {CurrentColorBandSet.Count} bands. The CurrentJob IsEmpty = {CurrentJob.IsEmpty}.");
			}

			if (colorBandSet == CurrentColorBandSet)
			{
				Debug.WriteLine($"PosterViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
				return false;
			}

			var targetIterations = colorBandSet.HighCutoff;

			if (targetIterations != currentJob.MapCalcSettings.TargetIterations)
			{
				poster.Add(colorBandSet);

				Debug.WriteLine($"PosterViewModel is updating the Target Iterations. Current ColorBandSetId = {poster.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				var mapCalcSettings = new MapCalcSettings(targetIterations, currentJob.MapCalcSettings.RequestsPerJob);

				//CurrentAreaColorAndCalcSettings = new AreaColorAndCalcSettings(CurrentAreaColorAndCalcSettings, mapCalcSettings);
				LoadMap(poster, currentJob, currentJob.Coords, colorBandSet.Id, mapCalcSettings, TransformType.IterationUpdate, null);
			}
			else
			{
				Debug.WriteLine($"PosterViewModel is updating the ColorBandSet. Current ColorBandSetId = {poster.CurrentColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");

				//poster.Add(CurrentColorBandSet);
				//OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				poster.CurrentColorBandSet = colorBandSet;
			}

			return true;
		}

		#endregion

		#region Public Methods -- Job

		// PosterDesigner Edit Size
		public void UpdateMapSpecs(Poster currentPoster, MapAreaInfo newMapAreaInfo)
		{
			var curJob = currentPoster.CurrentJob;
			var colorBandSetId = curJob.ColorBandSetId;
			var mapCalcSettings = curJob.MapCalcSettings;

			LoadMap(currentPoster, curJob, newMapAreaInfo, colorBandSetId, mapCalcSettings, TransformType.ZoomIn);
			OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
			currentPoster.DisplayPosition = new VectorInt();
			currentPoster.DisplayZoom = 1;

			_logicalDisplaySize = new SizeDbl(10, 10);
			LogicalDisplaySize = CanvasSize;

			UpdateMapView(currentPoster);
		}

		// PosterDesigner Pan and Zoom Out
		public void UpdateMapSpecs(TransformType transformType, RectangleInt screenArea)
		{
			Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");

			var currentPoster = CurrentPoster;
			if (currentPoster == null)
			{
				return;
			}

			var curJob = currentPoster.CurrentJob;

			var mapPosition = curJob.Coords.Position;
			var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);

			var colorBandSetId = curJob.ColorBandSetId;
			var mapCalcSettings = curJob.MapCalcSettings;
			LoadMap(currentPoster, curJob, coords, colorBandSetId, mapCalcSettings, transformType, screenArea);

			OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
			currentPoster.DisplayPosition = new VectorInt();
			currentPoster.DisplayZoom = 1;

			_logicalDisplaySize = new SizeDbl(10, 10);
			LogicalDisplaySize = CanvasSize;

			UpdateMapView(currentPoster);
		}

		public MapAreaInfo GetUpdatedMapAreaInfo(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize)
		{
			var mapPosition = mapAreaInfo.Coords.Position;
			var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
			var screenAreaInt = screenArea.Round();

			var coords = RMapHelper.GetMapCoords(screenAreaInt, mapPosition, samplePointDelta);

			CheckCoordsChange(mapAreaInfo, screenArea, coords);

			var posterSize = newMapSize.Round();
			var blockSize = mapAreaInfo.Subdivision.BlockSize;

			var result = _mapJobHelper.GetMapAreaInfo(coords, posterSize, blockSize);

			return result;
		}

		//// Used to service Zoom and Pan jobs raised by the MapDisplay Control
		//public MapAreaInfo? GetUpdatedMapAreaInfo(TransformType transformType, RectangleInt screenArea)
		//{
		//	var curJob = CurrentJob;

		//	if (curJob.IsEmpty)
		//	{
		//		return null;
		//	}

		//	if (screenArea == new RectangleInt())
		//	{
		//		Debug.WriteLine("GetUpdatedJobInfo was given an empty newArea rectangle.");
		//		//return MapJobHelper.GetMapAreaInfo(curJob, CanvasSize);
		//		return curJob.MapAreaInfo;
		//	}
		//	else
		//	{
		//		var mapPosition = curJob.Coords.Position;
		//		var samplePointDelta = curJob.Subdivision.SamplePointDelta;
		//		var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);
		//		var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(coords, PosterSize, _blockSize);

		//		return mapAreaInfo;
		//	}
		//}

		#endregion

		#region Private Methods

		// PosterViewModel update from position and zoom changes
		private void UpdateMapView(Poster poster)
		{
			// Use the new map specification and the current zoom and display position to set the region to display.
			var currentJob = poster.CurrentJob;
			var currentColorBandSet = poster.CurrentColorBandSet;

			CurrentAreaColorAndCalcSettings = CreateDisplayJob(currentJob, currentColorBandSet, DisplayPosition, LogicalDisplaySize.Round());
		}

		// Get Display Job
		private AreaColorAndCalcSettings CreateDisplayJob(Job currentJob, ColorBandSet currentColorBandSet, VectorInt displayPosition, SizeInt logicalDisplaySize)
		{
			var viewPortArea = GetNewViewPort(currentJob.MapAreaInfo, displayPosition, logicalDisplaySize);

			var mapCalcSettingsCpy = currentJob.MapCalcSettings.Clone();
			//mapCalcSettingsCpy.DontFetchZValuesFromRepo = true;

			var areaColorAndCalcSettings = new AreaColorAndCalcSettings(currentJob.Id.ToString(), JobOwnerType.Poster, viewPortArea, currentColorBandSet, mapCalcSettingsCpy);

			return areaColorAndCalcSettings;
		}

		private MapAreaInfo GetNewViewPort(MapAreaInfo currentAreaInfo, VectorInt displayPosition, SizeInt logicalDisplaySize)
		{
			var diagScreenArea = new RectangleInt(new PointInt(), currentAreaInfo.CanvasSize);
			var screenArea = new RectangleInt(new PointInt(displayPosition), logicalDisplaySize);
			var diagSqAmt = diagScreenArea.Width * diagScreenArea.Height;
			var screenSqAmt = screenArea.Width * screenArea.Height;
			var sizeRat = screenSqAmt / (double) diagSqAmt;

			Debug.WriteLine($"Creating ViewPort at pos: {displayPosition} and size: {logicalDisplaySize}.");
			Debug.WriteLine($"The new ViewPort covers {sizeRat}. Total Screen Area: {diagScreenArea}, viewPortArea: {screenArea}.");

			var totalLeft = diagScreenArea.Position.X + diagScreenArea.Width;
			var screenLeft = screenArea.Position.X + screenArea.Width;
			var totalTop = diagScreenArea.Position.Y + diagScreenArea.Height;
			var screenTop = screenArea.Position.Y + screenArea.Height;
			var trAmt = new VectorInt(Math.Max(screenLeft - totalLeft, 0), Math.Max(screenTop - totalTop, 0));
			screenArea = screenArea.Translate(trAmt);

			var mapPosition = currentAreaInfo.Coords.Position;
			var subdivision = currentAreaInfo.Subdivision;

			var newCoords = RMapHelper.GetMapCoords(screenArea, mapPosition, subdivision.SamplePointDelta);
			var newMapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, subdivision, out var newCanvasControlOffset);

			var result = new MapAreaInfo(newCoords, logicalDisplaySize, subdivision, newMapBlockOffset, newCanvasControlOffset);

			return result;
		}

		// Create new Poster Specs
		private void LoadMap(Poster poster, Job currentJob, MapAreaInfo mapAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType)
		{
			var newScreenArea = new RectangleInt();
			var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newScreenArea);

			Debug.WriteLine($"Starting Job with new coords: {mapAreaInfo.Coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			poster.Add(job);

			OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
		}

		private void LoadMap(Poster poster, Job currentJob, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newArea)
		{
			var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, PosterSize, coords, colorBandSetId, mapCalcSettings, transformType, newArea, _blockSize);

			Debug.WriteLine($"Starting Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			poster.Add(job);

			OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
			//OnPropertyChanged(nameof(IPosterViewModel.CanGoBack));
			//OnPropertyChanged(nameof(IPosterViewModel.CanGoForward));
		}

		//private void FindOrCreateJobForNewCanvasSize(Poster poster, Job job, SizeInt newCanvasSizeInBlocks)
		//{
		//	// Note if this job is itself a CanvasSizeUpdate Proxy Job, then its parent is used to conduct the search.
		//	if (poster.TryGetCanvasSizeUpdateProxy(job, newCanvasSizeInBlocks, out var matchingProxy))
		//	{
		//		poster.CurrentJob = matchingProxy;
		//		return;
		//	}

		//	// Make sure we use the original job and not a 'CanvasSizeUpdate Proxy Job'.
		//	if (job.TransformType == TransformType.CanvasSizeUpdate)
		//	{
		//		var preferredJob = poster.GetParent(job);

		//		if (preferredJob is null)
		//		{
		//			throw new InvalidOperationException("Could not get the preferred job as we create a new job for the updated canvas size.");
		//		}

		//		job = preferredJob;
		//	}

		//	var newCoords = RMapHelper.GetNewCoordsForNewCanvasSize(job.Coords, job.CanvasSizeInBlocks, newCanvasSizeInBlocks, job.Subdivision);

		//	var transformType = TransformType.CanvasSizeUpdate;
		//	RectangleInt? newArea = null;

		//	var newJob = _mapJobHelper.BuildJob(job.Id, poster.Id, PosterSize, newCoords, job.ColorBandSetId, job.MapCalcSettings, transformType, newArea, _blockSize);

		//	Debug.WriteLine($"Re-runing job. Current CanvasSize: {job.CanvasSizeInBlocks}, new CanvasSize: {newCanvasSizeInBlocks}.");
		//	Debug.WriteLine($"Starting Job with new coords: {newCoords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

		//	poster.Add(newJob);
		//}

		[Conditional("DEBUG")]
		private void CheckCoordsChange(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, RRectangle newCoords)
		{
			if (screenArea == new RectangleDbl(new RectangleInt(new PointInt(), mapAreaInfo.CanvasSize)))
			{
				if (Reducer.Reduce(newCoords) != Reducer.Reduce(mapAreaInfo.Coords))
				{
					Debug.WriteLine($"The new ScreenArea matches the existing ScreenArea, but the Coords were updated.");
					//throw new InvalidOperationException("if the pos has not changed, the coords should not change.");
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

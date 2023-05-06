using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly MapJobHelper _mapJobHelper;

		//private readonly SizeInt _blockSize;

		//private SizeDbl _canvasSize;
		//private SizeDbl _logicalDisplaySize;

		private Poster? _currentPoster;

		AreaColorAndCalcSettings _areaColorAndCalcSettings;
		private ColorBandSet? _previewColorBandSet;

		#endregion

		#region Constructor

		public PosterViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper, SizeInt blockSize)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;

			//_blockSize = blockSize;

			//var subdivisionProvider = new SubdivisonProvider(mapSectionAdapter);
			//_oldJobHelper = new MapJobHelper(subdivisionProvider, 10, blockSize);

			//_canvasSize = new SizeDbl();
			_currentPoster = null;
			_areaColorAndCalcSettings = AreaColorAndCalcSettings.Empty;
			_previewColorBandSet = null;
		}

		#endregion

		#region Public Properties - Derived

		public new bool InDesignMode => base.InDesignMode;

		#endregion

		#region Public Properties

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
						//var dispPos = _currentPoster.DisplayPosition;
						//OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
						//OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));

						// Setting the PosterSize and DisplayZoom can update the DisplayPosition. Use the value read from file.
						//_currentPoster.DisplayPosition = dispPos;
						//OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));

						//var currentJob = _currentPoster.CurrentJob;
						//var currentColorBandSet = _currentPoster.CurrentColorBandSet;
						//CurrentAreaColorAndCalcSettings = CreateDisplayJob(currentJob, currentColorBandSet, DisplayPosition, LogicalDisplaySize.Round(), _currentPoster.DisplayZoom);

						if (CurrentPoster != null && !CurrentPoster.CurrentJob.IsEmpty)
						{
							var currentJob = CurrentPoster.CurrentJob;
							Debug.WriteLine("The PosterViewModel is setting its CurrentAreaColorAndCalcSettings as its value of CurrentPoster is being updated.");

							//CurrentAreaColorAndCalcSettings = GetUpdatedMapView(CurrentPoster, DisplayPosition, LogicalDisplaySize, DisplayZoom);
							var areaColorAndCalcSettings = new AreaColorAndCalcSettings(currentJob.Id.ToString(), JobOwnerType.Poster, currentJob.MapAreaInfo, CurrentPoster.CurrentColorBandSet, currentJob.MapCalcSettings.Clone());
							CurrentAreaColorAndCalcSettings = areaColorAndCalcSettings;
						}

						_currentPoster.PropertyChanged += CurrentPoster_PropertyChanged;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentPoster));
					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				}
			}
		}

		public bool CurrentPosterIsDirty => CurrentPoster?.IsDirty ?? false;

		public int GetGetNumberOfDirtyJobs()
		{
			return CurrentPoster?.GetNumberOfDirtyJobs() ?? 0;
		}

		public string? CurrentPosterName => CurrentPoster?.Name;
		public bool CurrentPosterOnFile => CurrentPoster?.OnFile ?? false;

		public Job CurrentJob
		{
			get => CurrentPoster?.CurrentJob ?? Job.Empty;
			set
			{
				var currentPoster = CurrentPoster;
				if (currentPoster != null)
				{
					if (value != CurrentJob)
					{
						currentPoster.CurrentJob = value;

						currentPoster.DisplayPosition = new VectorInt();
						currentPoster.DisplayZoom = 1;

						//_logicalDisplaySize = new SizeDbl(10, 10);
						//LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);

						if (!currentPoster.CurrentJob.IsEmpty)
						{
							var currentJob = currentPoster.CurrentJob;

							Debug.WriteLine("The PosterViewModel is setting its CurrentAreaColorAndCalcSettings as its value of CurrentJob is being updated.");
							//CurrentAreaColorAndCalcSettings = GetUpdatedMapView(currentPoster, DisplayPosition, LogicalDisplaySize, DisplayZoom);

							var areaColorAndCalcSettings = new AreaColorAndCalcSettings(currentJob.Id.ToString(), JobOwnerType.Poster, currentJob.MapAreaInfo, currentPoster.CurrentColorBandSet, currentJob.MapCalcSettings.Clone());
							CurrentAreaColorAndCalcSettings = areaColorAndCalcSettings;
						}
					}
					else
					{
						Debug.WriteLine($"Not setting the CurrentJob {value.Id}, the CurrentJob already has this value.");
					}
				}
				else
				{
					Debug.WriteLine($"Not setting the CurrentJob {value.Id}, the CurrentPoster is null.");
				}
			}
		}

		public MapAreaInfo2 PosterAreaInfo => CurrentPoster?.CurrentJob.MapAreaInfo ?? MapAreaInfo2.Empty;

		public SizeInt PosterSize
		{
			get => CurrentPoster?.PosterSize ?? new SizeInt(1024);
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (value != PosterSize)
					{
						curPoster.PosterSize = value;
					}
				}
			}
		}

		public ColorBandSet CurrentColorBandSet
		{
			get => PreviewColorBandSet ?? CurrentPoster?.CurrentColorBandSet ?? new ColorBandSet();
			set
			{
				var currentProject = CurrentPoster;
				if (currentProject != null && !currentProject.CurrentJob.IsEmpty)
				{
					CheckCurrentProject(currentProject);

					// Discard the Preview ColorBandSet. 
					_previewColorBandSet = null;

					if (value == CurrentColorBandSet)
					{
						Debug.WriteLine($"PosterViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
					}

					var targetIterations = value.HighCutoff;
					var currentJob = currentProject.CurrentJob;

					if (targetIterations != currentJob.MapCalcSettings.TargetIterations)
					{
						Debug.WriteLine($"PosterViewModel is updating the Target Iterations. Current ColorBandSetId = {currentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");

						currentProject.Add(value);

						AddNewIterationUpdateJob(currentProject, value);
					}
					else
					{
						Debug.WriteLine($"PosterViewModel is updating the ColorBandSet. Current ColorBandSetId = {currentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");
						currentProject.CurrentColorBandSet = value;
					}

					OnPropertyChanged(nameof(IProjectViewModel.CurrentColorBandSet));
				}
			}
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
						//Debug.WriteLine($"The PosterViewModel's DisplayPosition is being updated to {value}.");
						curPoster.DisplayPosition = value;

						//var currentJob = curPoster.CurrentJob;
						//var currentColorBandSet = curPoster.CurrentColorBandSet;

						//var msg = $"The PosterViewModel DisplayPosition setter is setting CurrentAreaColorAndCalcSettings to " +
						//	$"DisplayPosition: {value}, LogicalDisplaySize: {LogicalDisplaySize}, Zoom: {DisplayZoom}.";
						//Debug.WriteLine(msg);

						//CurrentAreaColorAndCalcSettings = CreateDisplayJob(currentJob, currentColorBandSet, value, LogicalDisplaySize.Round(), DisplayZoom);

						//OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));
					}
				}
				else
				{
					//var msg = $"The PosterViewModel DisplayPosition setter is setting CurrentAreaColorAndCalcSettings to NULL -- The CurrentPoster is Null.";
					//Debug.WriteLine(msg);
					//CurrentAreaColorAndCalcSettings = AreaColorAndCalcSettings.Empty;
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
						//Debug.WriteLine($"The PosterViewModel's DisplayZoom is being updated to {value}.");
						//OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
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

			else if (e.PropertyName == nameof(Poster.CurrentColorBandSet))
			{
				Debug.WriteLine("The PosterViewModel is raising PropertyChanged: IPosterViewModel.CurrentColorBandSet as the Poster's ColorBandSet is being updated.");
				OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
			}

			else if (e.PropertyName == nameof(Poster.CurrentJob))
			{
				//if (CurrentPoster != null)
				//{
				//	CurrentJob = CurrentPoster.CurrentJob;
				//}

				Debug.WriteLine("The PosterViewModel is raising PropertyChanged: IPosterViewModel.CurrentJob as the Poster's CurrentJob is being updated.");
				OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));

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
			Debug.WriteLine($"Opening Poster: {name}.");
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

		public void Load(Poster poster, MapAreaInfo2? newMapArea)
		{
			if (newMapArea != null)
			{
				AddNewCoordinateUpdateJob(poster, newMapArea);
			}

			CurrentPoster = poster;
		}

		public bool PosterSave()
		{
			var poster = CurrentPoster;

			if (poster == null)
			{
				throw new InvalidOperationException("The poster must be non-null.");
			}

			if (!poster.OnFile)
			{
				throw new InvalidOperationException("Cannot save a new poster, use Save As instead.");
			}

			Debug.Assert(!poster.CurrentJob.IsEmpty, "Poster Savefound the CurrentJob to be empty.");

			var result = JobOwnerHelper.Save(poster, _projectAdapter);
			
			OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
			OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));

			return result;
		}

		public bool PosterSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText)
		{
			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				throw new InvalidOperationException("The project must be non-null.");
			}

			if (_projectAdapter.PosterExists(name, out var posterId))
			{
				if (!ProjectAndMapSectionHelper.DeletePoster(posterId, _projectAdapter, _mapSectionAdapter, out var numberOfMapSectionsDeleted))
				{
					errorText = $"Could not delete existing poster having name: {name}";
					return false;
				}
				else
				{
					Debug.WriteLine($"As new Poster is being SavedAs, overwriting exiting poster: {name}, {numberOfMapSectionsDeleted} Map Sections were deleted.");
				}
			}

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

			var poster = (Poster)JobOwnerHelper.CreateCopy(currentPoster, name, description, _projectAdapter, _mapSectionAdapter);

			if (JobOwnerHelper.Save(currentPoster, _projectAdapter))
			{
				CurrentPoster = poster;
				errorText = null;
				return true;
			}
			else
			{
				errorText = "Could not save the new poster record.";
				return false;
			}
		}

		public void Close()
		{
			CurrentPoster = null;
		}

		#endregion

		#region Public Methods - Job

		// Called in preparation to call UpdateMapSpecs
		public MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeInt currentPosterSize, RectangleDbl screenArea)
		{
			var newCenter = screenArea.GetCenter();
			var oldCenter = new PointDbl(currentPosterSize.Width / 2, currentPosterSize.Height / 2);
			var zoomPoint = newCenter.Diff(oldCenter).Round();

			var xFactor = currentPosterSize.Width / screenArea.Width;
			var yFactor = currentPosterSize.Height / screenArea.Height;
			var factor = Math.Min(xFactor, yFactor);

			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(mapAreaInfo, zoomPoint, factor);

			return newMapAreaInfo;
		}

		// Always called after GetUpdatedMapAreaInfo
		public void UpdateMapSpecs(Poster currentPoster, MapAreaInfo2 newMapAreaInfo)
		{
			AddNewCoordinateUpdateJob(currentPoster, newMapAreaInfo);
		}

		// Called in response to the MapDisplayViewModel raising a MapViewUpdateRequested event,
		// or the PosterDesignerView code behind handling a Pan or Zoom UI event.
		public void UpdateMapSpecs(TransformType transformType, VectorInt panAmount, double factor, MapAreaInfo2? diagnosticAreaInfo)
		{
			Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");

			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				return;
			}

			AddNewCoordinateUpdateJob(currentPoster, transformType, panAmount, factor);
		}

		#endregion

		#region Private Methods

		//// PosterViewModel update from position and zoom changes
		//private AreaColorAndCalcSettings GetUpdatedMapView(Poster currentPoster, VectorInt displayPosition, SizeDbl logicalDisplaySize, double zoomFactorForDiagnosis)
		//{
		//	var job = currentPoster.CurrentJob;
		//	var colorBandSet = currentPoster.CurrentColorBandSet;

		//	var msg = $"UpdateMapView is setting CurrentAreaColorAndCalcSettings to DisplayPosition: {DisplayPosition}, LogicalDisplaySize: {LogicalDisplaySize}, Zoom: {DisplayZoom}.";
		//	Debug.WriteLine(msg);

		//	// Use the new map specification and the current zoom and display position to set the region to display.
		//	var result = CreateDisplayJob(job, colorBandSet, displayPosition, logicalDisplaySize.Round(), zoomFactorForDiagnosis);

		//	return result;
		//}

		//// Get Display Job
		//private AreaColorAndCalcSettings CreateDisplayJob(Job currentJob, ColorBandSet currentColorBandSet, VectorInt displayPosition, SizeInt logicalDisplaySize, double zoomFactorForDiagnosis)
		//{
		//	var viewPortArea = GetNewViewport(currentJob.MapAreaInfo, displayPosition, logicalDisplaySize, zoomFactorForDiagnosis);

		//	var areaColorAndCalcSettings = new AreaColorAndCalcSettings(currentJob.Id.ToString(), JobOwnerType.Poster, viewPortArea, currentColorBandSet, currentJob.MapCalcSettings.Clone());

		//	return areaColorAndCalcSettings;
		//}

		//private MapAreaInfo2 GetNewViewportOld(MapAreaInfo2 currentAreaInfo, VectorInt displayPosition, SizeInt logicalDisplaySize, double zoomFactorForDiagnosis)
		//{
		//	if (CurrentPoster == null)
		//	{
		//		return currentAreaInfo;
		//	}

		//	var posterSize = CurrentPoster.PosterSize;

		//	var diagScreenArea = new RectangleInt(new PointInt(), posterSize);
		//	var screenArea = new RectangleInt(new PointInt(displayPosition), logicalDisplaySize);
		//	ReportNewDisplayInfo(diagScreenArea, screenArea, displayPosition, logicalDisplaySize, zoomFactorForDiagnosis);

		//	var totalLeft = diagScreenArea.Position.X + diagScreenArea.Width;
		//	var screenLeft = screenArea.Position.X + screenArea.Width;
		//	var totalTop = diagScreenArea.Position.Y + diagScreenArea.Height;
		//	var screenTop = screenArea.Position.Y + screenArea.Height;
		//	var trAmt = new VectorInt(Math.Max(screenLeft - totalLeft, 0), Math.Max(screenTop - totalTop, 0));

		//	//screenArea = screenArea.Translate(trAmt);
		//	//var mapPosition = currentAreaInfo.Coords.Position;
		//	//var subdivision = currentAreaInfo.Subdivision;

		//	//var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, subdivision.SamplePointDelta);
		//	//var newMapBlockOffset = RMapHelper.GetMapBlockOffset(coords.Position, subdivision.SamplePointDelta, subdivision.BlockSize, out var newCanvasControlOffset);
		//	////var newCoords = RMapHelper.CombinePosAndSize(newPosition, coords.Size);

		//	//// TODO: Check the calculated precision as the new Map Coordinates are calculated.
		//	//var precision = RValueHelper.GetPrecision(coords.Right, coords.Left, out var _);
		//	//var result = new MapAreaInfo(coords, logicalDisplaySize, subdivision, precision, newMapBlockOffset, newCanvasControlOffset);

		//	//var result = _mapJobHelper.GetMapAreaInfoPan(currentAreaInfo, trAmt);
		//	var result = _mapJobHelper.GetMapAreaInfoPan(currentAreaInfo, displayPosition);

		//	return result;
		//}

		//private MapAreaInfo2 GetNewViewport(MapAreaInfo2 currentAreaInfo, VectorInt displayPosition, SizeInt logicalDisplaySize, double zoomFactorForDiagnosis)
		//{
		//	if (CurrentPoster == null)
		//	{
		//		return currentAreaInfo;
		//	}

		//	var posterSize = CurrentPoster.PosterSize;

		//	var diagScreenArea = new RectangleInt(new PointInt(), posterSize);
		//	var screenArea = new RectangleInt(new PointInt(displayPosition), logicalDisplaySize);
		//	ReportNewDisplayInfo(diagScreenArea, screenArea, displayPosition, logicalDisplaySize, zoomFactorForDiagnosis);

		//	var posterCenter = diagScreenArea.GetCenter();
		//	var screenCenter = screenArea.GetCenter();

		//	var trAmt = new VectorDbl(screenCenter).Diff(new VectorDbl(posterCenter));

		//	var result = _mapJobHelper.GetMapAreaInfoPan(currentAreaInfo, trAmt.Round());

		//	return result;
		//}

		private void ReportNewDisplayInfo(RectangleInt diagScreenArea, RectangleInt screenArea, VectorInt displayPosition, SizeInt logicalDisplaySize, double zoomFactorForDiagnosis)
		{
			var diagSqAmt = diagScreenArea.Width * diagScreenArea.Height;
			var screenSqAmt = screenArea.Width * screenArea.Height;
			var sizeRat = screenSqAmt / (double)diagSqAmt;

			Debug.WriteLine($"Creating Viewport at pos: {displayPosition} and size: {logicalDisplaySize} zoom: {zoomFactorForDiagnosis}.");
			Debug.WriteLine($"The new Viewport covers {sizeRat}. Total Screen Area: {diagScreenArea}, viewPortArea: {screenArea}.");
		}

		// Create new Poster Specs using a new MapAreaInfo
		private void AddNewCoordinateUpdateJob(Poster poster, MapAreaInfo2 mapAreaInfo)
		{
			var currentJob = poster.CurrentJob;
			var colorBandSetId = currentJob.ColorBandSetId;
			var mapCalcSettings = currentJob.MapCalcSettings;

			// TODO: Determine TransformType
			var transformType = TransformType.ZoomIn;

			var newScreenArea = new RectangleInt();
			var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newScreenArea);

			Debug.WriteLine($"Adding Poster Job with new coords: {mapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			poster.Add(job);

			OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
		}

		private void AddNewCoordinateUpdateJob(Poster poster, TransformType transformType, VectorInt panAmount, double factor)
		{
			var currentJob = poster.CurrentJob;
			Debug.Assert(!currentJob.IsEmpty, "AddNewCoordinateUpdateJob was called while the current job is empty.");

			// Calculate the new Map Coordinates 
			var mapAreaInfo = currentJob.MapAreaInfo;

			MapAreaInfo2? newMapAreaInfo;

			if (transformType == TransformType.ZoomIn)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(mapAreaInfo, panAmount, factor);
			}
			else if (transformType == TransformType.Pan)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPan(mapAreaInfo, panAmount);
			}
			else if (transformType == TransformType.ZoomOut)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomCenter(mapAreaInfo, factor);
			}
			else
			{
				throw new InvalidOperationException($"AddNewCoordinateUpdateJob does not support a TransformType of {transformType}.");
			}

			var colorBandSetId = currentJob.ColorBandSetId;
			var mapCalcSettings = currentJob.MapCalcSettings;

			var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, newMapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea: null);

			Debug.WriteLine($"Adding Project Job with new coords: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			poster.Add(job);

			OnPropertyChanged(nameof(IProjectViewModel.CurrentJob));
		}

		private void AddNewIterationUpdateJob(Poster poster, ColorBandSet colorBandSet)
		{
			var currentJob = poster.CurrentJob;

			// Use the ColorBandSet's highCutoff to set the targetIterations of the current MapCalcSettings
			var targetIterations = colorBandSet.HighCutoff;
			var mapCalcSettings = MapCalcSettings.UpdateTargetIterations(currentJob.MapCalcSettings, targetIterations);

			// Use the current display size and Map Coordinates
			var mapAreaInfo = currentJob.MapAreaInfo;

			// This an iteration update with the same screen area
			var transformType = TransformType.IterationUpdate;
			var newScreenArea = new RectangleInt();

			//var job = _mapJobHelper.BuildJob(currentJob.Id, project.Id, mapSize, coords, colorBandSet.Id, mapCalcSettings, transformType, newScreenArea, _blockSize);
			var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, mapAreaInfo, colorBandSet.Id, mapCalcSettings, transformType, newScreenArea);

			Debug.WriteLine($"Adding Poster Job with new coords: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			poster.Add(job);

			OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
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

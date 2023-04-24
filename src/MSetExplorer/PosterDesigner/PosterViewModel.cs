using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Controls;
using Windows.UI.Accessibility;

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

			var subdivisionProvider = new SubdivisonProvider(mapSectionAdapter);
			//_oldJobHelper = new MapJobHelper(subdivisionProvider, 10, blockSize);

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
					Debug.WriteLine($"The PosterViewModel's LogicalDisplaySize is being updated to {value}.");
					_logicalDisplaySize = value;

					if (CurrentPoster != null && !CurrentPoster.CurrentJob.IsEmpty)
					{
						CurrentAreaColorAndCalcSettings = GetUpdatedMapView(CurrentPoster, DisplayPosition, LogicalDisplaySize, DisplayZoom);
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

						//var currentJob = _currentPoster.CurrentJob;
						//var currentColorBandSet = _currentPoster.CurrentColorBandSet;
						//CurrentAreaColorAndCalcSettings = CreateDisplayJob(currentJob, currentColorBandSet, DisplayPosition, LogicalDisplaySize.Round(), _currentPoster.DisplayZoom);

						if (CurrentPoster != null && !CurrentPoster.CurrentJob.IsEmpty)
						{
							CurrentAreaColorAndCalcSettings = GetUpdatedMapView(CurrentPoster, DisplayPosition, LogicalDisplaySize, DisplayZoom);
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
						_logicalDisplaySize = new SizeDbl(10, 10);
						LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);

						if (!currentPoster.CurrentJob.IsEmpty)
						{
							CurrentAreaColorAndCalcSettings = GetUpdatedMapView(currentPoster, DisplayPosition, LogicalDisplaySize, DisplayZoom);
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

		//public SizeInt PosterSize => PosterAreaInfo.CanvasSize;
		public SizeInt PosterSize => new SizeInt(1024);

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
						Debug.WriteLine($"The PosterViewModel's DisplayPosition is being updated to {value}.");
						curPoster.DisplayPosition = value;

						var currentJob = curPoster.CurrentJob;
						var currentColorBandSet = curPoster.CurrentColorBandSet;

						var msg = $"The PosterViewModel DisplayPosition setter is setting CurrentAreaColorAndCalcSettings to " +
							$"DisplayPosition: {value}, LogicalDisplaySize: {LogicalDisplaySize}, Zoom: {DisplayZoom}.";
						Debug.WriteLine(msg);

						CurrentAreaColorAndCalcSettings = CreateDisplayJob(currentJob, currentColorBandSet, value, LogicalDisplaySize.Round(), DisplayZoom);

						OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));
					}
				}
				else
				{
					var msg = $"The PosterViewModel DisplayPosition setter is setting CurrentAreaColorAndCalcSettings to NULL -- The CurrentPoster is Null.";
					Debug.WriteLine(msg);
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
						Debug.WriteLine($"The PosterViewModel's DisplayZoom is being updated to {value}.");
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

					CurrentJob = CurrentPoster.CurrentJob;
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
			Debug.WriteLine($"Opening Poster: {name}. DisplayPosition: {DisplayPosition}, LogicalDisplaySize: {LogicalDisplaySize}, Zoom: {DisplayZoom}.");
			if (_projectAdapter.TryGetPoster(name, out var poster))
			{
				if (poster.DisplayZoom - 0 < 0.01)
				{
					poster.DisplayPosition = new VectorInt(0, 0);
					poster.DisplayZoom = 1;
				}

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

		// PosterDesigner Edit Size
		public void UpdateMapSpecs(Poster currentPoster, MapAreaInfo2 newMapAreaInfo)
		{
			AddNewCoordinateUpdateJob(currentPoster, newMapAreaInfo);
		}

		// PosterDesigner Pan and Zoom Out
		public void UpdateMapSpecs(TransformType transformType, RectangleInt screenArea, SizeDbl canvasSize)
		{
			throw new NotImplementedException();
			//Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");

			//var currentPoster = CurrentPoster;
			//if (currentPoster == null)
			//{
			//	return;
			//}

			////var curJob = currentPoster.CurrentJob;

			////var mapPosition = curJob.Coords.Position;
			////var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			////var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);

			////var colorBandSetId = curJob.ColorBandSetId;
			////var mapCalcSettings = curJob.MapCalcSettings;
			////LoadMap(currentPoster, curJob, coords, colorBandSetId, mapCalcSettings, transformType, screenArea);

			//AddNewCoordinateUpdateJob(currentPoster, transformType, screenArea, canvasSize);

			////currentPoster.DisplayPosition = new VectorInt();
			////currentPoster.DisplayZoom = 1;
			////_logicalDisplaySize = new SizeDbl(10, 10);
			////LogicalDisplaySize = CanvasSize;

			////UpdateMapView(currentPoster);
		}

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

		//public MapAreaInfo GetUpdatedMapAreaInfoOLD(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize)
		//{
		//	var mapPosition = mapAreaInfo.Coords.Position;
		//	var samplePointDelta = mapAreaInfo.Subdivision.SamplePointDelta;
		//	var screenAreaInt = screenArea.Round();

		//	var coords = RMapHelper.GetMapCoords(screenAreaInt, mapPosition, samplePointDelta);

		//	CheckCoordsChange(mapAreaInfo, screenArea, coords);

		//	var posterSize = newMapSize.Round();
		//	var blockSize = mapAreaInfo.Subdivision.BlockSize;

		//	var result = _mapJobHelper.GetMapAreaInfo(coords, posterSize);

		//	return result;
		//}

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
		//		return _mapJobHelper.GetMapAreaInfo(curJob, CanvasSize);
		//	}
		//	else
		//	{
		//		var mapAreaInfo = BuildMapAreaInfo(currentMapAreaInfo, screenArea);
		//		return mapAreaInfo;
		//	}
		//}

		public MapAreaInfo2? GetUpdatedMapAreaInfoNotUsed(TransformType transformType, RectangleInt screenArea, MapAreaInfo2 currentMapAreaInfo)
		{
			var currentJob = CurrentJob;

			if (currentJob.IsEmpty)
			{
				return null;
			}

			if (screenArea == new RectangleInt())
			{
				Debug.WriteLine("GetUpdatedJobInfo was given an empty newArea rectangle.");
				//return MapJobHelper.GetMapAreaInfo(curJob, CanvasSize);
				return currentJob.MapAreaInfo;
			}
			else
			{
				var mapAreaInfo = BuildMapAreaInfo(currentMapAreaInfo, screenArea);
				return mapAreaInfo;
			}
		}

		public MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeInt posterSize, VectorInt offsetFromCenter, RectangleDbl screenArea, SizeDbl newMapSize)
		{
			var newCenter = screenArea.GetCenter();
			var oldCenter = new PointDbl(posterSize.Width / 2, posterSize.Height / 2);
			var zoomPoint = newCenter.Diff(oldCenter).Round();

			var xFactor = posterSize.Width / screenArea.Width;
			var yFactor = posterSize.Height / screenArea.Height;
			var factor = Math.Min(xFactor, yFactor);

			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(mapAreaInfo, zoomPoint, factor);

			return newMapAreaInfo;
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
		private AreaColorAndCalcSettings GetUpdatedMapView(Poster currentPoster, VectorInt displayPosition, SizeDbl logicalDisplaySize, double zoomFactorForDiagnosis)
		{
			var job = currentPoster.CurrentJob;
			var colorBandSet = currentPoster.CurrentColorBandSet;

			var msg = $"UpdateMapView is setting CurrentAreaColorAndCalcSettings to DisplayPosition: {DisplayPosition}, LogicalDisplaySize: {LogicalDisplaySize}, Zoom: {DisplayZoom}.";
			Debug.WriteLine(msg);

			// Use the new map specification and the current zoom and display position to set the region to display.
			var result = CreateDisplayJob(job, colorBandSet, displayPosition, logicalDisplaySize.Round(), zoomFactorForDiagnosis);

			return result;
		}

		// Get Display Job
		private AreaColorAndCalcSettings CreateDisplayJob(Job currentJob, ColorBandSet currentColorBandSet, VectorInt displayPosition, SizeInt logicalDisplaySize, double zoomFactorForDiagnosis)
		{
			var viewPortArea = GetNewViewPort(currentJob.MapAreaInfo, displayPosition, logicalDisplaySize, zoomFactorForDiagnosis);

			var mapCalcSettingsCpy = currentJob.MapCalcSettings.Clone();
			//mapCalcSettingsCpy.DontFetchZValuesFromRepo = true;

			var areaColorAndCalcSettings = new AreaColorAndCalcSettings(currentJob.Id.ToString(), JobOwnerType.Poster, viewPortArea, currentColorBandSet, mapCalcSettingsCpy);

			return areaColorAndCalcSettings;
		}

		private MapAreaInfo2 GetNewViewPort(MapAreaInfo2 currentAreaInfo, VectorInt displayPosition, SizeInt logicalDisplaySize, double zoomFactorForDiagnosis)
		{
			//var diagScreenArea = new RectangleInt(new PointInt(), currentAreaInfo.CanvasSize);
			//var screenArea = new RectangleInt(new PointInt(displayPosition), logicalDisplaySize);
			//var diagSqAmt = diagScreenArea.Width * diagScreenArea.Height;
			//var screenSqAmt = screenArea.Width * screenArea.Height;
			//var sizeRat = screenSqAmt / (double) diagSqAmt;

			//Debug.WriteLine($"Creating ViewPort at pos: {displayPosition} and size: {logicalDisplaySize} zoom: {zoomFactorForDiagnosis}.");
			//Debug.WriteLine($"The new ViewPort covers {sizeRat}. Total Screen Area: {diagScreenArea}, viewPortArea: {screenArea}.");

			//var totalLeft = diagScreenArea.Position.X + diagScreenArea.Width;
			//var screenLeft = screenArea.Position.X + screenArea.Width;
			//var totalTop = diagScreenArea.Position.Y + diagScreenArea.Height;
			//var screenTop = screenArea.Position.Y + screenArea.Height;
			//var trAmt = new VectorInt(Math.Max(screenLeft - totalLeft, 0), Math.Max(screenTop - totalTop, 0));
			//screenArea = screenArea.Translate(trAmt);

			//var mapPosition = currentAreaInfo.Coords.Position;
			//var subdivision = currentAreaInfo.Subdivision;

			//var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, subdivision.SamplePointDelta);
			//var newMapBlockOffset = RMapHelper.GetMapBlockOffset(coords.Position, subdivision.SamplePointDelta, subdivision.BlockSize, out var newCanvasControlOffset);
			////var newCoords = RMapHelper.CombinePosAndSize(newPosition, coords.Size);

			//// TODO: Check the calculated precision as the new Map Coordinates are calculated.
			//var precision = RValueHelper.GetPrecision(coords.Right, coords.Left, out var _);

			//var result = new MapAreaInfo(coords, logicalDisplaySize, subdivision, precision, newMapBlockOffset, newCanvasControlOffset);

			var result = currentAreaInfo.Clone();

			return result;
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

		//private void AddNewCoordinateUpdateJob(Poster poster, TransformType transformType, RectangleInt newScreenArea, SizeDbl canvasSize)
		//{
		//	var currentJob = poster.CurrentJob;

		//	Debug.Assert(!currentJob.IsEmpty, "AddNewCoordinateUpdateJob was called while the current job is empty.");

		//	//var mapPosition = currentJob.Coords.Position;
		//	//var samplePointDelta = currentJob.Subdivision.SamplePointDelta;

		//	//var newCoords = RMapHelper.GetMapCoords(newScreenArea, mapPosition, samplePointDelta);

		//	//var colorBandSetId = currentJob.ColorBandSetId;
		//	//var mapSize = currentJob.CanvasSize;
		//	//var coords = currentJob.MapAreaInfo.Coords;
		//	//var mapCalcSettings = currentJob.MapCalcSettings;

		//	// Calculate the new Map Coordinates from the newScreenArea, using the current Map's position and samplePointDelta.
		//	var mapAreaInfo = BuildMapAreaInfo(currentJob, newScreenArea, canvasSize);

		//	var colorBandSetId = currentJob.ColorBandSetId;
		//	var mapCalcSettings = currentJob.MapCalcSettings;

		//	//var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, mapSize, newCoords, colorBandSetId, mapCalcSettings, transformType, newScreenArea, _blockSize);
		//	var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newScreenArea);


		//	Debug.WriteLine($"Adding Poster Job with new coords: {job.MapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

		//	poster.Add(job);

		//	OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
		//}

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

		//private void LoadMap(Poster poster, Job currentJob, RRectangle coords, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings, TransformType transformType, RectangleInt? newScreenArea)
		//{
		//	var mapSize = currentJob.CanvasSize;
		//	var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, mapSize, coords, colorBandSetId, mapCalcSettings, transformType, newScreenArea, _blockSize);

		//	Debug.WriteLine($"Adding Poster Job with new coords: {coords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

		//	poster.Add(job);

		//	OnPropertyChanged(nameof(IPosterViewModel.CurrentJob));
		//}

		private MapAreaInfo2 BuildMapAreaInfo(Job curJob, RectangleInt screenArea, SizeDbl canvasSize)
		{
			//var mapPosition = curJob.Coords.Position;
			//var samplePointDelta = curJob.Subdivision.SamplePointDelta;
			//var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);

			//// Use the specified canvasSize, instead of this job's current value for the CanvasSize :: Updated by DRM on 4/16
			//var mapSize = canvasSize.Round();

			//var mapAreaInfo = _mapJobHelper.GetMapAreaInfo(coords, mapSize);

			var currentMapAreaInfo = curJob.MapAreaInfo;

			var zoomPoint = screenArea.GetCenter();
			var mapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(currentMapAreaInfo, zoomPoint, 3);

			return mapAreaInfo;
		}

		private MapAreaInfo2 BuildMapAreaInfo(MapAreaInfo2 currentMapAreaInfo, RectangleInt screenArea)
		{
			//var mapPosition = currentMapAreaInfo.Coords.Position;
			//var samplePointDelta = currentMapAreaInfo.Subdivision.SamplePointDelta;
			//var coords = RMapHelper.GetMapCoords(screenArea, mapPosition, samplePointDelta);

			//// Use the specified canvasSize, instead of this job's current value for the CanvasSize :: Updated by DRM on 4/16
			//var mapSize = currentMapAreaInfo.CanvasSize;

			var zoomPoint = screenArea.GetCenter();

			var mapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(currentMapAreaInfo, zoomPoint, 3);

			return mapAreaInfo;
		}


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

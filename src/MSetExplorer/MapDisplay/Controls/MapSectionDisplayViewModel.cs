﻿using MongoDB.Bson;
using MongoDB.Driver.Linq;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		#region Private Properties

		private static bool REUSE_SECTIONS_FOR_SOME_OPS;

		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;

		private BitmapGrid _bitmapGrid;

		private object _paintLocker;

		private SizeDbl _canvasSize;
		private double _displayZoom;
		//private SizeDbl _logicalDisplaySize;

		private AreaColorAndCalcSettings? _currentAreaColorAndCalcSettings;

		#endregion

		#region Constructor

		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			BlockSize = blockSize;

			REUSE_SECTIONS_FOR_SOME_OPS = true;

			_paintLocker = new object();

			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;

			//MapSections = new ObservableCollection<MapSection>();
			MapSections = new MapSectionCollection();

			_bitmapGrid = new BitmapGrid(_mapSectionHelper, BlockSize);

			_currentAreaColorAndCalcSettings = null;

			_canvasSize = new SizeDbl();
			//_logicalDisplaySize = new SizeDbl();

			DisplayZoom = 1.0;
		}

		#endregion

		#region Events

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		#endregion

		#region Public Properties - Content

		public SizeInt BlockSize { get; init; }

		//public ObservableCollection<MapSection> MapSections { get; init; }

		public MapSectionCollection MapSections { get; init; }

		public AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings
		{
			get => _currentAreaColorAndCalcSettings;
			private set
			{
				_currentAreaColorAndCalcSettings = value?.Clone() ?? null;
				OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings));
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _bitmapGrid.ColorBandSet;
			set
			{
				lock (_paintLocker)
				{
					_bitmapGrid.SetColorBandSet(value, MapSections);
				}
			}
		}

		public bool UseEscapeVelocities
		{
			get => _bitmapGrid.UseEscapeVelocities;
			set
			{
				lock (_paintLocker)
				{
					_bitmapGrid.SetUseEscapeVelocities(value, MapSections);
				}
			}
		}

		public bool HighlightSelectedColorBand
		{
			get => _bitmapGrid.HighlightSelectedColorBand;
			set
			{
				lock (_paintLocker)
				{
					_bitmapGrid.SetHighlightSelectedColorBand(value, MapSections);
				}
			}
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public WriteableBitmap Bitmap => _bitmapGrid.Bitmap;

		public Image Image => _bitmapGrid.Image;

		public SizeInt CanvasSizeInBlocks
		{
			get => _bitmapGrid.CanvasSizeInBlocks;
			set
			{
				if (value != _bitmapGrid.CanvasSizeInBlocks)
				{
					_bitmapGrid.CanvasSizeInBlocks = value;
					//HandleDisplaySizeUpdate();
				}
			}
		}

		public SizeDbl CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					_canvasSize = value;

					HandleDisplaySizeUpdate();

					OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasSize));
					OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
				}
			}
		}

		public SizeDbl LogicalDisplaySize => CanvasSize;

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value - _displayZoom) > 0.01)
				{
					//ClearMapSectionsAndBitmap(mapLoaderJobNumber: null);

					// TODX: Prevent the DisplayZoom from being set to a value that would require more than 100 x 100 blocks.
					// 1 = LogicalDisplay Size = PosterSize
					// 2 = LogicalDisplay Size Width is 1/2 PosterSize Width (Every screen pixels covers a 2x2 group of pixels from the final image, i.e., the poster.)
					// 4 = 1/4 PosterSize
					// Maximum is PosterSize / Actual CanvasSize 

					//Debug.WriteLine($"The DrawingGroup has {_screenSectionCollection.CurrentDrawingGroupCnt} item.");

					_displayZoom = value;

					// TODX: scc -- Need to place the WriteableBitmap within a DrawingGroup.
					//_scaleTransform.ScaleX = 1 / _displayZoom;
					//_scaleTransform.ScaleY = 1 / _displayZoom;

					//LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);
					//LogicalDisplaySize = CanvasSize;


					// TODO: DisplayZoom property is not being used on the MapSectionDisplayViewModel
					//OnPropertyChanged();
				}
			}
		}

		//public SizeDbl LogicalDisplaySize
		//{
		//	get => _logicalDisplaySize;
		//	private set
		//	{
		//		if (_logicalDisplaySize != value)
		//		{
		//			_logicalDisplaySize = value;

		//			Debug.WriteLine($"MapDisplay's Logical DisplaySize is now {value}.");

		//			OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
		//		}
		//	}
		//}



		#endregion

		#region Public Methods

		public int? SubmitJob(AreaColorAndCalcSettings newValue)
		{
			int? result = null;

			if (newValue != CurrentAreaColorAndCalcSettings)
			{
				var previousValue = CurrentAreaColorAndCalcSettings;
				CurrentAreaColorAndCalcSettings = newValue;

				ReportNewJobOldJob(previousValue, newValue);
				result = HandleCurrentJobChanged(previousValue, CurrentAreaColorAndCalcSettings);
			}

			return result;
		}

		private void ReportNewJobOldJob(AreaColorAndCalcSettings? previousValue, AreaColorAndCalcSettings newValue)
		{
			var currentJobId = previousValue?.OwnerId ?? ObjectId.Empty.ToString();
			var currentSamplePointDelta = previousValue?.MapAreaInfo.Subdivision.SamplePointDelta ?? new RSize();
			var newJobId = newValue.OwnerId;
			var newSamplePointDelta = newValue.MapAreaInfo.Subdivision.SamplePointDelta;

			Debug.WriteLine($"MapDisplay is handling SubmitJob. CurrentJobId: {currentJobId} ({currentSamplePointDelta}), NewJobId: {newJobId} ({newSamplePointDelta}).");
		}

		public void CancelJob()
		{
			CurrentAreaColorAndCalcSettings = null;

			lock (_paintLocker)
			{
				StopCurrentJobAndClearDisplay();
			}
		}

		public void RestartLastJob()
		{
			lock (_paintLocker)
			{
				var currentJob = CurrentAreaColorAndCalcSettings;

				if (currentJob != null && !currentJob.IsEmpty)
				{
					var newMapSections = _mapLoaderManager.Push(currentJob.OwnerId, currentJob.OwnerType, currentJob.MapAreaInfo, currentJob.MapCalcSettings, MapSectionReady, out var newJobNumber);
					_ = _bitmapGrid.ReuseAndLoad(MapSections, newMapSections, currentJob.ColorBandSet, newJobNumber, currentJob.MapAreaInfo.MapBlockOffset, currentJob.MapAreaInfo.CanvasControlOffset);
				}
			}
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				ClearDisplayInternal();
			}
		}

		#endregion

		#region Raise MapViewUpdateRequested Event Methods

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			var screenArea = e.Area;
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.ZoomIn, screenArea, CanvasSize, e.IsPreview));
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			var offset = e.Offset;

			// If the user has dragged the existing image to the right, then we need to move the map coordinates to the left.
			var invOffset = offset.Invert();
			var screenArea = new RectangleInt(new PointInt(invOffset), CanvasSize.Round());
			MapViewUpdateRequested?.Invoke(this, new MapViewUpdateRequestedEventArgs(TransformType.Pan, screenArea, CanvasSize));
		}

		#endregion

		#region Event Handlers

		private void MapSectionReady(MapSection mapSection)
		{
			_bitmapGrid.Dispatcher.Invoke(GetAndPlacePixelsWrapper, new object[] { mapSection });
		}

		public void GetAndPlacePixelsWrapper(MapSection mapSection)
		{
			if (mapSection.MapSectionVectors != null)
			{
				lock (_paintLocker)
				{
					var sectionWasDrawn = _bitmapGrid.GetAndPlacePixels(mapSection, mapSection.MapSectionVectors, out var blockPosition);

					if (sectionWasDrawn)
					{
						MapSections.Add(mapSection);
					}
				}
			}

			if (mapSection.IsLastSection)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
			}
		}

		#endregion

		#region Private Methods

		private void HandleDisplaySizeUpdate()
		{
			var mapAreaInfo = _currentAreaColorAndCalcSettings?.MapAreaInfo;
			Debug.WriteLine($"Will Request and Display New Map Sections here. Current MapAreaInfo: {mapAreaInfo}.");

			//var newCoords = RMapHelper.GetNewCoordsForNewCanvasSize(job.Coords, job.CanvasSizeInBlocks, newCanvasSizeInBlocks, job.Subdivision);

			//var transformType = TransformType.CanvasSizeUpdate;
			//RectangleInt? newArea = null;

			//var newJob = _mapJobHelper.BuildJob(job.Id, project.Id, CanvasSize, newCoords, job.ColorBandSetId, job.MapCalcSettings, transformType, newArea, _blockSize);

			//Debug.WriteLine($"Creating CanvasSizeUpdate Job. Current CanvasSize: {job.CanvasSizeInBlocks}, new CanvasSize: {newCanvasSizeInBlocks}.");
			//Debug.WriteLine($"Starting Job with new coords: {newCoords}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");
		}

		private int? HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob)
		{
			int? newJobNumber;

			var lastSectionWasIncluded = false;

			lock (_paintLocker)
			{
				if (newJob != null && !newJob.IsEmpty)
				{
					if (ShouldAttemptToReuseLoadedSections(previousJob, newJob))
					{
						newJobNumber = ReuseAndLoad(newJob, out lastSectionWasIncluded);
					}
					else
					{
						StopCurrentJobAndClearDisplay();
						newJobNumber = DiscardAndLoad(newJob, out lastSectionWasIncluded);
					}
				}
				else
				{
					StopCurrentJobAndClearDisplay();
					newJobNumber = null;
				}
			}

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			return newJobNumber;
		}

		private int? ReuseAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			lastSectionWasIncluded = false;
			int? result = null;

			var sectionsRequired = _mapSectionHelper.CreateEmptyMapSections(newJob.MapAreaInfo, newJob.MapCalcSettings);
			var loadedSections = new ReadOnlyCollection<MapSection>(MapSections);
			var sectionsToLoad = GetSectionsToLoad(sectionsRequired, loadedSections);
			var sectionsToRemove = GetSectionsToRemove(sectionsRequired, loadedSections);

			foreach (var section in sectionsToRemove)
			{
				MapSections.Remove(section);
				_mapSectionHelper.ReturnMapSection(section);
			}

			//Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {sectionsToRemove.Count}, retaining {cntRetained}, updating {cntUpdated}, shifting {shiftAmount}.");
			Debug.WriteLine($"Reusing Loaded Sections: requesting {sectionsToLoad.Count} new sections, removing {sectionsToRemove.Count}.");

			//_bitmapGrid.CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

			if (sectionsToLoad.Count > 0)
			{
				var newMapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady, out var newJobNumber);
				result = newJobNumber;
				lastSectionWasIncluded = _bitmapGrid.ReuseAndLoad(MapSections, newMapSections, newJob.ColorBandSet, newJobNumber, newJob.MapAreaInfo.MapBlockOffset, newJob.MapAreaInfo.CanvasControlOffset);
			}
			else
			{
				_bitmapGrid.Redraw(MapSections, newJob.ColorBandSet, newJob.MapAreaInfo.CanvasControlOffset);
			}

			return result;
		}

		private int DiscardAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			//_bitmapGrid.CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

			var mapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, MapSectionReady, out var newJobNumber);

			lastSectionWasIncluded = _bitmapGrid.DiscardAndLoad(mapSections, newJob.ColorBandSet, newJobNumber, newJob.MapAreaInfo.MapBlockOffset, newJob.MapAreaInfo.CanvasControlOffset);


			var sectionsToAdd = mapSections.Where(x => x.MapSectionVectors != null);

			//MapSections.
			//foreach(var mapSection in mapSections)
			//{
			//	if (mapSection.MapSectionVectors != null)
			//	{
			//		MapSections.Add(mapSection);
			//	}
			//}

			return newJobNumber;
		}

		private void StopCurrentJobAndClearDisplay()
		{
			foreach(var jobNumber in _bitmapGrid.ActiveJobNumbers)
			{
				_mapLoaderManager.StopJob(jobNumber);
			}

			_bitmapGrid.ActiveJobNumbers.Clear();

			ClearDisplayInternal();
		}

		private void ClearDisplayInternal()
		{
			_bitmapGrid.ClearDisplay();

			foreach (var ms in MapSections)
			{
				_mapSectionHelper.ReturnMapSection(ms);
			}

			MapSections.Clear();
		}

		private bool ShouldAttemptToReuseLoadedSections(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings newJob)
		{
			if (!REUSE_SECTIONS_FOR_SOME_OPS)
			{
				return false;
			}

			if (MapSections.Count == 0 || previousJob is null)
			{
				return false;
			}

			if (newJob.MapCalcSettings.TargetIterations != previousJob.MapCalcSettings.TargetIterations)
			{
				return false;
			}

			//var jobSpd = RNormalizer.Normalize(newJob.MapAreaInfo.Subdivision.SamplePointDelta, previousJob.MapAreaInfo.Subdivision.SamplePointDelta, out var previousSpd);
			//return jobSpd == previousSpd;

			var inSameSubdivision = newJob.MapAreaInfo.Subdivision.Id == previousJob.MapAreaInfo.Subdivision.Id;

			return inSameSubdivision;
		}

		private List<MapSection> GetSectionsToLoad(List<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent)
		{
			// Find all sections where exists in needed, but is not found in those present.

			var result = sectionsNeeded.Where(
				neededSection => !sectionsPresent.Any(
					presentSection => presentSection == neededSection
					&& presentSection.TargetIterations == neededSection.TargetIterations
					)
				).ToList();

			return result;
		}

		private List<MapSection> GetSectionsToRemove(List<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent)
		{
			// Find all sections where exists in present, but is not found in those needed.

			var result = sectionsPresent.Where(
				existingSection => !sectionsNeeded.Any(
					neededSection => neededSection == existingSection
					&& neededSection.TargetIterations == existingSection.TargetIterations
					)
				).ToList();

			return result;
		}

		private List<MapSection> GetSectionsToLoadX(List<MapSection> sectionsNeeded, IReadOnlyList<MapSection> sectionsPresent, out List<MapSection> sectionsToRemove)
		{
			var result = new List<MapSection>();
			sectionsToRemove = new List<MapSection>();

			foreach (var ms in sectionsPresent)
			{
				var stillNeed = sectionsNeeded.Any(presentSection => presentSection == ms && presentSection.TargetIterations == ms.TargetIterations);

				if (!stillNeed)
				{
					sectionsToRemove.Add(ms);
				}

			}

			//var result = sectionsNeeded.Where(
			//	neededSection => !sectionsPresent.Any(
			//		presentSection => presentSection == neededSection
			//		&& presentSection.TargetIterations == neededSection.TargetIterations
			//		)
			//	).ToList();

			return result;
		}

		#endregion

		#region IDisposable Support

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					//MapSections.CollectionChanged -= MapSections_CollectionChanged;
				}

				disposedValue = true;
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

using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class MapSectionDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		private static readonly bool REUSE_SECTIONS_FOR_SOME_OPS = true;

		//private readonly SynchronizationContext? _synchronizationContext;
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly IMapLoaderManager _mapLoaderManager;
		private int? _currentMapLoaderJobNumber;

		private object _paintLocker;



		private AreaColorAndCalcSettings? _currentJobAreaAndCalcSettings;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		// We're assuming that at any one given time, all entries in the JobMapOffsets Dictionary are from the same subdivision.
		// Consider tracking the sum of the Job's Subdivision's BaseMapPosition and the Job's MapOffset.
		private Dictionary<int, BigVector> _jobMapOffsets;

		#region Constructor


		public MapSectionDisplayViewModel(IMapLoaderManager mapLoaderManager, MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			BlockSize = blockSize;
			BlockRect = new Int32Rect(0, 0, BlockSize.Width, BlockSize.Height);

			_paintLocker = new object();

			_jobMapOffsets = new Dictionary<int, BigVector>();

			//_synchronizationContext = SynchronizationContext.Current;
			_mapSectionHelper = mapSectionHelper;
			_mapLoaderManager = mapLoaderManager;

			_useEscapeVelocities = true;
			_highlightSelectedColorBand = false;
			_colorBandSet = new ColorBandSet();
			_colorMap = null;

			MapSections = new ObservableCollection<MapSection>();

		}


		#endregion

		#region Public Events

		public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		public event EventHandler<int>? DisplayJobCompleted;

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }
		private Int32Rect BlockRect { get; init; }

		public ObservableCollection<MapSection> MapSections { get; init; }

		public AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings
		{
			get => _currentJobAreaAndCalcSettings;
			set
			{
				if (value != _currentJobAreaAndCalcSettings)
				{
					var previousValue = _currentJobAreaAndCalcSettings;
					_currentJobAreaAndCalcSettings = value?.Clone() ?? null;

					Debug.WriteLine($"MapDisplay is handling JobChanged. CurrentJobId: {_currentJobAreaAndCalcSettings?.OwnerId ?? ObjectId.Empty.ToString()}");
					HandleCurrentJobChanged(previousValue, _currentJobAreaAndCalcSettings);

					OnPropertyChanged(nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings));
				}
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					Debug.WriteLine($"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");
					_colorMap = LoadColorMap(value);

					if (_colorMap != null)
					{
						RedrawSections();
					}
				}
			}
		}

		public bool UseEscapeVelocities
		{
			get => _useEscapeVelocities;
			set
			{
				if (value != _useEscapeVelocities)
				{
					var strState = value ? "On" : "Off";
					Debug.WriteLine($"The MapDisplay is turning {strState} the use of EscapeVelocities.");
					_useEscapeVelocities = value;

					if (_colorMap != null)
					{
						_colorMap.UseEscapeVelocities = value;
						RedrawSections();
					}
				}
			}
		}

		public bool HighlightSelectedColorBand
		{
			get => _highlightSelectedColorBand;
			set
			{
				if (value != _highlightSelectedColorBand)
				{
					var strState = value ? "On" : "Off";
					Debug.WriteLine($"The MapDisplay is turning {strState} the Highlighting the selected ColorBand.");
					_highlightSelectedColorBand = value;

					if (_colorMap != null)
					{
						_colorMap.HighlightSelectedColorBand = value;
						RedrawSections();
					}
				}
			}
		}

		private void RedrawSections()
		{
			lock (_paintLocker)
			{
				if (_colorMap != null && _currentJobAreaAndCalcSettings != null)
				{
					RedrawSections(_colorMap, _currentJobAreaAndCalcSettings.MapAreaInfo.MapBlockOffset);
				}
			}
		}



		public bool HandleContainerSizeUpdates { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public SizeDbl ContainerSize { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public SizeDbl CanvasSize => throw new NotImplementedException();

		public VectorInt CanvasControlOffset { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		public SizeDbl LogicalDisplaySize => throw new NotImplementedException();

		public double DisplayZoom { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

		bool IMapDisplayViewModel.InDesignMode => throw new NotImplementedException();


		#endregion

		#region Public Methods

		public void CancelJob()
		{
			throw new NotImplementedException();
		}

		public void ClearDisplay()
		{
			throw new NotImplementedException();
		}

		public void RestartLastJob()
		{
			throw new NotImplementedException();
		}

		public void SubmitJob(AreaColorAndCalcSettings job)
		{
			throw new NotImplementedException();
		}

		public void UpdateMapViewPan(ImageDraggedEventArgs e)
		{
			throw new NotImplementedException();
		}

		public void UpdateMapViewZoom(AreaSelectedEventArgs e)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Event Handlers

		private void MapSectionReady(MapSection mapSection)
		{
			if (mapSection.MapSectionVectors != null)
			{
				//if (_bitmap.Dispatcher.)
				//_bitmap.Dispatcher.Invoke(GetAndPlacePixels, new object[] { mapSection, mapSection.MapSectionVectors });
			}
		}

		#endregion

		#region Private Methods

		private void HandleCurrentJobChanged(AreaColorAndCalcSettings? previousJob, AreaColorAndCalcSettings? newJob)
		{
			int? newJobNumber = null;

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
				}
			}

			if (newJobNumber.HasValue && lastSectionWasIncluded)
			{
				DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			}

			//OnPropertyChanged(nameof(Bitmap));
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

			CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

			// Refresh the display, load the sections immediately available, and send request to generate those not available.
			if (newJob.ColorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(newJob.ColorBandSet);
			}

			//ClearBitmap(_bitmap); -- Fix Me

			if (_colorMap != null)
			{
				RedrawSections(_colorMap, newJob.MapAreaInfo.MapBlockOffset);

				if (sectionsToLoad.Count > 0)
				{
					var mapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, sectionsToLoad, MapSectionReady, out var newJobNumber);
					_currentMapLoaderJobNumber = newJobNumber;
					_jobMapOffsets.Add(newJobNumber, newJob.MapAreaInfo.MapBlockOffset);

					result = newJobNumber;

					lastSectionWasIncluded = LoadAndDrawNewSections(mapSections, _colorMap, newJob.MapAreaInfo.MapBlockOffset);
				}
			}

			return result;
		}

		private int DiscardAndLoad(AreaColorAndCalcSettings newJob, out bool lastSectionWasIncluded)
		{
			CanvasControlOffset = newJob.MapAreaInfo.CanvasControlOffset;

			var mapSections = _mapLoaderManager.Push(newJob.OwnerId, newJob.OwnerType, newJob.MapAreaInfo, newJob.MapCalcSettings, MapSectionReady, out var newJobNumber);
			_currentMapLoaderJobNumber = newJobNumber;
			_jobMapOffsets.Add(newJobNumber, newJob.MapAreaInfo.MapBlockOffset);

			if (newJob.ColorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(newJob.ColorBandSet);
			}

			if (_colorMap != null)
			{
				lastSectionWasIncluded = LoadAndDrawNewSections(mapSections, _colorMap, newJob.MapAreaInfo.MapBlockOffset);
			}
			else
			{
				lastSectionWasIncluded = false;
			}

			return newJobNumber;
		}

		private bool LoadAndDrawNewSections(List<MapSection> mapSections, ColorMap colorMap, BigVector jobMapBlockOffset)
		{
			// All of these mapSections are new and have the same jobMapBlockOffset as the one provided to the method.

			var lastSectionWasIncluded = false;

			foreach (var mapSection in mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					var invertedBlockPos = GetInvertedBlockPos(mapSection.ScreenPosition);
					var loc = invertedBlockPos.Scale(BlockSize);

					MapSections.Add(mapSection);

					_mapSectionHelper.LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
					//_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);
					//--Fix Me


					if (mapSection.IsLastSection)
					{
						lastSectionWasIncluded = true;
					}
				}
			}

			return lastSectionWasIncluded;
		}

		private void RedrawSections(ColorMap colorMap, BigVector currentJobMapBlockOffset)
		{
			// The jobMapBlockOffset reflects the current content on the screen and will not change during the lifetime of this method.
			foreach (var mapSection in MapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					if (TryGetAdjustedBlockPositon(mapSection, currentJobMapBlockOffset, out var blockPosition))
					{
						if (IsBLockVisible(blockPosition.Value, new SizeInt()))             //--Fix Me
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition.Value);
							var loc = invertedBlockPos.Scale(BlockSize);

							_mapSectionHelper.LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
							//_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);
							//--Fix Me
						}
					}
					else
					{
						Debug.WriteLine($"Not drawing, the MapSectionVectors are empty.");
					}
				}
			}

			//OnPropertyChanged(nameof(Bitmap));
		}

		private void StopCurrentJobAndClearDisplay()
		{
			if (_currentMapLoaderJobNumber != null)
			{
				_mapLoaderManager.StopJob(_currentMapLoaderJobNumber.Value);
				_jobMapOffsets.Remove(_currentMapLoaderJobNumber.Value);

				_currentMapLoaderJobNumber = null;
			}

			foreach (var kvp in _jobMapOffsets)
			{
				_mapLoaderManager.StopJob(kvp.Key);
			}

			_jobMapOffsets.Clear();
			ClearMapSectionsAndBitmap();
		}

		private ColorMap LoadColorMap(ColorBandSet colorBandSet)
		{
			_colorBandSet = colorBandSet;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = _useEscapeVelocities,
				HighlightSelectedColorBand = _highlightSelectedColorBand
			};

			return colorMap;
		}

		public void ClearMapSectionsAndBitmap(int? mapLoaderJobNumber = null)
		{
			//ClearBitmap(_bitmap);  -- Fix Me

			if (mapLoaderJobNumber.HasValue)
			{
				var sectionsToRemove = MapSections.Where(x => x.JobNumber == mapLoaderJobNumber.Value).ToList();

				foreach (var ms in sectionsToRemove)
				{
					MapSections.Remove(ms);
					_mapSectionHelper.ReturnMapSection(ms);
				}
			}
			else
			{
				foreach (var ms in MapSections)
				{
					_mapSectionHelper.ReturnMapSection(ms);
				}

				MapSections.Clear();
			}
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

		#region Bitmap Methods

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			//--Fix Me

			//var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

			var result = new PointInt();

			return result;
		}

		//private void ClearBitmapOld(WriteableBitmap bitmap)
		//{
		//	var zeros = GetClearBytes(bitmap.PixelWidth * bitmap.PixelHeight * 4);
		//	var rect = new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight);

		//	bitmap.WritePixels(rect, zeros, rect.Width * 4, 0);
		//}

		private void ClearBitmap(WriteableBitmap bitmap)
		{
			//--Fix Me
			// Clear the bitmap, one row of bitmap blocks at a time.

			//var rect = new Int32Rect(0, 0, bitmap.PixelWidth, BlockSize.Height);
			//var blockRowPixelCount = bitmap.PixelWidth * BlockSize.Height;
			//var zeros = GetClearBytes(blockRowPixelCount * 4);

			//for (var vPtr = 0; vPtr < _allocatedBlocks.Height; vPtr++)
			//{
			//	var offset = vPtr * BlockSize.Height;
			//	bitmap.WritePixels(rect, zeros, rect.Width * 4, 0, offset);
			//}
		}

		//private byte[] GetClearBytes(int length)
		//{
		//	if (_pixelsToClear.Length != length)
		//	{
		//		_pixelsToClear = new byte[length];
		//	}

		//	return _pixelsToClear;
		//}

		private WriteableBitmap CreateBitmap(SizeInt size)
		{
			var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
			//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

			return result;
		}

		private void GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors)
		{
			// The current content of the screen may change from invocation to invocation of this method, but will not change while the _paintLocker is held.
			bool jobIsCompleted = false;

			lock (_paintLocker)
			{
				if (_currentJobAreaAndCalcSettings == null)
				{
					return;
				}

				var currentMapBlockOffset = _currentJobAreaAndCalcSettings.MapAreaInfo.MapBlockOffset;

				if (TryGetAdjustedBlockPositon(mapSection, currentMapBlockOffset, out var blockPosition))
				{
					if (IsBLockVisible(blockPosition.Value, new SizeInt()))             //--Fix Me
					{
						MapSections.Add(mapSection);

						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition.Value);
							var loc = invertedBlockPos.Scale(BlockSize);

							_mapSectionHelper.LoadPixelArray(mapSectionVectors, _colorMap, !mapSection.IsInverted);
							//_bitmap.WritePixels(BlockRect, mapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);
							//--Fix Me
						}
					}
				}
				else
				{
					Debug.WriteLine($"Not drawing map section: {mapSection} with adjusted block position: {blockPosition} for job number = {mapSection.JobNumber}.");
					_mapSectionHelper.ReturnMapSection(mapSection);
				}

				if (mapSection.IsLastSection)
				{
					jobIsCompleted = true;
				}
			}

			if (jobIsCompleted)
			{
				DisplayJobCompleted?.Invoke(this, mapSection.JobNumber);
				//OnPropertyChanged(nameof(Bitmap));
			}
		}

		private bool TryGetAdjustedBlockPositon(MapSection mapSection, BigVector mapBlockOffset, [NotNullWhen(true)] out PointInt? blockPosition)
		{
			blockPosition = null;
			var result = false;

			if (_jobMapOffsets.TryGetValue(mapSection.JobNumber, out var thisSectionsMapBlockOffset))
			{
				var df = thisSectionsMapBlockOffset.Diff(mapBlockOffset);

				if (df.IsZero())
				{
					blockPosition = mapSection.ScreenPosition;
					result = true;
				}
				else
				{
					if (int.TryParse(df.X.ToString(), out int x))
					{
						if (int.TryParse(df.Y.ToString(), out int y))
						{
							var offset = new VectorInt(x, y);
							blockPosition = mapSection.ScreenPosition.Translate(offset);
							result = true;
						}
					}
				}
			}

			return result;
		}

		private bool IsBLockVisible(PointInt blockPosition, SizeInt canvasSizeInBlocks)
		{
			if (blockPosition.X < 0 || blockPosition.Y < 0)
			{
				return false;
			}

			if (blockPosition.X > canvasSizeInBlocks.Width || blockPosition.Y > canvasSizeInBlocks.Height)
			{
				return false;
			}

			return true;
		}

		private bool IsCanvasSizeInWBsReasonable(SizeInt canvasSizeInWholeBlocks)
		{
			var result = !(canvasSizeInWholeBlocks.Width > 50 || canvasSizeInWholeBlocks.Height > 50);
			return result;
		}

		private void GetAndPlacePixelsExp(WriteableBitmap bitmap, PointInt blockPosition, MapSectionVectors mapSectionVectors, ColorMap colorMap, bool isInverted, bool useEscapeVelocities)
		{
			if (useEscapeVelocities)
			{
				Debug.WriteLine("UseEscapeVelocities is not supported. Resetting value.");
				useEscapeVelocities = false;
			}

			var invertedBlockPos = GetInvertedBlockPos(blockPosition);
			var loc = invertedBlockPos.Scale(BlockSize);

			_mapSectionHelper.FillBackBuffer(bitmap.BackBuffer, bitmap.BackBufferStride, loc, BlockSize, mapSectionVectors, colorMap, !isInverted, useEscapeVelocities);

			bitmap.Lock();
			bitmap.AddDirtyRect(new Int32Rect(loc.X, loc.Y, BlockSize.Width, BlockSize.Height));
			bitmap.Unlock();

			//OnPropertyChanged(nameof(Bitmap));
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

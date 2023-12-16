using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class BitmapGrid : IBitmapGrid, IDisposable
	{
		#region Private Properties

		private readonly static PixelFormat PIXEL_FORMAT = PixelFormats.Pbgra32;
		private const int DOTS_PER_INCH = 96;

		//private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		private readonly BitmapHelper _bitmapBufferLoader;

		private readonly SizeInt _blockSize;
		private readonly ObservableCollection<MapSection> _mapSections;

		private readonly Action<WriteableBitmap> _onBitmapUpdate;

		private Int32Rect _blockRect;
		private int _bitmapStride;

		private SizeDbl _logicalViewportSize;
		private VectorInt _canvasControlOffset;
		private SizeInt _imageSizeInBlocks;
		private SizeInt _canvasSizeInBlocks;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear;

		private readonly bool _useDetailedDebug = false;

		//private int noSkippedFillBackbufferOps = 0;

		#endregion

		#region Constructor

		public BitmapGrid(ObservableCollection<MapSection> mapSections, SizeDbl viewPortSize, Action<WriteableBitmap> onBitmapUpdate, SizeInt blockSize)
		{
			_mapSections = mapSections;
			_logicalViewportSize = viewPortSize;
			_canvasControlOffset = new VectorInt();

			_onBitmapUpdate = onBitmapUpdate;
			_blockSize = blockSize;

			_bitmapBufferLoader = new BitmapHelper();

			ImageSizeInBlocks = CalculateImageSize(_logicalViewportSize, _canvasControlOffset);
			CanvasSizeInBlocks = CalculateCanvasSize(_logicalViewportSize);

			//_bitmap = CreateBitmap(ImageSizeInBlocks);
			_bitmap = CreateBitmap(CanvasSizeInBlocks);

			_pixelsToClear = new byte[0];
			_blockRect = new Int32Rect(0, 0, _blockSize.Width, _blockSize.Height);
			_bitmapStride = _blockSize.Width * BYTES_PER_PIXEL;

			MapBlockOffset = new VectorLong();

			_colorBandSet = new ColorBandSet();
			_useEscapeVelocities = true;
			_highlightSelectedColorBand = false;
			_colorMap = null;
		}

		#endregion

		#region Public Properties

		public Dispatcher Dispatcher => Bitmap.Dispatcher;

		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			private set
			{
				_bitmap = value;
				_onBitmapUpdate(_bitmap);
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");
					_colorBandSet = value;

					if (_colorMap != null)
					{
						_colorMap.Dispose();
					}
					_colorMap = LoadColorMap(value);

					if (_colorMap != null)
					{
						ReDrawSections(reapplyColorMap: true);
					}
				}
				else
				{
					if (HighlightSelectedColorBand && value.SelectedColorBand != _colorBandSet.SelectedColorBand)
					{
						//if (_colorMap != null)
						//{
						//	ReDrawSections(reapplyColorMap: true);
						//}

						CurrentColorBand = value.SelectedColorBand;
					}
				}
			}
		}

		public ColorBand? CurrentColorBand
		{
			get => _colorBandSet.SelectedColorBand;
			set
			{
				_colorBandSet.SelectedColorBand = value;

				if (HighlightSelectedColorBand && _colorMap != null)
				{
					ReDrawSections(reapplyColorMap: true);
				}
			}
		}

		public int SelectedColorBandIndex
		{
			get => ColorBandSet.SelectedColorBandIndex;
			set
			{
				ColorBandSet.SelectedColorBandIndex = value;

				if (HighlightSelectedColorBand && _colorMap != null)
				{
					ReDrawSections(reapplyColorMap: true);
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
					Debug.WriteLineIf(_useDetailedDebug, $"The MapDisplay is turning {strState} the use of EscapeVelocities.");
					_useEscapeVelocities = value;

					if (_colorMap != null)
					{
						_colorMap.UseEscapeVelocities = value;
						ReDrawSections(reapplyColorMap: true);
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
					Debug.WriteLineIf(_useDetailedDebug, $"The MapDisplay is turning {strState} the Highlighting the selected ColorBand.");
					_highlightSelectedColorBand = value;

					if (_colorMap != null)
					{
						_colorMap.HighlightSelectedColorBand = value;
						ReDrawSections(reapplyColorMap: true);
					}
				}
			}
		}

		public VectorLong MapBlockOffset { get; set; }

		public SizeDbl LogicalViewportSize
		{
			get => _logicalViewportSize;
			set
			{
				var imageSizeInBlocks = CalculateImageSize(value, _canvasControlOffset);

				if (imageSizeInBlocks != ImageSizeInBlocks)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGrid is having its LogicalViewportSize updated from {_logicalViewportSize} to {value}. ImageSizeInBlocks from: {ImageSizeInBlocks} to {imageSizeInBlocks}.");
					ImageSizeInBlocks = imageSizeInBlocks;
				}
				//else
				//{
				//	Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGrid is having its LogicalViewportSize updated from {_logicalViewportSize} to {value}. ImageSizeInBlocks remains the same.");
				//}

				CanvasSizeInBlocks = CalculateCanvasSize(value);

				_logicalViewportSize = value;
			}
		}

		public VectorInt CanvasControlOffset
		{
			get => _canvasControlOffset;
			set
			{
				var imageSizeInBlocks = CalculateImageSize(_logicalViewportSize, value);

				if (imageSizeInBlocks != ImageSizeInBlocks)
				{
					Debug.WriteLine($"WARNING: As the CanvasControlOffset is updated from {_canvasControlOffset} to {value}, the ImageSizeInBlocks is being updated from: {ImageSizeInBlocks} to {imageSizeInBlocks}.");

					ImageSizeInBlocks = imageSizeInBlocks;
				}
				//else
				//{
				//	Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGrid is having its CanvasControlOffset updated from {_canvasControlOffset} to {value}. ImageSizeInBlocks remains the same.");
				//}

				_canvasControlOffset = value;
			}
		}

		public SizeInt CalculateImageSize(SizeDbl logicalViewportSize, VectorInt canvasControlOffset)
		{
			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(logicalViewportSize.Round(), canvasControlOffset, _blockSize);
			return mapExtentInBlocks;
		}

		public SizeInt CalculateCanvasSize(SizeDbl logicalViewportSize)
		{
			var mapExtentInBlocks = RMapHelper.GetMaximumMapExtentInBlocks(logicalViewportSize.Round(), _blockSize);
			return mapExtentInBlocks;
		}

		public SizeInt ImageSizeInBlocks
		{
			get => _imageSizeInBlocks;
			set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (value != _imageSizeInBlocks)
				{
					// The Image must be able to accommodate one block before and one block after the set of visible blocks.
					//var newImageSizeInBlocks = value.Inflate(2);

					Debug.WriteLineIf(_useDetailedDebug, $"BitmapGrid Updating the ImageSizeInBlocks from: {_imageSizeInBlocks} to: {value}.");
					_imageSizeInBlocks = value;
				}
			}
		}

		// Each time a drawing operation is performed this is checked to see if the current canvas need to be resized.
		// NOTE: Every drawing operation should begin with a call to ClearDisplay or RedrawSections.
		public SizeInt CanvasSizeInBlocks
		{
			get => _canvasSizeInBlocks;
			set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (value != _canvasSizeInBlocks)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGrid. Updating the CanvasSizeInBlocks from {_canvasSizeInBlocks} to {value}.");
					_canvasSizeInBlocks = value;
				}
			}
		}

		//public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public void ClearDisplay()
		{
			var bitmapSize = new SizeDbl(_bitmap.Width, _bitmap.Height);

			if (RefreshBitmap(bitmapSize, CanvasSizeInBlocks, out var bitmap))
			{
				Debug.WriteLine("WARNING: Creating a new bitmap, just to clear the display.");
				Bitmap = bitmap;
			}
			else
			{
				ClearBitmap(Bitmap);
			}
		}

		public void DrawSections(IList<MapSection> mapSections)
		{
			var errors = 0L;
			//var anyDrawnOnLastRow = false;

			foreach (var mapSection in mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					DrawOneSection(mapSection, mapSection.MapSectionVectors, "DrawSections");
				}
			}

			if (errors > 0)
			{
				Debug.WriteLine($"There were {errors} color placement errors while Drawing Sections for {mapSections.FirstOrDefault()?.JobNumber}.");
			}

			//if (mapSections.Count > 0 && !anyDrawnOnLastRow)
			//{
			//	Debug.WriteLine($"No blocks were drawn on the last row for DrawSections: {mapSections.FirstOrDefault()?.JobNumber}.");
			//}
		}

		public int ClearSections(List<Tuple<int, PointInt, VectorLong>> jobAndScreenPositions)
		{
			var numberCleared = 0;

			var blockRowPixelCount = Bitmap.PixelWidth * _blockSize.Height;
			var zeros = GetClearBytes(blockRowPixelCount * BYTES_PER_PIXEL);

			foreach (var jobAndScreenPosition in jobAndScreenPositions)
			{
				var jobNumber = jobAndScreenPosition.Item1;
				var screenPosition = jobAndScreenPosition.Item2;
				var jobMapBlockOffsetForSection = jobAndScreenPosition.Item3;

				var blockPosition = GetAdjustedBlockPositon(screenPosition, jobMapBlockOffsetForSection, MapBlockOffset);

				if (IsBLockVisible(blockPosition, ImageSizeInBlocks))
				{
					var invertedBlockPos = GetInvertedBlockPos(blockPosition);
					var loc = invertedBlockPos.Scale(_blockSize);

					try
					{
						Bitmap.WritePixels(_blockRect, zeros, _bitmapStride, loc.X, loc.Y);
						numberCleared++;
					}
					catch (Exception e)
					{
						Debug.WriteLine($"DrawSections got exception: {e.Message}. JobNumber: {jobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
					}
				}
			}

			return numberCleared;
		}

		public int ReDrawSections(bool reapplyColorMap)
		{
			if (_colorMap != null)
			{
				var bitmapSize = new SizeDbl(_bitmap.Width, _bitmap.Height);

				if (RefreshBitmap(bitmapSize, CanvasSizeInBlocks, out var bitmap))
				{
					Bitmap = bitmap;
				}
			}

			//noSkippedFillBackbufferOps = 0;
			var numberSectionsNotDrawn = 0;

			foreach (var mapSection in _mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					if (!DrawOneSection(mapSection, mapSection.MapSectionVectors, "RedrawSections", reapplyColorMap))
					{
						numberSectionsNotDrawn++;
					}
				}
			}

			ReportPercentMapSectionsWithUpdatedScrPos();

			//Debug.WriteLine($"ReDraw skipped: {noSkippedFillBackbufferOps} fill BackBuffer operations.");

			return numberSectionsNotDrawn;
		}

		public bool DrawOneSection(MapSection mapSection, MapSectionVectors mapSectionVectors, string description, bool reapplyColorMap = true)
		{
			if (mapSection.MapSectionVectors == null) return false;
			//CheckBitmapSize(Bitmap, ImageSizeInBlocks, description);

			var wasAdded = false;
			var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

			if (IsBLockVisible(blockPosition, ImageSizeInBlocks))
			{
				var invertedBlockPos = GetInvertedBlockPos(blockPosition);
				wasAdded = true;

				if (_colorMap != null)
				{
					//var invertedBlockPos = GetInvertedBlockPos(blockPosition);
					var loc = invertedBlockPos.Scale(_blockSize);

					if (reapplyColorMap || !mapSectionVectors.BackBufferIsLoaded)
					{
						//var errors = _bitmapBufferLoader.LoadPixelArray(mapSection, _colorMap);
						var errors = _bitmapBufferLoader.LoadPixelArray(mapSection, _colorMap, mapSection.MapSectionVectors.BackBuffer);
						mapSection.MapSectionVectors.BackBufferIsLoaded = true;

						if (errors > 0)
						{
							Debug.WriteLine($"There were {errors} color placement errors while Drawing Section on the UI thread for {mapSection.JobNumber}.");
						}
					}
					//else
					//{
					//	noSkippedFillBackbufferOps++;
					//}

					try
					{
						//Debug.WriteLine($"GetAndPlacePixels is drawing MapSection: {mapSection.ToString(blockPosition)}({mapSection.RequestNumber}).");
						Bitmap.WritePixels(_blockRect, mapSectionVectors.BackBuffer, _bitmapStride, loc.X, loc.Y);
					}
					catch (Exception e)
					{
						Debug.WriteLine($"DrawOneSection-{description} got exception: {e.Message}. {mapSection.ToString(invertedBlockPos)}, CanvasSize:{CanvasSizeInBlocks}, ImageSize:{ImageSizeInBlocks}; BitmapSize: {Bitmap.PixelWidth} x {Bitmap.PixelHeight}.");
					}
				}
			}
			else
			{
				ReportMapSectionNotVisible(blockPosition, mapSection, description);
			}

			return wasAdded;
		}

		public List<MapSection> GetSectionsNotVisible()
		{
			var sectionsNotVisible = new List<MapSection>();

			foreach (var mapSection in _mapSections)
			{
				var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

				if (!IsBLockVisible(blockPosition, ImageSizeInBlocks))
				{
					sectionsNotVisible.Add(mapSection);
				}
			}

			return sectionsNotVisible;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mapSection"></param>
		/// <param name="jobMapBlockOffset">The block offset for the block at the lower, left-hand side of the map</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		private PointInt GetAdjustedBlockPositon(MapSection mapSection, VectorLong jobMapBlockOffset)
		{
			PointInt screenPosition;

			var df = mapSection.JobMapBlockOffset.Sub(jobMapBlockOffset);

			if (df.EqualsZero)
			{
				screenPosition = mapSection.ScreenPosition;
			}
			else
			{
				if (!df.TryConvertToInt(out var offset))
				{
					throw new ArgumentException($"Cannot convert the result of subtracting the JobMapBlockOffset for the current display from the JobMapBlockOffset that was used to create this MapSection. " +
						$"Current JobMapBlockOffset: {jobMapBlockOffset} This section's JobMapBlockOffset: {mapSection.JobMapBlockOffset}.");
				}

				screenPosition = mapSection.ScreenPosition.Translate(offset);

				// Update the mapSection's JobMapBlockOffset and ScreenPosition to avoid this transalation again.
				mapSection.UpdateJobMapBlockOffsetAndPos(jobMapBlockOffset, screenPosition);
				CheckScreenPos(mapSection);
			}

			return screenPosition;
		}

		private PointInt GetAdjustedBlockPositon(PointInt screenPosition, VectorLong jobMapBlockOffsetForSection, VectorLong jobMapBlockOffset)
		{
			PointInt result;

			var df = jobMapBlockOffsetForSection.Sub(jobMapBlockOffset);

			if (df.EqualsZero)
			{
				result = screenPosition;
			}
			else
			{
				if (!df.TryConvertToInt(out var offset))
				{
					throw new ArgumentException($"Cannot convert the result of subtracting the JobMapBlockOffset for the current display from the JobMapBlockOffset that was used to create this MapSection. " +
						$"Current JobMapBlockOffset: {jobMapBlockOffset} This section's JobMapBlockOffset: {jobMapBlockOffsetForSection}.");
				}

				result = screenPosition.Translate(offset);
			}

			return result;
		}

		private bool IsBLockVisible(PointInt blockPosition, SizeInt imageSizeInBlocks/*, int jobNumber, string desc, bool warnOnFail = false*/)
		{
			if (blockPosition.X < 0 || blockPosition.Y < 0)
			{
				//if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {jobNumber}. BlockPosition: {blockPosition} is negative.");
				return false;
			}

			//CheckBitmapSize(Bitmap, imageSizeInBlocks, desc);

			if (blockPosition.X >= imageSizeInBlocks.Width || blockPosition.Y >= imageSizeInBlocks.Height)
			{
				//if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {jobNumber}. BlockPosition: {blockPosition}, ImageSize: {imageSizeInBlocks}.");
				return false;
			}

			return true;
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			CheckCanvasSize(Bitmap, CanvasSizeInBlocks, "GetInvertedBlockPosition");
			var maxYPtr = CanvasSizeInBlocks.Height - (CanvasSizeInBlocks.Height - (ImageSizeInBlocks.Height - 1));
			var result = new PointInt(blockPosition.X, maxYPtr - blockPosition.Y);

			return result;
		}

		private ColorMap? LoadColorMap(ColorBandSet colorBandSet)
		{
			_colorBandSet = colorBandSet;

			if (colorBandSet.Count < 2)
			{
				return null;
			}
			else
			{
				var colorMap = new ColorMap(colorBandSet)
				{
					UseEscapeVelocities = _useEscapeVelocities,
					HighlightSelectedColorBand = _highlightSelectedColorBand
				};

				return colorMap;
			}
		}

		private bool RefreshBitmap(SizeDbl bitmapSize, SizeInt canvasSizeInBlocks, [NotNullWhen(true)] out WriteableBitmap? bitmap)
		{
			var canvasSize = canvasSizeInBlocks.Scale(_blockSize);

			if (bitmapSize.Width != canvasSize.Width || bitmapSize.Height != canvasSize.Height)
			{
				Debug.WriteLine($"BitmapGrid RefreshBitmap is being called. BitmapSize {bitmapSize} != CanvasSize: Creating new bitmap with size: {canvasSize}.");

				bitmap = CreateBitmap(canvasSizeInBlocks);
				return true;
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"BitmapGrid RefreshBitmap is being called. BitmapSize {bitmapSize} = CanvasSize. Not creating a new bitmap.");
				bitmap = null;
				return false;
			}
		}

		private void ClearBitmap(WriteableBitmap bitmap)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGrid ClearBitmap is being called. BitmapSize {ImageSizeInBlocks}.");

			// Clear the bitmap, one row of bitmap blocks at a time.
			var rect = new Int32Rect(0, 0, bitmap.PixelWidth, _blockSize.Height);
			var blockRowPixelCount = bitmap.PixelWidth * _blockSize.Height;
			var stride = rect.Width * BYTES_PER_PIXEL;
			var zeros = GetClearBytes(blockRowPixelCount * BYTES_PER_PIXEL);

			for (var vPtr = 0; vPtr < ImageSizeInBlocks.Height; vPtr++)
			{
				var offset = vPtr * _blockSize.Height;
				bitmap.WritePixels(rect, zeros, stride, 0, offset);
			}
		}

		private byte[] GetClearBytes(int length)
		{
			if (_pixelsToClear.Length != length)
			{
				_pixelsToClear = new byte[length];
			}

			return _pixelsToClear;
		}

		private WriteableBitmap CreateBitmap(SizeInt imageSizeInBlocks)
		{
			var size = imageSizeInBlocks.Scale(_blockSize);

			if (imageSizeInBlocks.NumberOfCells > 550) // 550 x 128 x 128 = ~9 Megapixels
			{
				Debug.WriteLine($"Creating a HUGE Bitmap. Size is {imageSizeInBlocks} / ({size}).");
			}

			try
			{
				var result = new WriteableBitmap(size.Width, size.Height, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);
				//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

				return result;
			} 
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e} while attempting to create the bitmap. Creating place holder bitmap with size = 10.");

				var result = new WriteableBitmap(10, 10, DOTS_PER_INCH, DOTS_PER_INCH, PIXEL_FORMAT, null);
				return result;
			}
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportMapSectionNotVisible(PointInt blockPosition, MapSection mapSection, string description)
		{
			var invertedBlockPos = GetInvertedBlockPos(blockPosition);
			Debug.WriteLine($"DrawOneSection-{description} is not drawing MapSection: {mapSection.ToString(invertedBlockPos)}, ImageSize:{ImageSizeInBlocks}, it's off the map.");
		}

		[Conditional("DEBUG2")]
		private void CheckBitmapSize(WriteableBitmap bitmap, SizeInt imageSizeInBlocks, string desc)
		{
			var imageSize = imageSizeInBlocks.Scale(_blockSize);
			var bitmapSize = new SizeInt(bitmap.PixelWidth, bitmap.PixelHeight);

			if (bitmapSize != imageSize)
			{
				Debug.WriteLine($"ImageSizeInBlocks != Bitmap Size. On {desc}.");
			}
		}

		[Conditional("DEBUG2")]
		private void CheckCanvasSize(WriteableBitmap bitmap, SizeInt canvasSizeInBlocks, string desc)
		{
			var canvasSize = canvasSizeInBlocks.Scale(_blockSize);
			var bitmapSize = new SizeInt(bitmap.PixelWidth, bitmap.PixelHeight);

			if (bitmapSize != canvasSize)
			{
				Debug.WriteLine($"ImageSizeInBlocks != Bitmap Size. On {desc}.");
			}
		}

		[Conditional("DEBUG2")]
		private void CheckScreenPos(MapSection mapSection)
		{
			var sectionBlockOffset = RMapHelper.ToSubdivisionCoords(mapSection.ScreenPosition, mapSection.JobMapBlockOffset, out var isInverted);
			//var sectionBlockOffset = _dtoMapper.Convert(bigVectorBlockOffset);

			Debug.Assert(sectionBlockOffset == mapSection.SectionBlockOffset && isInverted == mapSection.IsInverted, "Screen Position does not agree with the JobMapBlockOffset / SectionBlockOffset.");
			Debug.Assert(isInverted == mapSection.IsInverted, "IsInverted does not agree with the JobMapBlockOffset / SectionBlockOffset.");
		}

		[Conditional("DEBUG2")]
		private void ReportPercentMapSectionsWithUpdatedScrPos()
		{
			var numberOfMapSectionsWithUpdatedScrPos = _mapSections.Count(x => x.ScreenPosHasBeenUpdated);
			var percentWithUpdatedScrPos = 100 * (numberOfMapSectionsWithUpdatedScrPos / (double)_mapSections.Count);
			Debug.WriteLine($"{percentWithUpdatedScrPos:F3} MapSections have updated Screen Positions.");
		}

		#endregion

		#region Unsafe Load Pixel Array methods, Not Used.

		// Uses the unsafe method FillBackBuffer

		//private void GetAndPlacePixelsExp(WriteableBitmap bitmap, PointInt blockPosition, MapSectionVectors mapSectionVectors, ColorMap colorMap, bool isInverted, bool useEscapeVelocities)
		//{
		//	if (useEscapeVelocities)
		//	{
		//		Debug.WriteLine("UseEscapeVelocities is not supported. Resetting value.");
		//		useEscapeVelocities = false;
		//	}

		//	var invertedBlockPos = GetInvertedBlockPos(blockPosition);
		//	var loc = invertedBlockPos.Scale(_blockSize);

		//	//FillBackBuffer(bitmap.BackBuffer, bitmap.BackBufferStride, loc, BlockSize, mapSectionVectors, colorMap, !isInverted, useEscapeVelocities);

		//	bitmap.Lock();
		//	bitmap.AddDirtyRect(new Int32Rect(loc.X, loc.Y, _blockSize.Width, _blockSize.Height));
		//	bitmap.Unlock();

		//}

		//public unsafe void FillBackBuffer(IntPtr backBuffer, int backBufferStride, PointInt destination, SizeInt destSize, 
		//	MapSectionVectors mapSectionVectors, ColorMap colorMap, bool invert, bool useEscapeVelocities)
		//{
		//	Debug.Assert(mapSectionVectors.ReferenceCount > 0, "Getting the Pixel Array from a MapSectionVectors whose RefCount is < 1.");

		//	if (useEscapeVelocities)
		//	{
		//		throw new InvalidOperationException("Not supporting EscapeVelocities at this time.");
		//	}

		//	var counts = mapSectionVectors.Counts;

		//	var sourceStride = destSize.Width;
		//	var sourceRowPtr = invert ? (destSize.Height - 1) * sourceStride : 0;
		//	var sourceRowPtrIncrement = invert ? -1 * sourceStride : sourceStride;

		//	// Start the resultRowPtr at the first row of the destination
		//	var resultRowPtr = destination.Y * backBufferStride;

		//	// Advance the resultRowPtr to the first pixel in the destinataion
		//	resultRowPtr += destination.X * BYTES_PER_PIXEL;

		//	for (var rowPtr = 0; rowPtr < destSize.Height; rowPtr++)
		//	{
		//		var sourcePtr = sourceRowPtr;
		//		var resultPtr = resultRowPtr;

		//		for (var colPtr = 0; colPtr < destSize.Width; colPtr++)
		//		{
		//			var countVal = counts[sourcePtr];

		//			//colorMap.PlaceColor(countVal, escapeVelocity: 0, new Span<byte>(result, resultPtr, BYTES_PER_PIXEL));

		//			try
		//			{
		//				//var destBuf = new Span<byte>(IntPtr.Add(backBuffer, resultPtr).ToPointer(), BYTES_PER_PIXEL);

		//				var destPtr = IntPtr.Add(backBuffer, resultPtr);
		//				colorMap.PlaceColor(countVal, escapeVelocity: 0, destPtr);
		//			}
		//			catch (Exception e)
		//			{
		//				Debug.WriteLine($"Got exception: {e}.");
		//				throw;
		//			}

		//			sourcePtr += 1;
		//			resultPtr += BYTES_PER_PIXEL;
		//		}

		//		sourceRowPtr += sourceRowPtrIncrement;
		//		resultRowPtr += backBufferStride;
		//	}
		//}

		// FillBackBuffer notes

		//Consider using Marshal.WriteInt32(backBuffer, 10, 15);

		//Also consider using this to return an int
		//		byte alpha = 255;
		//		byte red = pixelArray[y, x, 0];
		//		byte green = pixelArray[y, x, 1];
		//		byte blue = pixelArray[y, x, 2];
		//		uint pixelValue = (uint)red + (uint)(green << 8) + (uint)(blue << 16) + (uint)(alpha << 24);
		//		pixelValues[y * width + x] = pixelValue;

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

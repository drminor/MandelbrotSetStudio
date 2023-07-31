using MSS.Common;
using MSS.Types;
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
	public class BitmapGrid : IDisposable, IBitmapGrid
	{
		#region Private Properties

		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		private readonly SizeInt _blockSize;
		private readonly Action<MapSection> _disposeMapSection;
		private readonly Action<WriteableBitmap> _onBitmapUpdate;

		private readonly ObservableCollection<MapSection> _mapSections;

		private Int32Rect _blockRect { get; init; }

		private SizeDbl _logicalViewportSize;
		private VectorInt _canvasControlOffset;
		private SizeInt _imageSizeInBlocks;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public BitmapGrid(ObservableCollection<MapSection> mapSections, SizeDbl viewPortSize, Action<MapSection> disposeMapSection, Action<WriteableBitmap> onBitmapUpdate, SizeInt blockSize)
		{
			_mapSections = mapSections;
			_logicalViewportSize = viewPortSize;
			_canvasControlOffset = new VectorInt();

			_disposeMapSection = disposeMapSection;
			_onBitmapUpdate = onBitmapUpdate;
			_blockSize = blockSize;

			ImageSizeInBlocks = CalculateImageSize(_logicalViewportSize, _canvasControlOffset);

			_bitmap = CreateBitmap(ImageSizeInBlocks);

			_pixelsToClear = new byte[0];
			_blockRect = new Int32Rect(0, 0, _blockSize.Width, _blockSize.Height);

			MapBlockOffset = new BigVector();

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
					_colorMap = LoadColorMap(value);

					if (_colorMap != null)
					{
						ReDrawSections();
					}
				}
				else
				{
					if (HighlightSelectedColorBand && value.SelectedColorBand != _colorBandSet.SelectedColorBand)
					{
						if (_colorMap != null)
						{
							ReDrawSections();
						}
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
					ReDrawSections();
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
						ReDrawSections();
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
						ReDrawSections();
					}
				}
			}
		}

		public BigVector MapBlockOffset { get; set; }

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
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGrid is having its LogicalViewportSize updated from {_logicalViewportSize} to {value}. ImageSizeInBlocks remains the same.");
				}

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
					Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGrid is having its CanvasControlOffset updated from {_canvasControlOffset} to {value}. ImageSizeInBlocks from: {ImageSizeInBlocks} to {imageSizeInBlocks}.");
					ImageSizeInBlocks = imageSizeInBlocks;
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The BitmapGrid is having its CanvasControlOffset updated from {_canvasControlOffset} to {value}. ImageSizeInBlocks remains the same.");
				}

				_canvasControlOffset = value;
			}
		}

		private SizeInt CalculateImageSize(SizeDbl logicalViewportSize, VectorInt canvasControlOffset)
		{
			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(logicalViewportSize.Round(), canvasControlOffset, _blockSize);

			//var adjMapExtentInBlocks = mapExtentInBlocks.Inflate(1);
			var adjMapExtentInBlocks = mapExtentInBlocks;

			return adjMapExtentInBlocks;
		}

		// Each time a drawing operation is performed this is checked to see if the current canvas need to be resized.
		// NOTE: Every drawing operation should begin with a call to ClearDisplay or RedrawSections.
		public SizeInt ImageSizeInBlocks
		{
			get => _imageSizeInBlocks;
			set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (_imageSizeInBlocks != value)
				{
					// The Image must be able to accommodate one block before and one block after the set of visible blocks.
					//var newImageSizeInBlocks = value.Inflate(2);

					//Debug.WriteLineIf(_useDetailedDebug, $"BitmapGrid Updating the ImageSizeInBlocks from: {ImageSizeInBlocks} to: {newImageSizeInBlocks}.");
					_imageSizeInBlocks = value;
				}
			}
		}

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public void ClearDisplay()
		{
			var bitmapSize = new SizeDbl(_bitmap.Width, _bitmap.Height);
			if (RefreshBitmap(bitmapSize, out var bitmap))
			{
				//Debug.WriteLine("WARNING: Creating a new bitmap, just to clear the display.");
				Bitmap = bitmap;
			}
			else
			{
				ClearBitmap(Bitmap);
			}

			foreach (var ms in _mapSections)
			{
				_disposeMapSection(ms);
			}

			_mapSections.Clear();
		}

		public void DrawSections(IList<MapSection> mapSections)
		{
			var anyDrawnOnLastRow = false;

			foreach (var mapSection in mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

					if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, "Draw", warnOnFail: true))
					{
						_mapSections.Add(mapSection);

						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);

							if (invertedBlockPos.Y == 0) anyDrawnOnLastRow = true;
							
							var loc = invertedBlockPos.Scale(_blockSize);

							LoadPixelArray(mapSection.MapSectionVectors, _colorMap, !mapSection.IsInverted);

							try
							{
								Bitmap.WritePixels(_blockRect, mapSection.MapSectionVectors.BackBuffer, _blockRect.Width * BYTES_PER_PIXEL, loc.X, loc.Y);
							}
							catch (Exception e)
							{
								Debug.WriteLine($"DrawSections got exception: {e.Message}. JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
							}
						}
					}
					else
					{
						_disposeMapSection(mapSection);
					}
				}
			}

			if (mapSections.Count > 0 && !anyDrawnOnLastRow)
				Debug.WriteLine($"No blocks were drawn on the last row for DrawSections: {mapSections.FirstOrDefault()?.JobNumber}.");
		}

		public int ClearSections(IList<MapSection> mapSections)
		{
			var numberCleared = 0;

			var blockRowPixelCount = Bitmap.PixelWidth * _blockSize.Height;
			var zeros = GetClearBytes(blockRowPixelCount * BYTES_PER_PIXEL);
			var sourceStride = Bitmap.PixelWidth * BYTES_PER_PIXEL;

			foreach (var mapSection in mapSections)
			{
				var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

				if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, "Clear", warnOnFail: true))
				{
					var invertedBlockPos = GetInvertedBlockPos(blockPosition);
					var loc = invertedBlockPos.Scale(_blockSize);

					try
					{
						Bitmap.WritePixels(_blockRect, zeros, sourceStride, loc.X, loc.Y);
						numberCleared++;
						
						//_mapSections.Remove(mapSection);
						//_disposeMapSection(mapSection);
					}
					catch (Exception e)
					{
						Debug.WriteLine($"DrawSections got exception: {e.Message}. JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
					}
				}
			}

			return numberCleared;
		}

		public int ReDrawSections()
		{
			if (_colorMap != null)
			{
				var bitmapSize = new SizeDbl(_bitmap.Width, _bitmap.Height);
				if (RefreshBitmap(bitmapSize, out var bitmap))
				{
					Bitmap = bitmap;
				}
			}

			//var anyDrawnOnLastRow = false;

			var sectionsDisposed = new List<MapSection>();

			foreach (var mapSection in _mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

					if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, "ReDraw", warnOnFail: true))
					{
						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);

							//if (invertedBlockPos.Y == 0) anyDrawnOnLastRow = true;

							var loc = invertedBlockPos.Scale(_blockSize);

							LoadPixelArray(mapSection.MapSectionVectors, _colorMap, !mapSection.IsInverted);

							try
							{
								Bitmap.WritePixels(_blockRect, mapSection.MapSectionVectors.BackBuffer, _blockRect.Width * BYTES_PER_PIXEL, loc.X, loc.Y);
							}
							catch (Exception e)
							{
								Debug.WriteLine($"ReDrawSections got exception: {e.Message}. JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
							}
						}
					}
					else
					{
						_disposeMapSection(mapSection);
						sectionsDisposed.Add(mapSection);
					}
				}
			}

			//if (_mapSections.Count > 0 && !anyDrawnOnLastRow)
			//	Debug.WriteLine($"No blocks were drawn on the last row for ReDraw:{_mapSections.FirstOrDefault()?.JobNumber}.");

			foreach (var ms in sectionsDisposed)
			{
				_mapSections.Remove(ms);
			}

			ReportPercentMapSectionsWithUpdatedScrPos();

			return sectionsDisposed.Count;
		}

		public bool GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors)
		{
			var result = Bitmap.Dispatcher.Invoke(DrawSectionOnUiThread, new object[] { mapSection, mapSectionVectors });

			return (bool)result;
		}

		private bool DrawSectionOnUiThread(MapSection mapSection, MapSectionVectors mapSectionVectors)
		{
			var wasAdded = false;
			var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

			if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, "GetAndPlacePixels"))
			{
				_mapSections.Add(mapSection);
				wasAdded = true;

				if (_colorMap != null)
				{
					var invertedBlockPos = GetInvertedBlockPos(blockPosition);
					var loc = invertedBlockPos.Scale(_blockSize);

					LoadPixelArray(mapSectionVectors, _colorMap, !mapSection.IsInverted);

					try
					{
						Bitmap.WritePixels(_blockRect, mapSectionVectors.BackBuffer, _blockRect.Width * BYTES_PER_PIXEL, loc.X, loc.Y);
					}
					catch (Exception e)
					{
						Debug.WriteLine($"GetAndPlacePixels got exception: {e.Message}. JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
					}
				}
			}
			else
			{
				Debug.WriteLine($"GetAndPlacePixels is not drawing MapSection: {mapSection.ToString(blockPosition)}, it's off the map.");
				_disposeMapSection(mapSection);
			}

			return wasAdded;
		}

		#endregion

		#region Private Methods

		private PointInt GetAdjustedBlockPositon(MapSection mapSection, BigVector mapBlockOffset)
		{
			PointInt result;

			var df = mapSection.JobMapBlockOffset.Diff(mapBlockOffset);

			if (df.IsZero())
			{
				result = mapSection.ScreenPosition;
			}
			else
			{
				if (!df.TryConvertToInt(out var offset))
				{
					throw new ArgumentException("Cannot convert the result of subtracting the current MapBlockOffset from this MapSection's BlockOffset.");
				}

				result = mapSection.ScreenPosition.Translate(offset);

				// Update the mapSection's JobMapBlockOffset and ScreenPosition to avoid this transalation again.
				mapSection.UpdateJobMapBlockOffsetAndPos(mapBlockOffset, result);
			}

			return result;
		}

		private bool IsBLockVisible(MapSection mapSection, PointInt blockPosition, SizeInt imageSizeInBlocks, string desc, bool warnOnFail = false)
		{
			if (blockPosition.X < 0 || blockPosition.Y < 0)
			{
				if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition} is negative.");
				return false;
			}

			CheckBitmapSize(Bitmap, imageSizeInBlocks, desc);

			if (blockPosition.X >= imageSizeInBlocks.Width || blockPosition.Y >= imageSizeInBlocks.Height)
			{
				if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {imageSizeInBlocks}.");
				return false;
			}

			return true;
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			//var maxYPtr = MapExtentInBlocks.Height - 1;
			var maxYPtr = ImageSizeInBlocks.Height - 1;
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

		private bool RefreshBitmap(SizeDbl bitmapSize, [NotNullWhen(true)] out WriteableBitmap? bitmap)
		{
			var imageSize = ImageSizeInBlocks.Scale(_blockSize);

			if (bitmapSize.Width != imageSize.Width || bitmapSize.Height != imageSize.Height)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"BitmapGrid RefreshBitmap is being called. BitmapSize {bitmapSize} != ImageSize: Creating new bitmap with size: {imageSize}.");

				bitmap = CreateBitmap(ImageSizeInBlocks);
				return true;
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"BitmapGrid RefreshBitmap is being called. BitmapSize {bitmapSize} = ImageSize. Not creating a new bitmap.");
				bitmap = null;
				return false;
			}
		}

		[Conditional("DEBUG2")]
		private void CheckBitmapSize(WriteableBitmap bitmap, SizeInt imageSizeInBlocks, string desc)
		{
			var imageSize = ImageSizeInBlocks.Scale(_blockSize);
			var bitmapSize = new SizeInt(bitmap.PixelWidth, bitmap.PixelHeight);
				
			if (bitmapSize != imageSize)
			{
				Debug.WriteLine($"ImageSizeInBlocks != Bitmap Size. On {desc}.");
			}
		}

		private void ClearBitmap(WriteableBitmap bitmap)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"BitmapGrid ClearBitmap is being called. BitmapSize {ImageSizeInBlocks}.");

			// Clear the bitmap, one row of bitmap blocks at a time.
			var rect = new Int32Rect(0, 0, bitmap.PixelWidth, _blockSize.Height);
			var blockRowPixelCount = bitmap.PixelWidth * _blockSize.Height;
			var zeros = GetClearBytes(blockRowPixelCount * BYTES_PER_PIXEL);

			for (var vPtr = 0; vPtr < ImageSizeInBlocks.Height; vPtr++)
			{
				var offset = vPtr * _blockSize.Height;
				bitmap.WritePixels(rect, zeros, rect.Width * BYTES_PER_PIXEL, 0, offset);
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
				var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
				//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

				return result;
			} 
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception: {e} while attempting to create the bitmap. Creating place holder bitmap with size = 10.");

				var result = new WriteableBitmap(10, 10, 96, 96, PixelFormats.Pbgra32, null);
				return result;
			}
		}

		#endregion

		#region Pixel Array Support

		private void LoadPixelArray(MapSectionVectors mapSectionVectors, ColorMap colorMap, bool invert)
		{
			Debug.Assert(mapSectionVectors.ReferenceCount > 0, "Getting the Pixel Array from a MapSectionVectors whose RefCount is < 1.");

			// Currently EscapeVelocities are not supported.
			//var useEscapeVelocities = colorMap.UseEscapeVelocities;
			var useEscapeVelocities = false;

			var rowCount = mapSectionVectors.BlockSize.Height;
			var sourceStride = mapSectionVectors.BlockSize.Width;
			var maxRowIndex = mapSectionVectors.BlockSize.Height - 1;

			var pixelStride = sourceStride * BYTES_PER_PIXEL;

			var backBuffer = mapSectionVectors.BackBuffer;

			var counts = mapSectionVectors.Counts;
			var previousCountVal = counts[0];

			var resultRowPtr = invert ? maxRowIndex * pixelStride : 0;
			var resultRowPtrIncrement = invert ? -1 * pixelStride : pixelStride;
			var sourcePtrUpperBound = rowCount * sourceStride;

			if (useEscapeVelocities)
			{
				var escapeVelocities = new ushort[counts.Length]; // mapSectionValues.EscapeVelocities;
				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var diagSum = 0;

					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < sourceStride; colPtr++)
					{
						var countVal = counts[sourcePtr];
						TrackValueSwitches(countVal, ref previousCountVal);

						var escapeVelocity = escapeVelocities[sourcePtr] / VALUE_FACTOR;
						CheckEscapeVelocity(escapeVelocity);

						colorMap.PlaceColor(countVal, escapeVelocity, new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL));

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;

						diagSum += countVal;
					}

					if (diagSum < 10)
					{
						Debug.WriteLine("WARINING: Counts are empty.");
					}
				}
			}
			else
			{
				// The main for loop on GetPixel Array 
				// is for each row of pixels (0 -> 128)
				//		for each pixel in that row (0, -> 128)
				// each new row advanced the resultRowPtr to the pixel byte address at column 0 of the current row.
				// if inverted, the first row = 127 * # of bytes / Row (Pixel stride)

				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < sourceStride; colPtr++)
					{
						var countVal = counts[sourcePtr];
						TrackValueSwitches(countVal, ref previousCountVal);

						colorMap.PlaceColor(countVal, escapeVelocity: 0, new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL));

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}
		}

		[Conditional("DEBUG2")]
		private void ReportPercentMapSectionsWithUpdatedScrPos()
		{
			var numberOfMapSectionsWithUpdatedScrPos = _mapSections.Count(x => x.ScreenPosHasBeenUpdated);
			var percentWithUpdatedScrPos = 100 * (numberOfMapSectionsWithUpdatedScrPos / (double)_mapSections.Count);
			Debug.WriteLine($"{percentWithUpdatedScrPos:F3} MapSections have updated Screen Positions.");
		}

		[Conditional("DEBUG2")]
		private void TrackValueSwitches(ushort countVal, ref ushort previousCountVal)
		{
			if (countVal != previousCountVal)
			{
				NumberOfCountValSwitches++;
				previousCountVal = countVal;
			}
		}

		[Conditional("DEBUG2")]
		private void CheckEscapeVelocity(double escapeVelocity)
		{
			if (escapeVelocity > 1.0)
			{
				Debug.WriteLine($"WARNING: The Escape Velocity is greater than 1.0");
			}
		}

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

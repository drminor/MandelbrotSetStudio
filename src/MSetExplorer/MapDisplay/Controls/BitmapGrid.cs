using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class BitmapGrid : IDisposable, IBitmapGrid
	{
		#region Private Properties

		private readonly bool DEBUG = true;

		private Int32Rect _blockRect { get; init; }

		private SizeDbl _viewPortSize;
		//private VectorDbl _imageOffset;
		private SizeInt _canvasSizeInBlocks;

		//private int _maxYPtr;
		//private BigVector _mapBlockOffset;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;


		private Image _image;
		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear;

		#endregion

		#region Constructor

		public BitmapGrid(Image image, SizeDbl viewPortSize)
		{
			_image = image;
			BlockSize = RMapConstants.BLOCK_SIZE;
			DisposeMapSection = null;

			_viewPortSize = viewPortSize;

			var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(_viewPortSize, BlockSize, keepSquare: false);
			_canvasSizeInBlocks = sizeInWholeBlocks;

			ImageSizeInBlocks = _canvasSizeInBlocks.Inflate(2);

			_bitmap = CreateBitmap(ImageSizeInBlocks);
			_image.Source = Bitmap;


			_pixelsToClear = new byte[0];
			_blockRect = new Int32Rect(0, 0, BlockSize.Width, BlockSize.Height);

			//_mapBlockOffset = new BigVector();
			MapBlockOffset = new BigVector();

			_colorBandSet = new ColorBandSet();
			_useEscapeVelocities = true;
			_highlightSelectedColorBand = false;
			_colorMap = null;

			MapSections = new ObservableCollection<MapSection>();
		}

		#endregion

		#region Public Properties

		public Dispatcher Dispatcher => _bitmap.Dispatcher;

		public Image Image
		{
			get => _image;
			set
			{
				_image = value;
				_image.Source = Bitmap;
			}
		}

		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				_image.Source = Bitmap;

				
			}
		}

		public ObservableCollection<MapSection> MapSections { get; set; }

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					Debug.WriteLineIf(DEBUG, $"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");
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
					Debug.WriteLineIf(DEBUG, $"The MapDisplay is turning {strState} the use of EscapeVelocities.");
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
					Debug.WriteLineIf(DEBUG, $"The MapDisplay is turning {strState} the Highlighting the selected ColorBand.");
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

		public SizeDbl ViewPortSize
		{
			get => _viewPortSize;
			set
			{
				_viewPortSize = value;

				var sizeInWholeBlocks = RMapHelper.GetCanvasSizeInWholeBlocks(value, BlockSize, keepSquare: false);
				CanvasSizeInBlocks = sizeInWholeBlocks;

				//var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(value.Round(), ImageOffset.Round(), _blockSize);

				//Debug.WriteLineIf(DEBUG, $"BitmapGrid Updating the MapExtentInBlocks from: {MapExtentInBlocks} to: {mapExtentInBlocks}.");
				//MapExtentInBlocks = mapExtentInBlocks;
			}
		}

		public VectorDbl ImageOffset { get; set; }

		public SizeInt CanvasSizeInBlocks
		{
			get => _canvasSizeInBlocks;
			set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (_canvasSizeInBlocks != value)
				{
					_canvasSizeInBlocks = value;

					// The Image must be able to accommodate one block before and one block after the set of visible blocks.
					var newImageSizeInBlocks = value.Inflate(2);

					Debug.WriteLineIf(DEBUG, $"BitmapGrid Updating the ImageSizeInBlocks from: {ImageSizeInBlocks} to: {newImageSizeInBlocks}.");
					ImageSizeInBlocks = newImageSizeInBlocks;

					//if (!_registered)
					//{
					//	RefreshBitmap();
					//	_registered = true;
					//}


				}
			}
		}

		private SizeInt ImageSizeInBlocks { get; set; }

		public SizeInt BlockSize { get; set; }

		public Action<MapSection>? DisposeMapSection { get; set; }

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public void ClearDisplay()
		{
			if (!RefreshBitmap())
			{
				ClearBitmap(_bitmap);
			}

			if (DisposeMapSection != null)
			{
				foreach (var ms in MapSections)
				{
					DisposeMapSection(ms);
				}
			}

			MapSections.Clear();
		}

		public bool DrawSections(IList<MapSection> mapSections)
		{
			var lastSectionWasIncluded = false;
			foreach (var mapSection in mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

					if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, "Draw", warnOnFail: true))
					{
						MapSections.Add(mapSection);

						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);
							var loc = invertedBlockPos.Scale(BlockSize);

							LoadPixelArray(mapSection.MapSectionVectors, _colorMap, !mapSection.IsInverted);

							try
							{
								_bitmap.WritePixels(_blockRect, mapSection.MapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);
							}
							catch (Exception e)
							{
								Debug.WriteLine($"DrawSections got exception: {e.Message}. JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
							}
						}
					}
					else
					{
						if (DisposeMapSection != null) DisposeMapSection(mapSection);
					}

					if (mapSection.IsLastSection)
					{
						lastSectionWasIncluded = true;
					}
				}
			}

			return lastSectionWasIncluded;
		}

		public int ReDrawSections()
		{
			if (_colorMap != null)
			{
				RefreshBitmap();
			}

			var sectionsDisposed = new List<MapSection>();

			foreach (var mapSection in MapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

					if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, "ReDraw", warnOnFail: true))
					{
						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);
							var loc = invertedBlockPos.Scale(BlockSize);

							LoadPixelArray(mapSection.MapSectionVectors, _colorMap, !mapSection.IsInverted);

							try
							{
								_bitmap.WritePixels(_blockRect, mapSection.MapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);
							}
							catch (Exception e)
							{
								Debug.WriteLine($"ReDrawSections got exception: {e.Message}. JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
							}
						}
					}
					else
					{
						if (DisposeMapSection != null)
						{
							DisposeMapSection(mapSection);
							sectionsDisposed.Add(mapSection);
						}
					}
				}
			}

			foreach (var ms in sectionsDisposed)
			{
				MapSections.Remove(ms);
			}

			return sectionsDisposed.Count;
		}

		public void GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors)
		{
			_bitmap.Dispatcher.Invoke(DrawSectionOnUiThread, new object[] { mapSection, mapSectionVectors });
		}

		private void DrawSectionOnUiThread(MapSection mapSection, MapSectionVectors mapSectionVectors)
		{
			var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

			if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, "GetAndPlacePixels"))
			{
				MapSections.Add(mapSection);

				if (_colorMap != null)
				{
					var invertedBlockPos = GetInvertedBlockPos(blockPosition);
					var loc = invertedBlockPos.Scale(BlockSize);

					LoadPixelArray(mapSectionVectors, _colorMap, !mapSection.IsInverted);

					try
					{
						_bitmap.WritePixels(_blockRect, mapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);
					}
					catch (Exception e)
					{
						Debug.WriteLine($"GetAndPlacePixels got exception: {e.Message}. JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {ImageSizeInBlocks}.");
					}
				}
			}
			else
			{
				if (DisposeMapSection != null)
				{
					Debug.WriteLineIf(DEBUG, $"Not drawing MapSection: {mapSection.ToString(blockPosition)}, it's off the map.");
					DisposeMapSection(mapSection);
				}
				else
				{
					Debug.WriteLineIf(DEBUG, $"Not drawing MapSection: {mapSection.ToString(blockPosition)}, it's off the map. The value of the DisposeMapSection (Action<MapSection>) is null.");

				}
			}
		}

		//public void SetColorBandSet(ColorBandSet value)
		//{
		//	//if (value != _colorBandSet)
		//	//{
		//	//	_colorBandSet = value;
		//	//	_colorMap = LoadColorMap(value);

		//	//	if (_colorMap != null)
		//	//	{
		//	//		ReDrawSections();
		//	//	}
		//	//}

		//	ColorBandSet = value;
		//}

		//public void SetCurrentColorBand(ColorBand colorBand)
		//{
		//	_colorBandSet.SelectedColorBand = colorBand;

		//	if (HighlightSelectedColorBand && _colorMap != null)
		//	{
		//		ReDrawSections();
		//	}
		//}

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

			CheckBitmapSize(_bitmap, imageSizeInBlocks, desc);

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

		private bool RefreshBitmap()
		{
			var imageSize = ImageSizeInBlocks.Scale(BlockSize);

			if (_bitmap.Width != imageSize.Width || _bitmap.Height != imageSize.Height)
			{
				Debug.WriteLineIf(DEBUG, $"BitmapGrid RefreshBitmap is being called. BitmapSize {new Size(_bitmap.Width, _bitmap.Height)} != ImageSize: Creating new bitmap with size: {ImageSizeInBlocks}.");

				//_maxYPtr = ImageSizeInBlocks.Height - 1;
				Bitmap = CreateBitmap(imageSize);
				return true;
			}
			else
			{
				Debug.WriteLineIf(DEBUG, $"BitmapGrid RefreshBitmap is being called. BitmapSize {new Size(_bitmap.Width, _bitmap.Height)} does = ImageSize.");
				return false;
			}
		}

		[Conditional("DEBUG")]
		private void CheckBitmapSize(WriteableBitmap bitmap, SizeInt imageSizeInBlocks, string desc)
		{
			var imageSize = ImageSizeInBlocks.Scale(BlockSize);
			var bitmapSize = new SizeInt(bitmap.PixelWidth, bitmap.PixelHeight);
				
			if (bitmapSize != imageSize)
			{
				Debug.WriteLine($"ImageSizeInBlocks != Bitmap Size. On {desc}.");
			}
		}

		private void ClearBitmap(WriteableBitmap bitmap)
		{
			Debug.WriteLineIf(DEBUG, $"BitmapGrid ClearBitmap is being called. BitmapSize {ImageSizeInBlocks}.");

			// Clear the bitmap, one row of bitmap blocks at a time.
			var rect = new Int32Rect(0, 0, bitmap.PixelWidth, BlockSize.Height);
			var blockRowPixelCount = bitmap.PixelWidth * BlockSize.Height;
			var zeros = GetClearBytes(blockRowPixelCount * 4);

			for (var vPtr = 0; vPtr < ImageSizeInBlocks.Height; vPtr++)
			{
				var offset = vPtr * BlockSize.Height;
				bitmap.WritePixels(rect, zeros, rect.Width * 4, 0, offset);
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

		private WriteableBitmap CreateBitmap(SizeInt size)
		{
			var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
			//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

			return result;
		}

		#endregion

		#region Pixel Array Support

		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

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

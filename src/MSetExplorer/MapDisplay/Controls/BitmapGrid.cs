﻿using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class BitmapGrid : IDisposable
	{
		#region Private Properties

		private readonly bool DEBUG = true;

		private SizeInt _blockSize;
		private Int32Rect _blockRect { get; init; }

		private SizeInt _canvasSizeInBlocks;
		private int _maxYPtr;
		private BigVector _mapBlockOffset;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		private readonly Action<WriteableBitmap> _onUpdateBitmap;
		private readonly Action<MapSection> _diposeMapSection;
		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear;

		#endregion

		#region Constructor

		public BitmapGrid(SizeInt blockSize, Action<WriteableBitmap> onUpdateBitmap, Action<MapSection> disposeMapSection)
		{
			////MapSections = new ObservableCollection<MapSection>();
			MapSections = new MapSectionCollection();

			_blockSize = blockSize;
			_blockRect = new Int32Rect(0, 0, _blockSize.Width, _blockSize.Height);

			_canvasSizeInBlocks = new SizeInt();
			_maxYPtr = 1;
			_mapBlockOffset = new BigVector();

			_colorBandSet = new ColorBandSet();
			_colorMap = null;
			_useEscapeVelocities = true;
			_highlightSelectedColorBand = false;

			_onUpdateBitmap = onUpdateBitmap;
			_diposeMapSection = disposeMapSection;
			_bitmap = CreateBitmap(size: _blockSize);
			_pixelsToClear = new byte[0];
		}

		#endregion

		#region Public Properties

		public MapSectionCollection MapSections { get; init; }

		public ColorBandSet ColorBandSet
		{
			get =>  _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					Debug.WriteLineIf(DEBUG, $"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");
					_colorBandSet = value;
					_colorMap = LoadColorMap(value);

					if (_colorMap != null)
					{
						RefreshBitmap();
						DrawSections(MapSections);
					}
				}

			}
		}

		public void SetColorBandSet(ColorBandSet value)
		{
			if (value != _colorBandSet)
			{
				_colorBandSet = value;
				_colorMap = LoadColorMap(value);
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
						RefreshBitmap();
						DrawSections(MapSections);
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
						RefreshBitmap();
						DrawSections(MapSections);
					}
				}
			}
		}

		public BigVector MapBlockOffset
		{
			get => _mapBlockOffset;
			set
			{
				if (value != _mapBlockOffset)
				{
					_mapBlockOffset = value;
				}
			}
		}

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
					Debug.WriteLineIf(DEBUG, $"BitmapGrid CanvasSizeUpdate: Old size: {_canvasSizeInBlocks}, new size: {value}.");

					_canvasSizeInBlocks = value;

					// The Image must be able to accommodate a postive or negative CanvasControlOffset of up to one full block. 
					ImageSizeInBlocks = value.Inflate(2);
				}
			}
		}

		private SizeInt ImageSizeInBlocks { get; set; }

		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				_onUpdateBitmap(_bitmap);
			}
		}

		public Dispatcher Dispatcher => _bitmap.Dispatcher;

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods
		
		public void ClearDisplay()
		{
			if (!RefreshBitmap())
			{
				ClearBitmap(_bitmap);
			}

			foreach (var ms in MapSections)
			{
				_diposeMapSection(ms);
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

					if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, warnOnFail: true))
					{
						MapSections.Add(mapSection);

						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);
							var loc = invertedBlockPos.Scale(_blockSize);

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
						_diposeMapSection(mapSection);
					}

					if (mapSection.IsLastSection)
					{
						lastSectionWasIncluded = true;
					}
				}
			}

			return lastSectionWasIncluded;
		}

		public List<MapSection> ReDrawSections()
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

					if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks, warnOnFail: true))
					{
						if (_colorMap != null)
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition);
							var loc = invertedBlockPos.Scale(_blockSize);

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
						sectionsDisposed.Add(mapSection);
						_diposeMapSection(mapSection);
					}
				}
			}

			return sectionsDisposed;
		}

		public bool GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors)
		{
			var blockPosition = GetAdjustedBlockPositon(mapSection, MapBlockOffset);

			bool sectionWasAdded;

			if (IsBLockVisible(mapSection, blockPosition, ImageSizeInBlocks))
			{
				MapSections.Add(mapSection);
				sectionWasAdded = true;

				if (_colorMap != null)
				{
					var invertedBlockPos = GetInvertedBlockPos(blockPosition);
					var loc = invertedBlockPos.Scale(_blockSize);

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
				_diposeMapSection(mapSection);
				sectionWasAdded = false;
				Debug.WriteLineIf(DEBUG, $"Not drawing MapSection: {mapSection.ToString(blockPosition)}, it's off the map.");
			}

			return sectionWasAdded;
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
			}

			return result;
		}

		private bool IsBLockVisible(MapSection mapSection, PointInt blockPosition, SizeInt imageSizeInBlocks, bool warnOnFail = false)
		{
			if (blockPosition.X < 0 || blockPosition.Y < 0)
			{
				if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition} is negative.");
				return false;
			}

			if (blockPosition.X > imageSizeInBlocks.Width || blockPosition.Y > imageSizeInBlocks.Height)
			{
				if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, ImageSize: {imageSizeInBlocks}.");
				return false;
			}

			return true;
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

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
			var imageSize = ImageSizeInBlocks.Scale(_blockSize);

			if (_bitmap.Width != imageSize.Width || _bitmap.Height != imageSize.Height)
			{
				Debug.WriteLineIf(DEBUG, $"BitmapGrid RefreshBitmap is being called. BitmapSize {new Size(_bitmap.Width, _bitmap.Height)} != ImageSize: Creating new bitmap with size: {ImageSizeInBlocks}.");

				_maxYPtr = ImageSizeInBlocks.Height - 1;
				Bitmap = CreateBitmap(imageSize);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void ClearBitmap(WriteableBitmap bitmap)
		{
			Debug.WriteLineIf(DEBUG, $"BitmapGrid ClearBitmap is being called. BitmapSize {ImageSizeInBlocks}.");

			// Clear the bitmap, one row of bitmap blocks at a time.
			var rect = new Int32Rect(0, 0, bitmap.PixelWidth, _blockSize.Height);
			var blockRowPixelCount = bitmap.PixelWidth * _blockSize.Height;
			var zeros = GetClearBytes(blockRowPixelCount * 4);

			for (var vPtr = 0; vPtr < ImageSizeInBlocks.Height; vPtr++)
			{
				var offset = vPtr * _blockSize.Height;
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

		public void LoadPixelArray(MapSectionVectors mapSectionVectors, ColorMap colorMap, bool invert)
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

		#region Old Draw Sections Code

		// TODO: Decompose the DrawSections method into a set of methods that can be called individually

		//public bool ReuseAndLoad(IList<MapSection> existingMapSections, List<MapSection> newMapSections, ColorBandSet colorBandSet)
		//{
		//	if (colorBandSet != ColorBandSet)
		//	{
		//		_colorMap = LoadColorMap(colorBandSet);
		//	}

		//	bool lastSectionWasIncluded;

		//	if (_colorMap != null)
		//	{
		//		ClearBitmap(_bitmap);
		//		DrawSections(existingMapSections, _colorMap);

		//		lastSectionWasIncluded = DrawSections(newMapSections, _colorMap);
		//	}
		//	else
		//	{
		//		lastSectionWasIncluded = false;
		//	}

		//	return lastSectionWasIncluded;
		//}

		//public void Redraw(IList<MapSection> existingMapSections, ColorBandSet colorBandSet)
		//{
		//	if (colorBandSet != ColorBandSet)
		//	{
		//		_colorMap = LoadColorMap(colorBandSet);
		//	}

		//	if (_colorMap != null)
		//	{
		//		ClearBitmap(_bitmap);
		//		DrawSections(existingMapSections, _colorMap);
		//	}
		//}

		//public bool DiscardAndLoad(List<MapSection> mapSections, ColorBandSet colorBandSet)
		//{
		//	if (colorBandSet != ColorBandSet)
		//	{
		//		_colorMap = LoadColorMap(colorBandSet);
		//	}

		//	bool lastSectionWasIncluded;

		//	if (_colorMap != null)
		//	{
		//		lastSectionWasIncluded = DrawSections(mapSections, _colorMap);
		//	}
		//	else
		//	{
		//		lastSectionWasIncluded = false;
		//	}

		//	return lastSectionWasIncluded;
		//}

		//private void RedrawSections(IList<MapSection> mapSections, ColorMap colorMap, BigVector jobMapBlockOffset)
		//{
		//	// The jobMapBlockOffset reflects the current content on the screen and will not change during the lifetime of this method.
		//	foreach (var mapSection in mapSections)
		//	{
		//		if (mapSection.MapSectionVectors != null)
		//		{
		//			if (TryGetAdjustedBlockPositon(mapSection, jobMapBlockOffset, out var blockPosition))
		//			{
		//				if (IsBLockVisible(mapSection, blockPosition.Value, CanvasSizeInBlocks))
		//				{
		//					var invertedBlockPos = GetInvertedBlockPos(blockPosition.Value);
		//					var loc = invertedBlockPos.Scale(_blockSize);

		//					LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
		//					_bitmap.WritePixels(_blockRect, mapSection.MapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);
		//				}
		//			}
		//			else
		//			{
		//				Debug.WriteLine($"Not drawing, the MapSectionVectors are empty.");
		//			}
		//		}
		//	}
		//}


		#endregion
	}
}

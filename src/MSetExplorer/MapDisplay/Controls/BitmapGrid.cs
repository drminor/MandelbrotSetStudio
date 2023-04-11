using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class BitmapGrid : IDisposable
	{
		#region Private Properties

		private SizeInt _blockSize;

		private readonly Action<WriteableBitmap> _onUpdateBitmap;
		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear = new byte[0];
		private Int32Rect _blockRect { get; init; }

		private SizeInt _canvasSizeInBlocks;
		private SizeInt _imageSizeInBlocks;
		private int _maxYPtr;

		private BigVector _mapBlockOffset;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		#endregion

		#region Constructor

		public BitmapGrid(SizeInt blockSize, Action<WriteableBitmap> onUpdateBitmap)
		{
			_blockSize = blockSize;
			_blockRect = new Int32Rect(0, 0, _blockSize.Width, _blockSize.Height);

			_onUpdateBitmap = onUpdateBitmap;

			_bitmap = CreateBitmap(size: _blockSize);
			_maxYPtr = 1;

			_mapBlockOffset = new BigVector();

			_useEscapeVelocities = true;
			_highlightSelectedColorBand = false;
			_colorBandSet = new ColorBandSet();
			_colorMap = null;
		}

		#endregion

		#region Public Properties

		public ColorBandSet ColorBandSet => _colorBandSet;

		public void SetColorBandSet(ColorBandSet value, IList<MapSection> mapSections)
		{
			if (value != _colorBandSet)
			{
				Debug.WriteLine($"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");
				_colorMap = LoadColorMap(value);

				if (_colorMap != null)
				{
					DrawSections(mapSections, _colorMap);
				}
			}
		}

		public bool UseEscapeVelocities => _useEscapeVelocities;

		public void SetUseEscapeVelocities(bool value, IList<MapSection> mapSections)
		{
			if (value != _useEscapeVelocities)
			{
				var strState = value ? "On" : "Off";
				Debug.WriteLine($"The MapDisplay is turning {strState} the use of EscapeVelocities.");
				_useEscapeVelocities = value;

				if (_colorMap != null)
				{
					_colorMap.UseEscapeVelocities = value;
					DrawSections(mapSections, _colorMap);
				}
			}
		}

		public bool HighlightSelectedColorBand => _highlightSelectedColorBand;

		public void SetHighlightSelectedColorBand(bool value, IList<MapSection> mapSections)
		{
			if (value != _highlightSelectedColorBand)
			{
				var strState = value ? "On" : "Off";
				Debug.WriteLine($"The MapDisplay is turning {strState} the Highlighting the selected ColorBand.");
				_highlightSelectedColorBand = value;

				if (_colorMap != null)
				{
					_colorMap.HighlightSelectedColorBand = value;
					DrawSections(mapSections, _colorMap);
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
					Debug.WriteLine($"BitmapGrid CanvasSizeUpdate: Old size: {_canvasSizeInBlocks}, new size: {value}.");

					_canvasSizeInBlocks = value;

					// The Image must be able to accomodate a postive or negative CanvasControlOffset of up to one full block. 
					ImageSizeInBlocks = value.Inflate(2);
				}
			}
		}

		public SizeInt ImageSizeInBlocks
		{
			get  => _imageSizeInBlocks;

			private set
			{
				if (_imageSizeInBlocks != value)
				{
					Debug.WriteLine($"BitmapGrid ImageSizeUpdate Writeable Bitmap. Old size: {_imageSizeInBlocks}, new size: {value}.");

					_imageSizeInBlocks = value;
					//_maxYPtr = _imageSizeInBlocks.Height - 1;
					//Bitmap = CreateBitmap(_imageSizeInBlocks);
				}
			}
		}

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

		public bool DrawSections(IList<MapSection> existingMapSections, List<MapSection> newMapSections, ColorBandSet colorBandSet)
		{
			if (colorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(colorBandSet);
			}

			bool lastSectionWasIncluded = false;

			if (_colorMap != null)
			{
				if (existingMapSections.Count > 0)
				{
					ClearBitmap(_bitmap);
					DrawSections(existingMapSections, _colorMap);
				}

				if (newMapSections.Count > 0)
				{
					lastSectionWasIncluded = DrawSections(newMapSections, _colorMap);
				}
			}

			return lastSectionWasIncluded;
		}

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

		public bool GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors, out PointInt? blockPosition)
		{
			var sectionWasAdded = false;

			if (TryGetAdjustedBlockPositon(mapSection, MapBlockOffset, out blockPosition))
			{
				if (IsBLockVisible(mapSection, blockPosition.Value, CanvasSizeInBlocks))
				{
					sectionWasAdded = true;

					if (_colorMap != null)
					{
						var invertedBlockPos = GetInvertedBlockPos(blockPosition.Value);
						var loc = invertedBlockPos.Scale(_blockSize);

						LoadPixelArray(mapSectionVectors, _colorMap, !mapSection.IsInverted);
						_bitmap.WritePixels(_blockRect, mapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);
					}
				}
			}

			return sectionWasAdded;
		}

		public void ClearDisplay()
		{
			ClearBitmap(_bitmap);
		}

		#endregion

		#region Private Methods

		private bool DrawSections(IList<MapSection> mapSections, ColorMap colorMap)
		{
			var lastSectionWasIncluded = false;

			foreach (var mapSection in mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					if (TryGetAdjustedBlockPositon(mapSection, MapBlockOffset, out var blockPosition, warnOnFail: true))
					{
						if (IsBLockVisible(mapSection, blockPosition.Value, CanvasSizeInBlocks, warnOnFail: true))
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition.Value);
							var loc = invertedBlockPos.Scale(_blockSize);

							LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
							_bitmap.WritePixels(_blockRect, mapSection.MapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);

						}
					}

					if (mapSection.IsLastSection)
					{
						lastSectionWasIncluded = true;
					}
				}
			}

			return lastSectionWasIncluded;
		}

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

		private bool TryGetAdjustedBlockPositon(MapSection mapSection, BigVector mapBlockOffset, [NotNullWhen(true)] out PointInt? blockPosition, bool warnOnFail = false)
		{
			blockPosition = null;
			var result = false;

			var df = mapSection.JobMapBlockOffset.Diff(mapBlockOffset);

			if (df.IsZero())
			{
				blockPosition = mapSection.ScreenPosition;
				result = true;
			}
			else
			{
				if (df.TryConvertToInt(out var offset))
				{
					blockPosition = mapSection.ScreenPosition.Translate(offset);
					result = true;
				}
			}

			if (!result && warnOnFail)
			{
				Debug.WriteLine($"WARNING: Could not GetAdjustedBlockPosition for MapSection with JobNumber: {mapSection.JobNumber}.");
			}

			return result;
		}

		private bool IsBLockVisible(MapSection mapSection, PointInt blockPosition, SizeInt canvasSizeInBlocks, bool warnOnFail = false)
		{
			if (blockPosition.X < 0 || blockPosition.Y < 0)
			{
				//if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition} is negative.");
				return false;
			}

			// TODO: Should we subtract 1 BlockSize from the width when checking the Bounds in IsBlockVisible method.
			if (blockPosition.X > canvasSizeInBlocks.Width || blockPosition.Y > canvasSizeInBlocks.Height)
			{
				//if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, CanvasSize: {canvasSizeInBlocks}.");
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

		private void ClearBitmap(WriteableBitmap bitmap)
		{
			var imageSize = _imageSizeInBlocks.Scale(_blockSize);

			if (_bitmap.Width != imageSize.Width || _bitmap.Height != imageSize.Height)
			{
				Debug.WriteLine($"BitmapGrid ClearBitmap is being called Creating new bitmap with size: {imageSize}.");

				_maxYPtr = _imageSizeInBlocks.Height - 1;
				Bitmap = CreateBitmap(imageSize);
			}
			else
			{
				Debug.WriteLine($"BitmapGrid ClearBitmap is being called.");

				// Clear the bitmap, one row of bitmap blocks at a time.

				var rect = new Int32Rect(0, 0, bitmap.PixelWidth, _blockSize.Height);
				var blockRowPixelCount = bitmap.PixelWidth * _blockSize.Height;
				var zeros = GetClearBytes(blockRowPixelCount * 4);

				for (var vPtr = 0; vPtr < _imageSizeInBlocks.Height; vPtr++)
				{
					var offset = vPtr * _blockSize.Height;
					bitmap.WritePixels(rect, zeros, rect.Width * 4, 0, offset);
				}
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
			//var size = sizeInBlocks.Scale(BlockSize);
			var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
			//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

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
			var loc = invertedBlockPos.Scale(_blockSize);

			//_mapSectionHelper.FillBackBuffer(bitmap.BackBuffer, bitmap.BackBufferStride, loc, BlockSize, mapSectionVectors, colorMap, !isInverted, useEscapeVelocities);

			bitmap.Lock();
			bitmap.AddDirtyRect(new Int32Rect(loc.X, loc.Y, _blockSize.Width, _blockSize.Height));
			bitmap.Unlock();

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

			//_pixelArraySize = _blockSize.NumberOfCells * BYTES_PER_PIXEL;
			var pixelStride = sourceStride * BYTES_PER_PIXEL;

			//var invert = !mapSection.IsInverted;
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
						Debug.WriteLine("Counts are empty.");
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
				Debug.WriteLine($"The Escape Velocity is greater than 1.0");
			}
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

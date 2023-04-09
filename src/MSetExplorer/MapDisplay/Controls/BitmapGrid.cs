using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MSetExplorer
{
	public class BitmapGrid : IDisposable
	{
		#region Private Properties

		private readonly MapSectionHelper _mapSectionHelper;

		private WriteableBitmap _bitmap;
		private byte[] _pixelsToClear = new byte[0];
		private Int32Rect _blockRect { get; init; }

		private int? _currentMapLoaderJobNumber;

		private SizeInt _canvasSizeInBlocks;
		private SizeInt _imageSizeInBlocks;
		private int _maxYPtr;

		private VectorInt _canvasControlOffset;
		private BigVector _mapBlockOffset;

		private ColorBandSet _colorBandSet;
		private ColorMap? _colorMap;
		private bool _useEscapeVelocities;
		private bool _highlightSelectedColorBand;

		#endregion

		#region Constructor

		public BitmapGrid(MapSectionHelper mapSectionHelper, SizeInt blockSize)
		{
			_mapSectionHelper = mapSectionHelper;
			BlockSize = blockSize;
			_blockRect = new Int32Rect(0, 0, BlockSize.Width, BlockSize.Height);

			//JobMapOffsets = new Dictionary<int, BigVector>();
			ActiveJobNumbers = new List<int>();

			_currentMapLoaderJobNumber = null;

			Image = new Image();
			_bitmap = CreateBitmap(sizeInBlocks: new SizeInt(1));
			Image.Source = _bitmap;
			_maxYPtr = 1;

			_mapBlockOffset = new BigVector();

			_canvasControlOffset = new VectorInt();
			SetCanvasOffset(_canvasControlOffset);

			_useEscapeVelocities = true;
			_highlightSelectedColorBand = false;
			_colorBandSet = new ColorBandSet();
			_colorMap = null;
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize { get; init; }

		public ColorBandSet ColorBandSet => _colorBandSet;

		public void SetColorBandSet(ColorBandSet value, IList<MapSection> mapSections)
		{
			if (value != _colorBandSet)
			{
				Debug.WriteLine($"The MapDisplay is processing a new ColorMap. Id = {value.Id}.");
				_colorMap = LoadColorMap(value);

				if (_colorMap != null)
				{
					RedrawSections(mapSections, _colorMap, MapBlockOffset);
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
					RedrawSections(mapSections, _colorMap, MapBlockOffset);
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
					RedrawSections(mapSections, _colorMap, MapBlockOffset);
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
					Debug.WriteLine($"BitmapGrid CanvasSizeUpdate: Old size: {_imageSizeInBlocks}, new size: {value}.");

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
					_maxYPtr = _imageSizeInBlocks.Height - 1;
					Bitmap = CreateBitmap(_imageSizeInBlocks);
				}
			}
		}

		public VectorInt CanvasControlOffset
		{
			get => _canvasControlOffset;
			set
			{
				if (value != _canvasControlOffset)
				{
					Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");
					_canvasControlOffset = value;
					SetCanvasOffset(value);	
				}
			}
		}

		public Image Image { get; init; }

		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				Image.Source = value;
			}
		}

		//public Dictionary<int, BigVector> JobMapOffsets { get; init; }

		public List<int> ActiveJobNumbers { get; init; }

		public int? CurrentMapLoaderJobNumber
		{
			get => _currentMapLoaderJobNumber;
			set
			{
				_currentMapLoaderJobNumber = value;
			}
		}

		public Dispatcher Dispatcher => _bitmap.Dispatcher;

		#endregion

		#region Public Methods

		public bool ReuseAndLoad(IList<MapSection> existingMapSections, List<MapSection> newMapSections, ColorBandSet colorBandSet, int jobNumber, BigVector mapBlockOffset, VectorInt canvasControlOffset)
		{
			AddJobNumAndMapOffset(jobNumber, mapBlockOffset);
			CanvasControlOffset = canvasControlOffset;

			if (colorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(colorBandSet);
			}

			bool lastSectionWasIncluded;

			if (_colorMap != null)
			{
				ClearBitmap(_bitmap);
				RedrawSections(existingMapSections, _colorMap, mapBlockOffset);

				lastSectionWasIncluded = LoadAndDrawNewSections(newMapSections, _colorMap);
			}
			else
			{
				lastSectionWasIncluded = false;
			}

			return lastSectionWasIncluded;
		}

		public void Redraw(IList<MapSection> existingMapSections, ColorBandSet colorBandSet, VectorInt canvasControlOffset)
		{
			CanvasControlOffset = canvasControlOffset;

			if (colorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(colorBandSet);
			}

			if (_colorMap != null)
			{
				ClearBitmap(_bitmap);
				RedrawSections(existingMapSections, _colorMap, MapBlockOffset);
			}
		}

		public bool DiscardAndLoad(List<MapSection> mapSections, ColorBandSet colorBandSet, int jobNumber, BigVector mapBlockOffset, VectorInt canvasControlOffset)
		{
			AddJobNumAndMapOffset(jobNumber, mapBlockOffset);
			CanvasControlOffset = canvasControlOffset;

			if (colorBandSet != ColorBandSet)
			{
				_colorMap = LoadColorMap(colorBandSet);
			}

			bool lastSectionWasIncluded;

			if (_colorMap != null)
			{
				lastSectionWasIncluded = LoadAndDrawNewSections(mapSections, _colorMap);
			}
			else
			{
				lastSectionWasIncluded = false;
			}

			return lastSectionWasIncluded;
		}

		public bool LoadAndDrawNewSections(List<MapSection> mapSections, ColorMap colorMap)
		{
			// All of these mapSections are new and have the same jobMapBlockOffset as the one provided to the method.

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
							var loc = invertedBlockPos.Scale(BlockSize);

							_mapSectionHelper.LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);

							//try
							//{
							//	_bitmap.WritePixels(BlockRect, mapSection.MapSectionVectors.BackBuffer, BlockRect.Width * 4, loc.X, loc.Y);
							//}
							//catch (Exception e)
							//{
							//	Debug.WriteLine($"{e.Message}: Attempting to write a block off-canvas at {loc}. JobNumber: {mapSection.JobNumber}.");
							//	_mapSectionHelper.ReturnMapSection(mapSection);
							//}

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

		public void RedrawSections(IList<MapSection> mapSections, ColorMap colorMap, BigVector jobMapBlockOffset)
		{
			// The jobMapBlockOffset reflects the current content on the screen and will not change during the lifetime of this method.
			foreach (var mapSection in mapSections)
			{
				if (mapSection.MapSectionVectors != null)
				{
					if (TryGetAdjustedBlockPositon(mapSection, jobMapBlockOffset, out var blockPosition))
					{
						if (IsBLockVisible(mapSection, blockPosition.Value, CanvasSizeInBlocks))
						{
							var invertedBlockPos = GetInvertedBlockPos(blockPosition.Value);
							var loc = invertedBlockPos.Scale(BlockSize);

							_mapSectionHelper.LoadPixelArray(mapSection.MapSectionVectors, colorMap, !mapSection.IsInverted);
							_bitmap.WritePixels(_blockRect, mapSection.MapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);
						}
					}
					else
					{
						Debug.WriteLine($"Not drawing, the MapSectionVectors are empty.");
					}
				}
			}
		}

		public bool GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors, out PointInt? blockPosition)
		{
			var sectionWasAdded = false;

			if (TryGetAdjustedBlockPositon(mapSection, MapBlockOffset, out blockPosition))
			{
				if (IsBLockVisible(mapSection, blockPosition.Value, CanvasSizeInBlocks))
				{
					//MapSections.Add(mapSection);

					sectionWasAdded = true;

					if (_colorMap != null)
					{
						var invertedBlockPos = GetInvertedBlockPos(blockPosition.Value);
						var loc = invertedBlockPos.Scale(BlockSize);

						_mapSectionHelper.LoadPixelArray(mapSectionVectors, _colorMap, !mapSection.IsInverted);
						_bitmap.WritePixels(_blockRect, mapSectionVectors.BackBuffer, _blockRect.Width * 4, loc.X, loc.Y);
					}
				}
			}

			if (!sectionWasAdded)
			{
				Debug.WriteLine($"Not drawing map section: {mapSection} with adjusted block position: {blockPosition} for job number = {mapSection.JobNumber}.");
				_mapSectionHelper.ReturnMapSection(mapSection);
			}

			return sectionWasAdded;
		}

		public void ClearDisplay()
		{
			//ClearMapSectionsAndBitmap(mapLoaderJobNumber: null);
			ClearBitmap(_bitmap);
		}

		#endregion

		#region Private Methods

		//private bool TryGetAdjustedBlockPositon(MapSection mapSection, BigVector mapBlockOffset, [NotNullWhen(true)] out PointInt? blockPosition, bool warnOnFail = false)
		//{
		//	blockPosition = null;
		//	var result = false;

		//	if (JobMapOffsets.TryGetValue(mapSection.JobNumber, out var thisSectionsMapBlockOffset))
		//	{
		//		var df = thisSectionsMapBlockOffset.Diff(mapBlockOffset);

		//		if (df.IsZero())
		//		{
		//			blockPosition = mapSection.ScreenPosition;
		//			result = true;
		//		}
		//		else
		//		{
		//			if (df.TryConvertToInt(out var offset))
		//			{
		//				blockPosition = mapSection.ScreenPosition.Translate(offset);
		//				result = true;
		//			}
		//		}
		//	}

		//	if (!result && warnOnFail)
		//	{
		//		Debug.WriteLine($"WARNING: Could not GetAdjustedBlockPosition for MapSection with JobNumber: {mapSection.JobNumber}.");
		//	}

		//	return result;
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
				if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition} is negative.");
				return false;
			}

			// TODO: Should we subtract 1 BlockSize from the width when checking the Bounds in IsBlockVisible method.
			if (blockPosition.X > canvasSizeInBlocks.Width || blockPosition.Y > canvasSizeInBlocks.Height)
			{
				if (warnOnFail) Debug.WriteLine($"WARNING: IsBlockVisible = false for MapSection with JobNumber: {mapSection.JobNumber}. BlockPosition: {blockPosition}, CanvasSize: {canvasSizeInBlocks}.");
				return false;
			}

			return true;
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

			return result;
		}

		private void AddJobNumAndMapOffset(int jobNumber, BigVector jobMapOffset)
		{
			//JobMapOffsets.Add(jobNumber, jobMapOffset);

			ActiveJobNumbers.Add(jobNumber);
			CurrentMapLoaderJobNumber = jobNumber;
			MapBlockOffset = jobMapOffset;
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
			// Clear the bitmap, one row of bitmap blocks at a time.

			var rect = new Int32Rect(0, 0, bitmap.PixelWidth, BlockSize.Height);
			var blockRowPixelCount = bitmap.PixelWidth * BlockSize.Height;
			var zeros = GetClearBytes(blockRowPixelCount * 4);

			for (var vPtr = 0; vPtr < _imageSizeInBlocks.Height; vPtr++)
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

		private WriteableBitmap CreateBitmap(SizeInt sizeInBlocks)
		{
			var size = sizeInBlocks.Scale(BlockSize);
			var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
			//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

			return result;
		}

		private void SetCanvasOffset(VectorInt offset)
		{
			Debug.Assert(offset.X >= 0 && offset.Y >= 0, "Setting offset to negative value.");

			// For a postive offset, we "pull" the image down and to the left.
			var invertedOffset = offset.Invert();

			Image.SetValue(Canvas.LeftProperty, (double)invertedOffset.X);
			Image.SetValue(Canvas.BottomProperty, (double)invertedOffset.Y);
		}

		///// <summary>
		///// The position of the canvas' origin relative to the Image Block Data
		///// </summary>
		//private void SetCanvasOffset(VectorInt value, double displayZoom)
		//{
		//	if (value != _offset || Math.Abs(displayZoom - _offsetZoom) > 0.001)
		//	{
		//		//Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}. The ScreenCollection Index is {_vm.ScreenCollectionIndex}");
		//		Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}.");
		//		Debug.Assert(value.X >= 0 && value.Y >= 0, "Setting offset to negative value.");

		//		_offset = value;
		//		_offsetZoom = displayZoom;

		//		// For a postive offset, we "pull" the image down and to the left.
		//		var invertedOffset = value.Invert();

		//		var roundedZoom = RoundZoomToOne(displayZoom);
		//		var scaledInvertedOffset = invertedOffset.Scale(1 / roundedZoom);

		//		_image.SetValue(Canvas.LeftProperty, (double)scaledInvertedOffset.X);
		//		_image.SetValue(Canvas.BottomProperty, (double)scaledInvertedOffset.Y);

		//		ReportSizes("SetCanvasOffset");
		//	}
		//}

		//private double RoundZoomToOne(double scale)
		//{
		//	var zoomIsOne = Math.Abs(scale - 1) < 0.001;

		//	if (!zoomIsOne)
		//	{
		//		Debug.WriteLine($"WARNING: MapSectionDisplayControl: Display Zoom is not one.");
		//	}

		//	return zoomIsOne ? 1d : scale;
		//}

		//private void SetCanvasTransform(PointDbl scale)
		//{
		//	_canvas.RenderTransformOrigin = new Point(0.5, 0.5);
		//	_canvas.RenderTransform = new ScaleTransform(scale.X, scale.Y);
		//}

		private void ReportSizes(string label)
		{
			var iSize = new Size(Image.ActualWidth, Image.ActualHeight);
			var bSize = new Size(Bitmap.Width, Bitmap.Height);

			Debug.WriteLine($"At {label}, the sizes are CanvasSizeInBlocks: {CanvasSizeInBlocks}, ImageSizeInBlocks: {ImageSizeInBlocks}, Image: {iSize}, Bitmap: {bSize}.");
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

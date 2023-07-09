using MSetExplorer.MapDisplay.Support;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MSetExplorer
{
	public class CbshDisplayViewModel : ViewModelBase, ICbshDisplayViewModel
	{
		#region Private Fields

		//private readonly SynchronizationContext? _synchronizationContext;
		private readonly object _paintLocker;

		private readonly DrawingGroup _drawingGroup;
		private readonly ScaleTransform _scaleTransform;
		//private readonly GeometryDrawing _foundationRectangle;

		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private ColorBandSet _colorBandSet;
		private ListCollectionView _colorBandsView;
		private readonly IList<GeometryDrawing> _colorBandRectangles;

		private readonly IList<GeometryDrawing> _historgramItems;

		private SizeDbl _viewportSize;
		private VectorDbl _imageOffset;

		private VectorDbl _displayPosition;

		private ScaledImageViewInfo _viewportSizePositionAndScale;

		private ImageSource _imageSource;

		private double _displayZoom;
		private double _minimumDisplayZoom;

		private SizeDbl _containerSize;
		private SizeInt _canvasSize;
		private SizeDbl _unscaledExtent;

		private bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public CbshDisplayViewModel(IMapSectionHistogramProcessor mapSectionHistogramProcessor)
		{
			//_synchronizationContext = SynchronizationContext.Current;

			_paintLocker = new object();
			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;
			_colorBandSet = new ColorBandSet();
			_colorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet);
			_colorBandRectangles = new List<GeometryDrawing>();
			_historgramItems = new List<GeometryDrawing>();

			_viewportSize = new SizeDbl();
			_imageOffset = new VectorDbl();
			_displayPosition = new VectorDbl();

			_viewportSizePositionAndScale = ScaledImageViewInfo.Zero;

			_drawingGroup = new DrawingGroup();
			_scaleTransform = new ScaleTransform();
			_drawingGroup.Transform = _scaleTransform;

			_imageSource = new DrawingImage(_drawingGroup);

			_unscaledExtent = new SizeDbl();
			//_foundationRectangle = BuildFoundationRectangle(_unscaledExtent);
			//_drawingGroup.Children.Add(_foundationRectangle);


			ContainerSize = new SizeDbl(500, 300);
			//LogicalDisplaySize = CanvasSize;

			_mapSectionHistogramProcessor.PercentageBandsUpdated += PercentageBandsUpdated;
			_mapSectionHistogramProcessor.HistogramUpdated += HistogramUpdated;
		}

		#endregion

		#region Events

		//public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;

		#endregion

		#region Public Properties - Content

		public int StartPtr { get; set; }
		public int EndPtr { get; set; }

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The ColorBandSetHistogram Display is processing a new ColorBandSet. Id = {value.Id}.");

					_colorBandSet = value;
					StartPtr = 0;
					EndPtr = _colorBandSet.Count - 1;
					ColorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet);

					var unscaledWidth = GetUnscaledWidth();

					if (unscaledWidth.HasValue)
					{
						UnscaledExtent = new SizeDbl(unscaledWidth.Value, _canvasSize.Height);
						DrawColorBands();
					}
					else
					{
						UnscaledExtent = new SizeDbl(_canvasSize);
					}
				}
			}
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource
		{
			get => _imageSource;
			set
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbshDisplayViewModel's ImageSource is being set to value: {value}.");
				_imageSource = value;
				OnPropertyChanged(nameof(ICbshDisplayViewModel.ImageSource));
			}
		}

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			private set
			{
				_viewportSize = value;
				OnPropertyChanged(nameof(ICbshDisplayViewModel.ViewportSize));
			}
		}

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;

				Debug.WriteLineIf(_useDetailedDebug, $"The container size is now {value}.");

				CanvasSize = ContainerSize.Round();
			}
		}

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbshDisplayViewModel's Canvas Size is now {value}.");
					_canvasSize = value;

					//UnscaledExtent = new SizeDbl(CanvasSize.Scale(DisplayZoom));

					var unscaledWidth = GetUnscaledWidth();

					if (unscaledWidth.HasValue)
					{
						UnscaledExtent = new SizeDbl(unscaledWidth.Value, _canvasSize.Height);
						DrawColorBands();
						DrawHistogram();
					}
					else
					{
						UnscaledExtent = new SizeDbl(_canvasSize);
					}

					OnPropertyChanged(nameof(ICbshDisplayViewModel.CanvasSize));
				}
			}
		}

		public VectorDbl ImageOffset
		{
			get => _imageOffset;
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(_imageOffset, value))
				{
					//Debug.Assert(value.X >= 0 && value.Y >= 0, "The Bitmap Grid's CanvasControlOffset property is being set to a negative value.");
					_imageOffset = value;

					OnPropertyChanged(nameof(ICbshDisplayViewModel.ImageOffset));
				}
			}
		}

		#endregion

		#region Public Properties - Scroll

		public SizeDbl UnscaledExtent
		{
			get => _unscaledExtent;
			set
			{
				if (_unscaledExtent != value)
				{
					_unscaledExtent = value;

					Debug.WriteLineIf(_useDetailedDebug, $"The ColorBandSetHistogram's UnscaledExtent is being set to {value}.");

					//UpdateFoundationRectangle(_foundationRectangle, value);

					OnPropertyChanged(nameof(ICbshDisplayViewModel.UnscaledExtent));
				}
			}
		}

		public VectorDbl DisplayPosition
		{
			get => _displayPosition;
			private set => _displayPosition = value;
		}

		/// <summary>
		/// 1 = LogicalDisplay Size = PosterSize
		/// 2 = LogicalDisplay Size Width is 1/2 PosterSize Width (1 Screen Pixel = 2 * (CanvasSize / PosterSize)
		/// 4 = 1/4 PosterSize
		/// Maximum is PosterSize / Actual CanvasSize 
		/// </summary>
		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value - _displayZoom) > 0.00001)
				{
					_displayZoom = value;
					_scaleTransform.ScaleX = 1 / _displayZoom;
					Debug.WriteLineIf(_useDetailedDebug, $"ColorBandSetHistogram is setting it's DrawingGroup ScaleTransform to {_scaleTransform.ScaleX}.");
					OnPropertyChanged(nameof(ICbshDisplayViewModel.DisplayZoom));
				}
			}
		}

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			private set
			{
				_minimumDisplayZoom = value;
				//if (Math.Abs(value - _maximumDisplayZoom) > 0.001)
				//{
				//	_maximumDisplayZoom = value;

				//	if (DisplayZoom > MaximumDisplayZoom)
				//	{
				//		Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being adjusted to be less or equal to this.");
				//		DisplayZoom = MaximumDisplayZoom;
				//	}
				//	else
				//	{
				//		Debug.WriteLineIf(_useDetailedDebug, $"The MapSectionViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being kept the same.");
				//	}

				//	OnPropertyChanged(nameof(IMapDisplayViewModel.MaximumDisplayZoom));
				//}
			}
		}

		//public Func<IContentScaleInfo, ZoomSlider>? ZoomSliderFactory { get; set; }

		public ScaledImageViewInfo ViewportSizePositionAndScale
		{
			get => _viewportSizePositionAndScale;
			set
			{
				lock (_paintLocker)
				{
					_viewportSize = value.ContentViewportSize;
					var offset = new VectorDbl(value.ContentOffset.X * _scaleTransform.ScaleX, 0);
					ImageOffset = offset;
				}
			}
		}


		#endregion

		#region Private Properties

		private ColorBand? _currentColorBand;

		public ListCollectionView ColorBandsView
		{
			get => _colorBandsView;

			set
			{
				if (_colorBandsView != null)
				{
					_colorBandsView.CurrentChanged -= ColorBandsView_CurrentChanged;
				}

				_colorBandsView = value;

				if (_colorBandsView != null)
				{
					_colorBandsView.CurrentChanged += ColorBandsView_CurrentChanged;
				}

				OnPropertyChanged(nameof(ICbshDisplayViewModel.ColorBandsView));
			}
		}

		public ColorBand? CurrentColorBand
		{
			get => _currentColorBand;
			set
			{
				if (_currentColorBand != null)
				{
					_currentColorBand.PropertyChanged -= CurrentColorBand_PropertyChanged;
				}

				_currentColorBand = value;

				if (_currentColorBand != null)
				{
					_currentColorBand.PropertyChanged += CurrentColorBand_PropertyChanged;
				}

				OnPropertyChanged(nameof(ICbshDisplayViewModel.CurrentColorBand));
			}
		}

		#endregion

		#region Public Methods

		public void RefreshHistogramDisplay()
		{
			DrawHistogram();
		}
		
		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"CbshDisplayViewModel is having its ViewportSizeAndPos set to size:{contentViewportSize}, offset:{contentOffset}, scale:{contentScale}.");

			ViewportSize = contentViewportSize;

			var offset = new VectorDbl(contentOffset.X * _scaleTransform.ScaleX, 0);
			ImageOffset = offset;

			return null;
		}

		public int? UpdateViewportSize(SizeDbl newValue)
		{
			if (!newValue.IsNAN() && newValue != _viewportSize)
			{
				if (newValue.Width <= 2 || newValue.Height <= 2)
				{
					Debug.WriteLine($"WARNING: CbshDisplayViewModel is having its ViewportSize set to {newValue}, which is very small. Update was aborted. The ViewportSize remains: {_viewportSize}.");
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbshDisplayViewModel is having its ViewportSize set to {newValue}. Previously it was {_viewportSize}. Updating it's ContainerSize.");
					ContainerSize = newValue;
				}
			}
			else
			{
				Debug.WriteLineIf(_useDetailedDebug, $"CbshDisplayViewModel is having its ViewportSize set to {newValue}.The current value is aleady: {_viewportSize}, not updating the ContainerSize.");
			}

			return null;
		}

		public int? MoveTo(VectorDbl displayPosition)
		{
			if (UnscaledExtent.IsNearZero())
			{
				throw new InvalidOperationException("Cannot call MoveTo, if the UnscaledExtent is zero.");
			}

			Debug.WriteLineIf(_useDetailedDebug, $"CbshDisplayViewModel is moving to position:{displayPosition}.");

			var offset = new VectorDbl(displayPosition.X * _scaleTransform.ScaleX, 0);
			//var offset = new VectorDbl(displayPosition.X, 0);
			ImageOffset = offset;

			return null;
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				//_bitmapGrid.ClearDisplay();
			}
		}

		#endregion

		#region Event Handlers

		private void CurrentColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			if (ColorBandSet != null)
			{
				ColorBandSet.SelectedColorBandIndex = ColorBandsView.CurrentPosition;
			}

			CurrentColorBand = (ColorBand)ColorBandsView.CurrentItem;
		}

		private void HistogramUpdated(object? sender, HistogramUpdateType e)
		{
			if (e == HistogramUpdateType.Clear)
			{
				ClearHistogramItems();
			}
			else if (e == HistogramUpdateType.Refresh)
			{
				DrawHistogram();
			}
		}

		private void PercentageBandsUpdated(object? sender, PercentageBand[] e)
		{
			UpdatePercentages();
		}

		#endregion

		private int _histElevation = 2;
		private int _histDispHeight = 165;

		private int _cbElevation = 195;
		private int _cbHeight = 35;

		#region Private Methods

		private int? GetUnscaledWidth()
		{
			if (_colorBandSet.Count < 2)
			{
				return null;
			}

			//var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
			//var endingIndex = _colorBandSet[EndPtr].Cutoff;

			////var result = 1 + endingIndex - startingIndex;

			//var highCWidth = _colorBandSet.HighCutoff - _colorBandSet[EndPtr].Cutoff;
			//var result = highCWidth + endingIndex - startingIndex;

			var result = _colorBandSet.HighCutoff;
			return result;
		}

		private void DrawHistogram()
		{
			ClearHistogramItems();

			var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
			var endingIndex = _colorBandSet[EndPtr].Cutoff;
			var highCutoff = _colorBandSet.HighCutoff;

			//var rn = 1 + endingIndex - startingIndex;
			//if (Math.Abs(LogicalDisplaySize.Width - rn) > 20)
			//{
			//	Debug.WriteLineIf(_useDetailedDebug, $"The range of indexes does not match the Logical Display Width. Range: {endingIndex - startingIndex}, Width: {LogicalDisplaySize.Width}.");
			//	return;
			//}

			//LogicalDisplaySize = new SizeInt(rn + 10, _canvasSize.Height);

			var w = (int) Math.Round(UnscaledExtent.Width);

			DrawHistogramBorder(w, _histDispHeight);

			var hEntries = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

			if (hEntries.Length < 1)
			{
				Debug.WriteLine($"WARNING: The Histogram is empty.");
				return;
			}

			var maxV = hEntries.Max(x => x.Value) + 5; // Add 5 to reduce the height of each line.
			var vScaleFactor = _histDispHeight / (double)maxV;

			var geometryGroup = new GeometryGroup();

			foreach (var hEntry in hEntries)
			{
				var x = 1 + hEntry.Key - startingIndex;
				var height = hEntry.Value * vScaleFactor;
				geometryGroup.Children.Add(BuildHLine(x, height));
			}

			var hTestEntry = hEntries[^1];

			var lineGroupDrawing = new GeometryDrawing(Brushes.IndianRed, new Pen(Brushes.DarkRed, 0.75), geometryGroup);

			_historgramItems.Add(lineGroupDrawing);
			_drawingGroup.Children.Add(lineGroupDrawing);
		}

		private LineGeometry BuildHLine(int x, double height)
		{
			//var result = new LineGeometry(new Point(x, _histDispHeight + _histElevation), new Point(x, _histDispHeight - height + _histElevation));

			//var lineTop = _histDispHeight + _histElevation - height;
			//var result = new LineGeometry(new Point(x, lineTop), new Point(x, lineTop + height));

			// Top of the display is when y = 0, y increases as you move from top to bottom
			var lineBottom = 1 + _histDispHeight + _histElevation;
			var lineTop = lineBottom - height;

			var result = new LineGeometry(new Point(x, lineBottom), new Point(x, lineTop));


			return result;
		}

		//private GeometryDrawing DrawHistogramBorder(int width, int height)
		//{
		//	Debug.WriteLineIf(_useDetailedDebug, $"Drawing the Histogram Border with width: {width}.");

		//	var borderSize = width > 1 && height > 1 ? new Size(width - 1, height - 1) : new Size(1, 1);

		//	var histogramBorder = new GeometryDrawing
		//	(
		//		Brushes.Transparent,
		//		new Pen(Brushes.DarkGray, 0.5),
		//		new RectangleGeometry(new Rect(new Point(2, _histElevation), borderSize))
		//	);

		//	_historgramItems.Add(histogramBorder);
		//	_drawingGroup.Children.Add(histogramBorder);

		//	return histogramBorder;
		//}

		private GeometryDrawing DrawHistogramBorder(int width, int height)
		{
			var topLeft = new Point(0, 0);
			var borderSize = width > 1 && height > 1 ? new Size(width + 2, height + 2) : new Size(1, 1);

			var histogramBorder = new GeometryDrawing
			(
				Brushes.Transparent,
				new Pen(Brushes.DarkGray, 1),
				new RectangleGeometry(new Rect(topLeft, borderSize))
			);

			Debug.WriteLineIf(_useDetailedDebug, $"Drawing the Histogram Border with Size: {borderSize} at pos: {topLeft}.");


			_historgramItems.Add(histogramBorder);
			_drawingGroup.Children.Add(histogramBorder);

			return histogramBorder;
		}

		private void DrawColorBands()
		{
			RemoveColorBandRectangles();
			if (_colorBandSet.Count < 2)
			{
				return;
			}

			var curOffset = 0;
			int lastWidth;

			for (var i = StartPtr; i <= EndPtr; i++)
			{
				var colorBand = _colorBandSet[i];
				lastWidth = colorBand.BucketWidth;

				var area = new RectangleDbl(new PointDbl(curOffset, _cbElevation), new SizeDbl(lastWidth, _cbHeight));
				var r = DrawingHelper.BuildRectangle(area, colorBand.StartColor, colorBand.ActualEndColor, horizBlend: true);
				_colorBandRectangles.Add(r);
				_drawingGroup.Children.Add(r);

				curOffset += lastWidth;
			}
		}

		private void UpdatePercentages()
		{

		}

		private void RemoveColorBandRectangles()
		{
			foreach (var colorBandRectangle in _colorBandRectangles)
			{
				_drawingGroup.Children.Remove(colorBandRectangle);
			}

			_colorBandRectangles.Clear();
		}

		private void ClearHistogramItems()
		{
			foreach (var geometryDrawing in _historgramItems)
			{
				_drawingGroup.Children.Remove(geometryDrawing);
			}

			_historgramItems.Clear();
		}

		private GeometryDrawing BuildFoundationRectangle(SizeDbl logicalDisplaySize)
		{
			var result = new GeometryDrawing
			(
				Brushes.Transparent,
				new Pen(Brushes.DarkViolet, 3),
				new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize))
			);

			return result;
		}

		private void UpdateFoundationRectangle(GeometryDrawing foundationRectangle, SizeDbl logicalDisplaySize)
		{
			foundationRectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize));
		}

		#endregion
	}
}

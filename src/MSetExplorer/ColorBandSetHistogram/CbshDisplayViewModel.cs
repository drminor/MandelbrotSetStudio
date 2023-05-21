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
		private readonly GeometryDrawing _foundationRectangle;

		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private ColorBandSet _colorBandSet;
		private ListCollectionView _colorBandsView;
		private readonly IList<GeometryDrawing> _colorBandRectangles;

		private readonly IList<GeometryDrawing> _historgramItems;

		private SizeDbl _viewportSize;
		private VectorDbl _imageOffset;

		private VectorDbl _displayPosition;

		private double _displayZoom;
		private double _minimumDisplayZoom;

		private SizeDbl _containerSize;
		private SizeInt _canvasSize;
		private SizeDbl _unscaledExtent;

		//private bool _useDetailedDebug = false;

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

			_drawingGroup = new DrawingGroup();
			_scaleTransform = new ScaleTransform();
			_drawingGroup.Transform = _scaleTransform;
			ImageSource = new DrawingImage(_drawingGroup);

			_unscaledExtent = new SizeDbl();
			_foundationRectangle = BuildFoundationRectangle(_unscaledExtent);
			_drawingGroup.Children.Add(_foundationRectangle);


			DisplayZoom = 1.0;
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
					Debug.WriteLine($"The ColorBandSetHistogram Display is processing a new ColorBandSet. Id = {value.Id}.");

					_colorBandSet = value;
					StartPtr = 0;
					EndPtr = _colorBandSet.Count - 1;
					ColorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet);

					var newLogicalWidth = DrawColorBands();
					UnscaledExtent = newLogicalWidth.HasValue ? new SizeDbl(newLogicalWidth.Value, _canvasSize.Height) : new SizeDbl(_canvasSize);
				}
			}
		}

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			private set
			{
				_viewportSize = value;
				OnPropertyChanged(nameof(IMapDisplayViewModel.ViewportSize));
			}
		}

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;

				Debug.WriteLine($"The container size is now {value}.");

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
					Debug.WriteLine($"The CbshDisplayViewModel's Canvas Size is now {value}.");
					_canvasSize = value;
					UnscaledExtent = new SizeDbl(CanvasSize.Scale(DisplayZoom));

					var newLogicalWidth = DrawColorBands();
					UnscaledExtent = newLogicalWidth.HasValue ? new SizeDbl(newLogicalWidth.Value, _canvasSize.Height) : new SizeDbl(_canvasSize);
					DrawHistogram();

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

					OnPropertyChanged(nameof(IMapDisplayViewModel.ImageOffset));
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

					Debug.WriteLine($"ColorBandSetHistogram Display's Logical DisplaySize is now {value}.");

					DisplayZoom = _unscaledExtent.Width / (double)_canvasSize.Width;

					UpdateFoundationRectangle(_foundationRectangle, value);

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
				//		Debug.WriteLine($"The MapSectionViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being adjusted to be less or equal to this.");
				//		DisplayZoom = MaximumDisplayZoom;
				//	}
				//	else
				//	{
				//		Debug.WriteLine($"The MapSectionViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being kept the same.");
				//	}

				//	OnPropertyChanged(nameof(IMapDisplayViewModel.MaximumDisplayZoom));
				//}
			}
		}

		//public Func<IContentScaleInfo, ZoomSlider>? ZoomSliderFactory { get; set; }

		#endregion

		#region Public Methods

		public void RefreshHistogramDisplay()
		{
			DrawHistogram();
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

		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double baseScale)
		{
			int? newJobNumber;

			//if (CurrentAreaColorAndCalcSettings == null)
			//{
			//	_bitmapGrid.ViewportSize = contentViewportSize;
			//	ViewportSize = contentViewportSize;
			//	newJobNumber = null;
			//}
			//else
			//{
			//	if (BoundedMapArea == null)
			//	{
			//		throw new InvalidOperationException("The BoundedMapArea is null on call to UpdateViewportSizeAndPos.");
			//	}

			//	newJobNumber = LoadNewView(CurrentAreaColorAndCalcSettings, BoundedMapArea, contentViewportSize, contentOffset, baseScale);
			//}

			newJobNumber = null;
			return newJobNumber;
		}

		public int? UpdateViewportSize(SizeDbl newValue)
		{
			int? newJobNumber = null;

			//if (!newValue.IsNAN() && newValue != _viewportSize)
			//{
			//	if (newValue.Width <= 2 || newValue.Height <= 2)
			//	{
			//		Debug.WriteLine($"WARNING: MapSectionDisplayViewModel is having its ViewportSize set to {newValue}, which is very small. Update was aborted. The ViewportSize remains: {_viewportSize}.");
			//	}
			//	else
			//	{
			//		Debug.WriteLine($"MapSectionDisplayViewModel is having its ViewportSize set to {newValue}. Previously it was {_viewportSize}. The VM is updating the _bitmapGrid.Viewport Size.");
			//		newJobNumber = HandleDisplaySizeUpdate(newValue);
			//	}
			//}
			//else
			//{
			//	Debug.WriteLine($"MapSectionDisplayViewModel is having its ViewportSize set to {newValue}.The current value is aleady: {_viewportSize}, not calling HandleDisplaySizeUpdate, not raising OnPropertyChanged.");
			//}

			return newJobNumber;
		}

		public int? MoveTo(VectorDbl displayPosition)
		{
			//if (BoundedMapArea == null || UnscaledExtent.IsNearZero())
			//{
			//	//Debug.WriteLine($"WARNING: Cannot MoveTo {displayPosition}, there is no bounding info set or the UnscaledExtent is zero.");
			//	//return null;

			//	throw new InvalidOperationException("Cannot call MoveTo, if the boundedMapArea is null or if the UnscaledExtent is zero.");
			//}

			//if (CurrentAreaColorAndCalcSettings == null)
			//{
			//	throw new InvalidOperationException("Cannot call MoveTo, if the CurrentAreaColorAndCalcSettings is null.");
			//}

			//// Get the MapAreaInfo subset for the given display position
			//var mapAreaInfo2Subset = BoundedMapArea.GetView(displayPosition);

			//ReportMove(BoundedMapArea, displayPosition/*, BoundedMapArea.ContentScale, BoundedMapArea.BaseScale*/);

			//var newJobNumber = ReuseAndLoad(CurrentAreaColorAndCalcSettings, mapAreaInfo2Subset, out var lastSectionWasIncluded);

			//DisplayPosition = displayPosition;

			//if (newJobNumber.HasValue && lastSectionWasIncluded)
			//{
			//	DisplayJobCompleted?.Invoke(this, newJobNumber.Value);
			//}

			int? newJobNumber = null;
			return newJobNumber;
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

		private int _histDispHeight = 150;
		private int _cbElevation = 160;
		private int _cbHeight = 35;

		#region Private Methods

		private void DrawHistogram()
		{
			ClearHistogramItems();

			var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
			var endingIndex = _colorBandSet[EndPtr].Cutoff;
			var highCutoff = _colorBandSet.HighCutoff;

			//var rn = 1 + endingIndex - startingIndex;
			//if (Math.Abs(LogicalDisplaySize.Width - rn) > 20)
			//{
			//	Debug.WriteLine($"The range of indexes does not match the Logical Display Width. Range: {endingIndex - startingIndex}, Width: {LogicalDisplaySize.Width}.");
			//	return;
			//}

			//LogicalDisplaySize = new SizeInt(rn + 10, _canvasSize.Height);

			var w = (int) Math.Round(UnscaledExtent.Width - 10);

			DrawHistogramBorder(w, _histDispHeight);

			var hEntries = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

			if (hEntries.Length < 1)
			{
				Debug.WriteLine($"The Histogram is empty.");
				return;
			}

			var maxV = hEntries.Max(x => x.Value);
			var vScaleFactor = _histDispHeight / (double)maxV;

			var geometryGroup = new GeometryGroup();

			foreach (var hEntry in hEntries)
			{
				var x = hEntry.Key - startingIndex;
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
			var result = new LineGeometry(new Point(x, _histDispHeight - 2), new Point(x, _histDispHeight - height - 2));
			return result;
		}

		private GeometryDrawing DrawHistogramBorder(int width, int height)
		{
			var borderSize = width > 1 && height > 1 ? new Size(width - 1, height - 1) : new Size(1, 1);
			var histogramBorder = new GeometryDrawing
			(
				Brushes.Transparent,
				new Pen(Brushes.DarkGray, 0.5),
				new RectangleGeometry(new Rect(new Point(2, 2), borderSize))
			);

			_historgramItems.Add(histogramBorder);
			_drawingGroup.Children.Add(histogramBorder);

			return histogramBorder;
		}

		private int? DrawColorBands()
		{
			ClearColorBands();
			if (_colorBandSet.Count < 2)
			{
				return null;
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

			return GetExtent() + 10;
		}

		private int GetExtent()
		{
			var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
			var endingIndex = _colorBandSet[EndPtr].Cutoff;

			var result = 1 + endingIndex - startingIndex;

			return result;
		}

		private void UpdatePercentages()
		{

		}

		private void ClearColorBands()
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
				new Pen(Brushes.Transparent, 1),
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

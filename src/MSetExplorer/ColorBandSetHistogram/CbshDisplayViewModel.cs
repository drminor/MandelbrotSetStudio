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
		//private readonly SynchronizationContext? _synchronizationContext;
		//private readonly object _paintLocker;

		private readonly DrawingGroup _drawingGroup;
		private readonly ScaleTransform _scaleTransform;
		private readonly GeometryDrawing _foundationRectangle;

		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private SizeDbl _containerSize;
		private SizeInt _canvasSize;
		private double _displayZoom;
		private SizeInt _logicalDisplaySize;

		private ColorBandSet _colorBandSet;
		private ListCollectionView _colorBandsView;
		private readonly IList<GeometryDrawing> _colorBandRectangles;

		private readonly IList<GeometryDrawing> _historgramItems;

		#region Constructor

		public CbshDisplayViewModel(IMapSectionHistogramProcessor mapSectionHistogramProcessor)
		{
			//_synchronizationContext = SynchronizationContext.Current;

			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;
			_colorBandSet = new ColorBandSet();
			_colorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet);
			_colorBandRectangles = new List<GeometryDrawing>();
			_historgramItems = new List<GeometryDrawing>();

			_drawingGroup = new DrawingGroup();
			_scaleTransform = new ScaleTransform();
			_drawingGroup.Transform = _scaleTransform;
			ImageSource = new DrawingImage(_drawingGroup);

			_logicalDisplaySize = new SizeInt();
			_foundationRectangle = BuildFoundationRectangle(_logicalDisplaySize);
			_drawingGroup.Children.Add(_foundationRectangle);


			DisplayZoom = 1.0;
			ContainerSize = new SizeDbl(500, 300);
			LogicalDisplaySize = CanvasSize;

			_mapSectionHistogramProcessor.PercentageBandsUpdated += PercentageBandsUpdated;
			_mapSectionHistogramProcessor.HistogramUpdated += HistogramUpdated;
		}

		#endregion

		#region Public Properties

		//public event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;

		public new bool InDesignMode => base.InDesignMode;

		public ImageSource ImageSource { get; init; }

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
					Debug.WriteLine($"The MapDisplay Canvas Size is now {value}.");
					_canvasSize = value;
					LogicalDisplaySize = CanvasSize.Scale(DisplayZoom);

					var newLogicalWidth = DrawColorBands();
					LogicalDisplaySize = newLogicalWidth.HasValue ? new SizeInt(newLogicalWidth.Value, _canvasSize.Height) : _canvasSize;
					DrawHistogram();

					OnPropertyChanged(nameof(IMapDisplayViewModel.CanvasSize));
				}
			}
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
					OnPropertyChanged();
				}
			}
		}

		public SizeInt LogicalDisplaySize
		{
			get => _logicalDisplaySize;
			set
			{
				if (_logicalDisplaySize != value)
				{
					_logicalDisplaySize = value;

					Debug.WriteLine($"MapDisplay's Logical DisplaySize is now {value}.");

					DisplayZoom = _logicalDisplaySize.Width / (double)_canvasSize.Width;

					UpdateFoundationRectangle(_foundationRectangle, value);

					OnPropertyChanged(nameof(IMapDisplayViewModel.LogicalDisplaySize));
				}
			}
		}

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
					LogicalDisplaySize = newLogicalWidth.HasValue ? new SizeInt(newLogicalWidth.Value, _canvasSize.Height) : _canvasSize;
				}
			}
		}

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

				OnPropertyChanged();
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

				OnPropertyChanged();
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

			DrawHistogramBorder(LogicalDisplaySize.Width - 10, _histDispHeight);

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
			var histogramBorder = new GeometryDrawing
			(
				Brushes.Transparent,
				new Pen(Brushes.DarkGray, 0.5),
				new RectangleGeometry(new Rect(new Point(2, 2), new Size(width - 1, height - 1)))
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

		private GeometryDrawing BuildFoundationRectangle(SizeInt logicalDisplaySize)
		{
			var result = new GeometryDrawing
			(
				Brushes.Transparent,
				new Pen(Brushes.Transparent, 1),
				new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize))
			);

			return result;
		}

		private void UpdateFoundationRectangle(GeometryDrawing foundationRectangle, SizeInt logicalDisplaySize)
		{
			foundationRectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize));
		}

		#endregion
	}
}

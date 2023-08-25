using MSS.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace MSetExplorer
{
	public class CbsHistogramViewModel : ViewModelBase, ICbsHistogramViewModel
	{
		#region Private Fields

		private readonly object _paintLocker;

		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;

		private ColorBandSet _colorBandSet;
		private ListCollectionView? _colorBandsView;

		private ColorBand? _currentColorBand;

		private HPlotSeriesData _seriesData;

		private SizeDbl _unscaledExtent;		// Size of entire content at max zoom (i.e, 4 x Target Iterations)
		private SizeDbl _viewportSize;          // Size of display area in device independent pixels.
		private SizeDbl _contentViewportSize;	// Size of visible content

		private VectorDbl _displayPosition;

		private double _displayZoom;
		private double _minimumDisplayZoom;
		private double _maximumDisplayZoom;

		private ScrollBarVisibility _horizontalScrollBarVisibility;

		private bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public CbsHistogramViewModel(IMapSectionHistogramProcessor mapSectionHistogramProcessor)
		{
			_paintLocker = new object();

			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;

			_colorBandSet = new ColorBandSet();
			_colorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet);

			_seriesData = HPlotSeriesData.Empty;

			_unscaledExtent = new SizeDbl();
			_viewportSize = new SizeDbl(500, 300);
			_contentViewportSize = new SizeDbl();

			_displayPosition = new VectorDbl();

			_minimumDisplayZoom = RMapConstants.DEFAULT_MINIMUM_DISPLAY_ZOOM; // 0.0625;
			_maximumDisplayZoom = 4.0;
			_displayZoom = 1;

			HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;

			_mapSectionHistogramProcessor.HistogramUpdated += HistogramUpdated;
		}

		#endregion

		#region Events

		public event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;
		public event EventHandler<ValueTuple<int, int>>? ColorBandCutoffChanged;

		#endregion

		#region Public Properties - Content

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					ColorBandsView = null;

					Debug.WriteLineIf(_useDetailedDebug, $"The ColorBandSetHistogram Display is processing a new ColorBandSet. Id = {value.Id}.");

					_colorBandSet = value;

					var unscaledWidth = GetExtent(_colorBandSet);

					if (unscaledWidth > 10)
					{
						ResetView(unscaledWidth, DisplayPosition, DisplayZoom);
					}

					ColorBandsView = (ListCollectionView)CollectionViewSource.GetDefaultView(_colorBandSet);
				}
			}
		}

		public ListCollectionView? ColorBandsView
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

				OnPropertyChanged(nameof(ICbsHistogramViewModel.ColorBandsView));
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

				OnPropertyChanged(nameof(ICbsHistogramViewModel.CurrentColorBand));
			}
		}

		//public int PlotExtent => SeriesData.Length;

		#endregion

		#region Public Properties - Control

		public new bool InDesignMode => base.InDesignMode;

		public SizeDbl ViewportSize
		{
			get => _viewportSize;
			set
			{
				if (!value.IsNAN() && value != _viewportSize)
				{
					if (value.Width <= 2 || value.Height <= 2)
					{
						Debug.WriteLine($"WARNING: CbsHistogramViewModel is having its ViewportSize set to {value}, which is very small. Update was aborted. The ViewportSize remains: {_viewportSize}.");
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ViewportSize set to {value}. Previously it was {_viewportSize}. Updating it's ContainerSize.");

						_viewportSize = value;

						OnPropertyChanged(nameof(ICbsHistogramViewModel.ViewportSize));
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ViewportSize set to {value}.The current value is aleady: {_viewportSize}, not updating the ContainerSize.");
				}
			}
		}

		public SizeDbl ContentViewportSize
		{
			get => _contentViewportSize;
			set
			{
				if (!value.IsNAN() && value != _contentViewportSize)
				{
					if (value.Width <= 2 || value.Height <= 2)
					{
						Debug.WriteLine($"WARNING: CbsHistogramViewModel is having its ContentViewportSize set to {value}, which is very small. Update was aborted. The ContentViewportSize remains: {_contentViewportSize}.");
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ContentViewportSize set to {value}. Previously it was {_contentViewportSize}.");

						_contentViewportSize = value;

						OnPropertyChanged(nameof(ICbsHistogramViewModel.ContentViewportSize));
					}
				}
				else
				{
					Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is having its ContentViewportSize set to {value}.The current value is aleady: {_contentViewportSize}.");
				}
			}
		}

		public HPlotSeriesData SeriesData
		{
			get => _seriesData;
			set
			{
				Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's Series is being set. There are {value.LongLength} entries.");
				_seriesData = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.SeriesData));
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

					Debug.WriteLineIf(_useDetailedDebug, $"\nThe ColorBandSetHistogram's UnscaledExtent is being set to {value}.");

					//UpdateFoundationRectangle(_foundationRectangle, value);

					OnPropertyChanged(nameof(ICbsHistogramViewModel.UnscaledExtent));
				}
			}
		}

		public VectorDbl DisplayPosition
		{
			get => _displayPosition;
			set
			{
				if (ScreenTypeHelper.IsVectorDblChanged(value, DisplayPosition))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's DisplayPosition is being updated from {_displayPosition} to {value}.");

					_displayPosition = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.DisplayPosition));
				}
			}
		}

		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (ScreenTypeHelper.IsDoubleChanged(value, _displayZoom, 0.00001))
				{
					Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's DisplayZoom is being updated from {_displayZoom} to {value}.");
					
					_displayZoom = value;
					OnPropertyChanged(nameof(ICbsHistogramViewModel.DisplayZoom));
				}
			}
		}

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			set
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\nThe CbsHistogramViewModel's MinimumDisplayZoom is being updated from {_minimumDisplayZoom} to {value}.");

				_minimumDisplayZoom = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.MinimumDisplayZoom));
			}
		}

		public double MaximumDisplayZoom
		{
			get => _maximumDisplayZoom;
			set
			{
				Debug.WriteLineIf(_useDetailedDebug, $"\nThe CbsHistogramViewModel's MaximumDisplayZoom is being updated from {_maximumDisplayZoom} to {value}.");

				_maximumDisplayZoom = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.MaximumDisplayZoom));
			}
		}

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => _horizontalScrollBarVisibility;
			set
			{
				_horizontalScrollBarVisibility = value;
				OnPropertyChanged(nameof(ICbsHistogramViewModel.HorizontalScrollBarVisibility));
			}
		}

		#endregion

		#region Public Methods

		public bool RefreshDisplay()
		{
			bool result;

			lock (_paintLocker)
			{
				var values = _mapSectionHistogramProcessor.Histogram.Values;

				var anyNonzero = values.Any(x => x > 0);
				result = !anyNonzero;

				BuildSeriesData(SeriesData, values);
			}

			Debug.WriteLineIf(_useDetailedDebug, $"The CbsHistogramViewModel's SerieData is being updated There are {SeriesData.LongLength} entries.");

			// TODO: Implement some way to have the HistogramPlotControl be notfied of an update without creating a new instance.
			SeriesData = new HPlotSeriesData(SeriesData);

			//OnPropertyChanged(nameof(ICbsHistogramViewModel.SeriesData));

			return result;
		}

		public int? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"\nCbsHistogramViewModel is having its ViewportSizePosAndScale set to size:{contentViewportSize}, offset:{contentOffset}, scale:{contentScale}.");

			//_displayZoom uses a binding to stay curent with contentScale;	
			Debug.Assert(_displayZoom == contentScale, "The DisplayZoom does not equal the new ContentScale on the call to UpdateViewportSizePosAndScale.");

			ContentViewportSize = contentViewportSize;
			DisplayPosition = contentOffset;

			return null;
		}

		public int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"\nCbsHistogramViewModel is having its ViewportSizeAndPos set to size:{contentViewportSize}, offset:{contentOffset}.");

			ContentViewportSize = contentViewportSize;
			DisplayPosition = contentOffset;

			return null;
		}

		public int? MoveTo(VectorDbl displayPosition)
		{
			if (UnscaledExtent.IsNearZero())
			{
				throw new InvalidOperationException("Cannot call MoveTo, if the UnscaledExtent is zero.");
			}

			Debug.WriteLineIf(_useDetailedDebug, $"CbsHistogramViewModel is moving to position:{displayPosition}.");

			DisplayPosition = displayPosition;

			return null;
		}

		public void ClearDisplay()
		{
			lock (_paintLocker)
			{
				SeriesData.Clear();
			}
		}

		public void UpdateColorBandCutoff(int colorBandIndex, int newCutoff)
		{
			ColorBandCutoffChanged?.Invoke(this, (colorBandIndex, newCutoff));
		}

		#endregion

		#region Event Handlers

		private void CurrentColorBand_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
		}

		private void ColorBandsView_CurrentChanged(object? sender, EventArgs e)
		{
			if (ColorBandsView != null)
			{
				if (ColorBandSet != null)
				{
					ColorBandSet.SelectedColorBandIndex = ColorBandsView.CurrentPosition;
				}

				CurrentColorBand = (ColorBand)ColorBandsView.CurrentItem;
			}
		}

		private void HistogramUpdated(object? sender, HistogramUpdateType e)
		{
			// TODO: Consider handling BlockAdd and BlockRemoves

			switch (e)
			{
				case HistogramUpdateType.BlockAdded:
					//Debug.WriteLine($"Not handling HistogramUpdated. The update type is {e}.");
					break;
				case HistogramUpdateType.BlockRemoved:
					//Debug.WriteLine($"Not handling HistogramUpdated. The update type is {e}.");
					break;
				case HistogramUpdateType.Clear:
					{
						lock (_paintLocker)
						{
							SeriesData.ClearYValues();
							SeriesData = new HPlotSeriesData(SeriesData);
						}
						break;
					}
				case HistogramUpdateType.Refresh:
					{
						RefreshDisplay();
						break;
					}
				default:
					break;
			}
		}

		#endregion

		#region Private Methods

		private void BuildSeriesData(HPlotSeriesData hPlotSeriesData, int[] values)
		{
			var longLength = values.LongLength;
			
			if (longLength < 1)
			{
				Debug.WriteLine($"WARNING: The Histogram is empty (BuildSeriesData).");

				hPlotSeriesData.Clear();
				return;
			}

			var existingLength = hPlotSeriesData.LongLength;
			hPlotSeriesData.SetYValues(values, out var bufferWasPreserved);

			if (!bufferWasPreserved)
			{
				Debug.WriteLine($"WARNING: Allocating new buffer to hold the Y Values. New length: {longLength}, existing length:{existingLength}.");
			}
			else
			{
				Debug.WriteLine($"Updating SeriesData. Using existing buffer. Length:{longLength}.");
			}
		}

		private int GetExtent(ColorBandSet colorBandSet/*, out int endPtr*/)
		{
			var result = colorBandSet.Count < 2 ? 0 : colorBandSet.HighCutoff;
			//endPtr = colorBandSet.Count < 2 ? 0 : colorBandSet.Count - 1;
			return result;
		}

		private void ResetView(double extentWidth, VectorDbl displayPosition, double displayZoom)
		{
			if (ScreenTypeHelper.IsDoubleChanged(extentWidth, UnscaledExtent.Width))
			{
				// The ExtentWidth is changing -- reset the position and scale.
				UnscaledExtent = new SizeDbl();

				var extent = new SizeDbl(extentWidth, _viewportSize.Height);
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is raising the DisplaySettingsInitialized Event and resetting the Position and Scale.");

				var initialSettingsF = new DisplaySettingsInitializedEventArgs(extent, displayPosition, displayZoom);

				// Override the position
				var positionZero = new VectorDbl();
				// Override the zoom setting
				var zoomUnity = 1d;
				var initialSettings = new DisplaySettingsInitializedEventArgs(extent, positionZero, zoomUnity);

				DisplaySettingsInitialized?.Invoke(this, initialSettings);

				// Trigger a ViewportChanged event on the PanAndZoomControl -- this will result in our UpdateViewportSizeAndPos method being called.
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is setting the Unscaled Extent to complete the process of initializing the Histogram Display.");
				UnscaledExtent = extent;
			}
			else
			{
				Debug.WriteLine("The CbsHistogramViewModel is skipping ResetView: the current and new extent (i.e., Target Iterations) are the same.");

				UnscaledExtent = new SizeDbl();

				var extent = new SizeDbl(extentWidth, _viewportSize.Height);
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is raising the DisplaySettingsInitialized Event and keeping the Position and Scale.");

				var initialSettings = new DisplaySettingsInitializedEventArgs(extent, displayPosition, displayZoom);

				DisplaySettingsInitialized?.Invoke(this, initialSettings);

				// Trigger a ViewportChanged event on the PanAndZoomControl -- this will result in our UpdateViewportSizeAndPos method being called.
				Debug.WriteLineIf(_useDetailedDebug, "\n\t\t====== CbsHistogramViewModel is setting the Unscaled Extent to complete the process of initializing the Histogram Display.");
				UnscaledExtent = extent;

			}
		}

		#endregion

		#region UN USED

		//public ScaledImageViewInfo ViewportSizePositionAndScale
		//{
		//	get => _viewportSizePositionAndScale;
		//	set
		//	{
		//		lock (_paintLocker)
		//		{
		//			_viewportSize = value.ContentViewportSize;
		//			var offset = new VectorDbl(value.ContentOffset.X * _scaleTransform.ScaleX, 0);
		//			ImageOffset = offset;
		//		}
		//	}
		//}

		//private HPlotSeriesData BuildSeriesDataOld()
		//{
		//	var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
		//	var endingIndex = _colorBandSet[EndPtr].Cutoff;
		//	//var highCutoff = _colorBandSet.HighCutoff;

		//	var hEntries = GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

		//	if (hEntries.Length < 1)
		//	{
		//		Debug.WriteLine($"WARNING: The Histogram is empty (BuildSeriesData).");
		//		return HPlotSeriesData.Zero;
		//	}

		//	var dataX = new double[hEntries.Length];
		//	var dataY = new double[hEntries.Length];

		//	var extent = Math.Min(hEntries.Length, PlotExtent);

		//	for (var hPtr = 0; hPtr < extent; hPtr++)
		//	{
		//		var hEntry = hEntries[hPtr];

		//		dataX[hPtr] = hEntry.Key;
		//		dataY[hPtr] = hEntry.Value;
		//	}

		//	var result = new HPlotSeriesData(dataX, dataY);

		//	return result;
		//}

		//private KeyValuePair<int, int>[] GetKeyValuePairsForBand(int startingIndex, int endingIndex, bool includeCatchAll)
		//{
		//	var hEntries = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true);

		//	return hEntries;

		//}

		//private IEnumerable<KeyValuePair<int, int>> GetKeyValuePairsForBand(int previousCutoff, int cutoff)
		//{
		//	return _mapSectionHistogramProcessor.GetKeyValuePairsForBand(previousCutoff, cutoff);
		//}

		//private int[] GetACopyOfTheValuesArray()
		//{
		//	return _mapSectionHistogramProcessor.Histogram.Values;
		//}

		//private (double[] dataX, double[] dataY) GetPlotData1()
		//{
		//	//ClearHistogramItems();

		//	var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
		//	var endingIndex = _colorBandSet[EndPtr].Cutoff;
		//	var highCutoff = _colorBandSet.HighCutoff;

		//	var hEntries = GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

		//	if (hEntries.Length < 1)
		//	{
		//		Debug.WriteLine($"WARNING: The Histogram is empty (BuildSeriesData).");
		//		return (new double[0], new double[0]);
		//	}

		//	var dataX = new double[hEntries.Length];
		//	var dataY = new double[hEntries.Length];

		//	for (var hPtr = 0; hPtr < hEntries.Length; hPtr++)
		//	{
		//		var hEntry = hEntries[hPtr];
		//		dataX[hPtr] = hEntry.Key;
		//		dataY[hPtr] = hEntry.Value;
		//	}

		//	return (dataX, dataY);
		//}


		//private int _histElevation = 2;
		//private int _histDispHeight = 165;

		//private void DrawHistogram()
		//{
		//	ClearHistogramItems();

		//	var startingIndex = _colorBandSet[StartPtr].StartingCutoff;
		//	var endingIndex = _colorBandSet[EndPtr].Cutoff;
		//	var highCutoff = _colorBandSet.HighCutoff;

		//	//var rn = 1 + endingIndex - startingIndex;
		//	//if (Math.Abs(LogicalDisplaySize.Width - rn) > 20)
		//	//{
		//	//	Debug.WriteLineIf(_useDetailedDebug, $"The range of indexes does not match the Logical Display Width. Range: {endingIndex - startingIndex}, Width: {LogicalDisplaySize.Width}.");
		//	//	return;
		//	//}

		//	//LogicalDisplaySize = new SizeInt(rn + 10, _canvasSize.Height);

		//	var w = (int) Math.Round(UnscaledExtent.Width);

		//	DrawHistogramBorder(w, _histDispHeight);

		//	var hEntries = _mapSectionHistogramProcessor.GetKeyValuePairsForBand(startingIndex, endingIndex, includeCatchAll: true).ToArray();

		//	if (hEntries.Length < 1)
		//	{
		//		Debug.WriteLine($"WARNING: The Histogram is empty. (DrawHistogram)");
		//		return;
		//	}

		//	var maxV = hEntries.Max(x => x.Value) + 5; // Add 5 to reduce the height of each line.
		//	var vScaleFactor = _histDispHeight / (double)maxV;

		//	var geometryGroup = new GeometryGroup();

		//	foreach (var hEntry in hEntries)
		//	{
		//		var x = 1 + hEntry.Key - startingIndex;
		//		var height = hEntry.Value * vScaleFactor;
		//		geometryGroup.Children.Add(BuildHLine(x, height));
		//	}

		//	var hTestEntry = hEntries[^1];

		//	var lineGroupDrawing = new GeometryDrawing(Brushes.IndianRed, new Pen(Brushes.DarkRed, 0.75), geometryGroup);

		//	_historgramItems.Add(lineGroupDrawing);
		//	_drawingGroup.Children.Add(lineGroupDrawing);
		//}

		//private LineGeometry BuildHLine(int x, double height)
		//{
		//	//var result = new LineGeometry(new Point(x, _histDispHeight + _histElevation), new Point(x, _histDispHeight - height + _histElevation));

		//	//var lineTop = _histDispHeight + _histElevation - height;
		//	//var result = new LineGeometry(new Point(x, lineTop), new Point(x, lineTop + height));

		//	// Top of the display is when y = 0, y increases as you move from top to bottom
		//	var lineBottom = 1 + _histDispHeight + _histElevation;
		//	var lineTop = lineBottom - height;

		//	var result = new LineGeometry(new Point(x, lineBottom), new Point(x, lineTop));


		//	return result;
		//}

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

		//private GeometryDrawing DrawHistogramBorder(int width, int height)
		//{
		//	var topLeft = new Point(0, 0);
		//	var borderSize = width > 1 && height > 1 ? new Size(width + 2, height + 2) : new Size(1, 1);

		//	var histogramBorder = new GeometryDrawing
		//	(
		//		Brushes.Transparent,
		//		new Pen(Brushes.DarkGray, 1),
		//		new RectangleGeometry(new Rect(topLeft, borderSize))
		//	);

		//	Debug.WriteLineIf(_useDetailedDebug, $"Drawing the Histogram Border with Size: {borderSize} at pos: {topLeft}.");


		//	_historgramItems.Add(histogramBorder);
		//	_drawingGroup.Children.Add(histogramBorder);

		//	return histogramBorder;
		//}

		//private GeometryDrawing BuildFoundationRectangle(SizeDbl logicalDisplaySize)
		//{
		//	var result = new GeometryDrawing
		//	(
		//		Brushes.Transparent,
		//		new Pen(Brushes.DarkViolet, 3),
		//		new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize))
		//	);

		//	return result;
		//}

		//private void UpdateFoundationRectangle(GeometryDrawing foundationRectangle, SizeDbl logicalDisplaySize)
		//{
		//	foundationRectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(logicalDisplaySize));
		//}

		//private HPlotSeriesData BuildTestSeries()
		//{
		//	double[] dataX = new double[] { 1, 2, 3, 4, 5 };
		//	double[] dataY = new double[] { 1, 4, 9, 16, 25 };

		//	var result = new HPlotSeriesData(dataX, dataY);

		//	return result;
		//}

		#endregion
	}
}

using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapSectionPzControl.xaml
	/// </summary>
	public partial class MapSectionDisplayControl : UserControl
	{
		#region Private Properties

		//private readonly static bool SHOW_BORDER = false;

		private IMapDisplayViewModel _vm;

		private SelectionRectangle _selectionRectangle;
		//private Border? _border;

		#endregion

		#region Constructor

		public MapSectionDisplayControl()
		{
			_vm = (IMapDisplayViewModel)DataContext;

			_selectionRectangle = new SelectionRectangle(new Canvas(), new SizeDbl(), RMapConstants.BLOCK_SIZE);

			Loaded += MapSectionDisplayControl_Loaded;
			Unloaded += MapSectionDisplayControl_Unloaded;
			InitializeComponent();
		}

		private void MapSectionDisplayControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapSectionDisplayControl is being loaded.");
				return;
			}
			else
			{
				_vm = (IMapDisplayViewModel)DataContext;

				var ourSize = new SizeDbl(ActualWidth, ActualHeight);
				_vm.ViewportSize = ourSize;

				_vm.PropertyChanged += MapDisplayViewModel_PropertyChanged;

				BitmapGridControl1.UseScaling = false;
				//BitmapGridControl1.ViewportSizeChanged += BitmapGridControl1_ViewportSizeChanged;

				_selectionRectangle = new SelectionRectangle(BitmapGridControl1.Canvas, _vm.ViewportSize, _vm.BlockSize);
				_selectionRectangle.AreaSelected += SelectionRectangle_AreaSelected;
				_selectionRectangle.ImageDragged += SelectionRectangle_ImageDragged;

				Debug.WriteLine("The MapSectionDisplay Control is now loaded");
			}
		}

		private void MapSectionDisplayControl_Unloaded(object sender, RoutedEventArgs e)
		{
			_vm.PropertyChanged -= MapDisplayViewModel_PropertyChanged;
			//BitmapGridControl1.ViewportSizeChanged -= BitmapGridControl1_ViewportSizeChanged;

			if (!(_selectionRectangle is null))
			{
				_selectionRectangle.AreaSelected -= SelectionRectangle_AreaSelected;
				_selectionRectangle.ImageDragged -= SelectionRectangle_ImageDragged;
				_selectionRectangle.TearDown();
			}
		}

		//private Border BuildBorder(Canvas canvas)
		//{
		//	var result = new Border
		//	{
		//		Width = canvas.Width + 4,
		//		Height = canvas.Width + 4,
		//		HorizontalAlignment = HorizontalAlignment.Left,
		//		VerticalAlignment = VerticalAlignment.Top,
		//		BorderThickness = new Thickness(1),
		//		BorderBrush = Brushes.BlueViolet,
		//		Visibility = Visibility.Visible
		//	};

		//	_ = canvas.Children.Add(result);
		//	result.SetValue(Canvas.LeftProperty, -2d);
		//	result.SetValue(Canvas.TopProperty, -2d);
		//	result.SetValue(Panel.ZIndexProperty, 100);

		//	return result;
		//}

		#endregion

		#region Event Handlers

		private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.ViewportSize))
			{
				Debug.Assert(!ScreenTypeHelper.IsSizeDblChanged(_vm.LogicalViewportSize, _vm.ViewportSize), "The VM's Logical ViewportSize and the VM's ViewportSize are not the same.");

				_selectionRectangle.DisplaySize = _vm.ViewportSize;
			}
			else if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings))
			{
				_selectionRectangle.IsEnabled = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo != null;

				//// Just for Diagnostics
				//_selectionRectangle.MapAreaInfo = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo;
			}
		}

		//private void BitmapGridControl1_ViewportSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	BitmapGridControl1.TranslationAndClipSize = new RectangleDbl(new PointDbl(), e.Item2);
		//}

		private void SelectionRectangle_AreaSelected(object? sender, AreaSelectedEventArgs e)
		{
			if (!e.IsPreview)
			{
				var mapAreaInfo = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo;
				ReportFactorsVsSamplePointResolution(mapAreaInfo, e);
			}

			_vm.RaiseMapViewZoomUpdate(e);
		}

		private void SelectionRectangle_ImageDragged(object? sender, ImageDraggedEventArgs e)
		{
			_vm.RaiseMapViewPanUpdate(e);
		}

		#endregion

		#region Diagnostics

		[Conditional("DEBUG2")]
		private void ReportFactorsVsSamplePointResolution(MapAreaInfo2? mapAreaInfo, AreaSelectedEventArgs e)
		{
			if (mapAreaInfo == null) return;

			Debug.WriteLine("\nReporting various factors vs SamplePointDeltas.");

			var pointAndDelta = mapAreaInfo.PositionAndDelta;
			var reciprocal = 1 / e.Factor;
			var rK = (int)Math.Round(reciprocal * 1024);

			Debug.WriteLine($"Current SPD: {pointAndDelta.SamplePointDelta}. Starting with a rk of {rK}.");

			var st = Math.Max(rK - 20, 1);

			for (var i = 0; i < 41; i++)
			{
				var sFactor = 1 / ((double)st / 1024);
				var rReciprocal = new RValue(st++, -10);

				var rawResult = pointAndDelta.ScaleDelta(rReciprocal);
				var result = Reducer.Reduce(rawResult);

				Debug.WriteLine($"{i,3}: \trk: {st,4}\traw-W: {rawResult.SamplePointDelta.Width,10}\tfinal-W: {result.SamplePointDelta.Width,10}\tfinal-H: {result.SamplePointDelta.Height,10}\tfactor: {sFactor}.");
			}

			//var newPd = RMapHelper.GetNewSamplePointDelta(mapAreaInfo.PositionAndDelta, e.Factor);
			//Debug.WriteLine($"The new SPD is {newPd.SamplePointDelta}.");
		}

		#endregion
	}
}

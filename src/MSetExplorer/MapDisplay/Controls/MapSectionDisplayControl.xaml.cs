using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapSectionDisplayControl.xaml
	/// </summary>
	public partial class MapSectionDisplayControl : UserControl
	{
		#region Private Properties

		//private readonly static bool SHOW_BORDER = false;
		private readonly static bool CLIP_IMAGE_BLOCKS = true;

		private IMapDisplayViewModel _vm;

		private Canvas _canvas;

		private SelectionRectangle? _selectionRectangle;
		//private Border? _border;

		#endregion

		#region Constructor

		public MapSectionDisplayControl()
		{
			_canvas = new Canvas();
			_vm = (IMapDisplayViewModel)DataContext;

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
				_canvas = BitmapGridControl1.Canvas;
				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;

				_vm = (IMapDisplayViewModel)DataContext;
				BitmapGridControl1.DisposeMapSection = _vm.DisposeMapSection;
				_vm.BitmapGrid = BitmapGridControl1.BitmapGrid;

				//_vm.CanvasSize = BitmapGridControl1.ViewPortSize;

				//BitmapGridControl1.ImageOffset = _vm.ImageOffset;

				//BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;

				_vm.PropertyChanged += MapDisplayViewModel_PropertyChanged;

				_selectionRectangle = new SelectionRectangle(_canvas, _vm.CanvasSize, _vm.BlockSize);
				_selectionRectangle.AreaSelected += SelectionRectangle_AreaSelected;
				_selectionRectangle.ImageDragged += SelectionRectangle_ImageDragged;

				// A border is helpful for troubleshooting.
				//_border = SHOW_BORDER && (!CLIP_IMAGE_BLOCKS) ? BuildBorder(_canvas) : null;
				//_border = null;

				Debug.WriteLine("The MapSectionDisplay Control is now loaded");
			}
		}

		private void MapSectionDisplayControl_Unloaded(object sender, RoutedEventArgs e)
		{
			//BitmapGridControl1.ViewPortSizeChanged -= BitmapGridControl1_ViewPortSizeChanged;

			_vm.PropertyChanged -= MapDisplayViewModel_PropertyChanged;

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

		//#region Public Events

		//public event EventHandler<ValueTuple<Size, Size>>? ViewPortSizeChanged
		//{
		//	add
		//	{
		//		BitmapGridControl1.ViewPortSizeChanged += value;
		//	}
		//	remove
		//	{
		//		BitmapGridControl1.ViewPortSizeChanged -= value;
		//	}
		//}

		//#endregion

		#region Event Handlers

		//private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	Debug.WriteLine($"The {nameof(BitmapGridTestWindow)} is handling ViewPort Size Changed. Prev: {e.Item1}, New: {e.Item2}.");

		//	BitmapGridControl1.ReportSizes("ViewPortSizeChanged");
		//}

		private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (_selectionRectangle == null)
			{
				return;
			}

			if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings))
			{
				_selectionRectangle.MapAreaInfo = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo;
			}

			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				_selectionRectangle.DisplaySize = _vm.CanvasSize;
			}
		}

		private void SelectionRectangle_AreaSelected(object? sender, AreaSelectedEventArgs e)
		{
			//if (e.PerformDiagnostics && _vm.CurrentAreaColorAndCalcSettings != null)
			//{
			//	ReportFactorsVsSamplePointResolution(_vm.CurrentAreaColorAndCalcSettings.MapAreaInfo, e);
			//}

			_vm.UpdateMapViewZoom(e);
		}

		private void SelectionRectangle_ImageDragged(object? sender, ImageDraggedEventArgs e)
		{
			_vm.UpdateMapViewPan(e);
		}

		#endregion

	}
}

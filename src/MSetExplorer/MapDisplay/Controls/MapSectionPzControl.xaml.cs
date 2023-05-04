using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapSectionDisplayControl.xaml
	/// </summary>
	public partial class MapSectionPzControl : UserControl
	{
		#region Private Properties

		//private readonly static bool SHOW_BORDER = false;

		private IMapDisplayViewModel _vm;

		//private Canvas _canvas;

		//private SelectionRectangle _selectionRectangle;
		//private Border? _border;

		#endregion

		#region Constructor

		public MapSectionPzControl()
		{
			//_canvas = new Canvas();
			_vm = (IMapDisplayViewModel)DataContext;
			//_selectionRectangle = new SelectionRectangle(_canvas, new SizeDbl(), RMapConstants.BLOCK_SIZE);

			Loaded += MapSectionPzControl_Loaded;
			Unloaded += MapSectionPzControl_Unloaded;

			InitializeComponent();
		}

		private void MapSectionPzControl_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				Debug.WriteLine("The DataContext is null as the MapSectionDisplayControl is being loaded.");
				return;
			}
			else
			{
				//mainCanvas.SizeChanged += MainCanvas_SizeChanged;
				//_canvas = mainCanvas;
				//_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;

				_vm = (IMapDisplayViewModel)DataContext;
				_vm.ViewPortSize = PanAndZoomControl1.ViewPortSize;

				if (_vm.ZoomSliderFactory != null)
				{
					PanAndZoomControl1.ZoomSliderOwner = _vm.ZoomSliderFactory(PanAndZoomControl1);
				}

				BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;

				PanAndZoomControl1.ContentOffsetXChanged += PanAndZoomControl1_ContentOffsetXChanged;
				PanAndZoomControl1.ContentOffsetYChanged += PanAndZoomControl1_ContentOffsetYChanged;

				//_vm.PropertyChanged += MapDisplayViewModel_PropertyChanged;

				//_selectionRectangle = new SelectionRectangle(_canvas, _vm.ViewPortSize, _vm.BlockSize);

				//_selectionRectangle.AreaSelected += SelectionRectangle_AreaSelected;
				//_selectionRectangle.ImageDragged += SelectionRectangle_ImageDragged;

				// A border is helpful for troubleshooting.
				//_border = SHOW_BORDER && (!CLIP_IMAGE_BLOCKS) ? BuildBorder(_canvas) : null;
				//_border = null;

				Debug.WriteLine("The MapSectionDisplay Control is now loaded");
			}
		}

		private void MapSectionPzControl_Unloaded(object sender, RoutedEventArgs e)
		{
			PanAndZoomControl1.ZoomSliderOwner = null;

			BitmapGridControl1.ViewPortSizeChanged -= BitmapGridControl1_ViewPortSizeChanged;

			PanAndZoomControl1.ContentOffsetXChanged -= PanAndZoomControl1_ContentOffsetXChanged;
			PanAndZoomControl1.ContentOffsetYChanged -= PanAndZoomControl1_ContentOffsetYChanged;

			//_vm.PropertyChanged -= MapDisplayViewModel_PropertyChanged;

			//if (!(_selectionRectangle is null))
			//{
			//	_selectionRectangle.AreaSelected -= SelectionRectangle_AreaSelected;
			//	_selectionRectangle.ImageDragged -= SelectionRectangle_ImageDragged;
			//	_selectionRectangle.TearDown();
			//}

			//mainCanvas.SizeChanged -= MainCanvas_SizeChanged;
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
		//		PanAndZoomControl1.ViewPortSizeChanged += value;
		//	}
		//	remove
		//	{
		//		PanAndZoomControl1.ViewPortSizeChanged -= value;
		//	}
		//}

		//#endregion

		#region Event Handlers

		//private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		//{
		//	Debug.WriteLine($"The MainCanvas's size is being updated to {new SizeDbl(mainCanvas.ActualWidth, mainCanvas.ActualHeight)} ({new SizeDbl(mainCanvas.Width, mainCanvas.Height)}). The VM's ViewPortSize is {_vm.ViewPortSize}.");
		//	BitmapGridControl1.ViewPortSizeInternal = ScreenTypeHelper.ConvertToSizeDbl(e.NewSize);
		//}

		private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		{
			_vm.ViewPortSize = e.Item2;
		}

		//private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	var previousValue = e.Item1;
		//	var newValue = e.Item2;

		//	Debug.WriteLine($"The {nameof(MapSectionPzControl)} is handling the BitmapGridControl's ViewPort Size Changed. Prev: {previousValue}, New: {newValue}, , VM's ViewPortSize is {_vm.ViewPortSize}. The Canvas Size is {new SizeDbl(mainCanvas.ActualWidth, mainCanvas.ActualHeight)} ({new SizeDbl(mainCanvas.Width, mainCanvas.Height)}).");

		//	_vm.ViewPortSize = newValue;
		//}

		//private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	_vm.ViewPortSize = e.Item2;
		//}

		//private void PanAndZoomControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	//mainCanvas.Width = e.Item2.Width;
		//	//mainCanvas.Height = e.Item2.Height;

		//	var previousValue = e.Item1;
		//	var newValue = e.Item2;

		//	Debug.WriteLine($"The {nameof(MapSectionPzControl)} is handling the MapSectionPzControl's ViewPort Size Changed. Prev: {previousValue}, New: {newValue}, VM's ViewPortSize is {_vm.ViewPortSize}. The Canvas Size is {new SizeDbl(mainCanvas.ActualWidth, mainCanvas.ActualHeight)} ({new SizeDbl(mainCanvas.Width, mainCanvas.Height)}).");

		//	BitmapGridControl1.ViewPortSizeInternal = newValue;
		//	//BitmapGridControl1.ViewPortSize = newValue;
		//}

		private void PanAndZoomControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			_vm.VerticalPosition = PanAndZoomControl1.ContentOffsetY;
		}

		private void PanAndZoomControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			_vm.HorizontalPosition = PanAndZoomControl1.ContentOffsetX;
		}

		//private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		//{
		//	if (e.PropertyName == nameof(IMapDisplayViewModel.ViewPortSize))
		//	{
		//		_selectionRectangle.DisplaySize = _vm.ViewPortSize;
		//	}
		//	else if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings))
		//	{
		//		_selectionRectangle.IsEnabled = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo != null;

		//		// Just for Diagnostics
		//		_selectionRectangle.MapAreaInfo = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo;
		//	}
		//}

		//private void SelectionRectangle_AreaSelected(object? sender, AreaSelectedEventArgs e)
		//{
		//	_vm.UpdateMapViewZoom(e);
		//}

		//private void SelectionRectangle_ImageDragged(object? sender, ImageDraggedEventArgs e)
		//{
		//	_vm.UpdateMapViewPan(e);
		//}

		#endregion
	}
}

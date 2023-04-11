using MSetExplorer.XPoc;
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
	public partial class MapSectionDisplayControl : UserControl
	{
		#region Private Properties

		//private readonly static bool SHOW_BORDER = false;
		private readonly static bool CLIP_IMAGE_BLOCKS = true;

		private IMapDisplayViewModel _vm;

		private Canvas _canvas;
		//private Image _image;
		//private VectorInt _offset;
		//private double _offsetZoom;

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
				_vm = (IMapDisplayViewModel)DataContext;
				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				//_canvas.Children.Add(_vm.Image);

				_vm.CanvasSizeInBlocks = BitmapGridControl1.ViewPortSizeInBlocks;
				_vm.CanvasSize = ScreenTypeHelper.ConvertToSizeDbl(BitmapGridControl1.ViewPortSize);

				_vm.PropertyChanged += ViewModel_PropertyChanged;

				BitmapGridControl1.ViewPortSizeInBlocksChanged += OnViewPortSizeInBlocksChanged;
				BitmapGridControl1.ViewPortSizeChanged += OnViewPortSizeChanged;


				_selectionRectangle = new SelectionRectangle(_canvas, _vm, _vm.BlockSize);
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
			BitmapGridControl1.ViewPortSizeInBlocksChanged -= OnViewPortSizeInBlocksChanged;
			BitmapGridControl1.ViewPortSizeChanged -= OnViewPortSizeChanged;

			if (!(_selectionRectangle is null))
			{
				_selectionRectangle.AreaSelected -= SelectionRectangle_AreaSelected;
				_selectionRectangle.ImageDragged -= SelectionRectangle_ImageDragged;
				_selectionRectangle.TearDown();
			}

			if (!(_vm is null))
			{
				_vm.PropertyChanged -= ViewModel_PropertyChanged;
				_vm.Dispose();
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

		//public event EventHandler<ValueTuple<SizeInt, SizeInt>>? ViewPortSizeInBlocksChanged
		//{
		//	add
		//	{
		//		BitmapGridControl1.ViewPortSizeInBlocksChanged += value;
		//	}
		//	remove
		//	{
		//		BitmapGridControl1.ViewPortSizeInBlocksChanged -= value;
		//	}
		//}

		//#endregion

		#region Event Handlers

		private void OnViewPortSizeInBlocksChanged(object? sender, (SizeInt, SizeInt) e)
		{
			var previousSizeInBlocks = e.Item1;
			var newSizeInBlocks = e.Item2;
			Debug.WriteLine($"The {nameof(MapSectionDisplayControl)} is handling ViewPort SizeInBlocks Changed. Prev: {previousSizeInBlocks}, New: {newSizeInBlocks}.");

			_vm.CanvasSizeInBlocks = newSizeInBlocks;
		}

		private void OnViewPortSizeChanged(object? sender, (Size, Size) e)
		{
			var previousSize = e.Item1;
			var newSize = e.Item2;
	
			Debug.WriteLine($"The {nameof(MapSectionDisplayControl)} is handling ViewPort Size Changed. Prev: {previousSize}, New: {newSize}.");

			_vm.CanvasSize = ScreenTypeHelper.ConvertToSizeDbl(newSize);
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//if (e.PropertyName is nameof(IMapDisplayViewModel.DisplayZoom))
			//{
			//	var scale = new PointDbl(_vm.DisplayZoom, _vm.DisplayZoom);
			//	SetCanvasTransform(scale);
			//}

			// Enable SelectionRectangle when ever we have a non-null "Job."
			if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings))
			{
				if (_selectionRectangle != null)
				{
					_selectionRectangle.Enabled = _vm.CurrentAreaColorAndCalcSettings != null;
				}
			}

			else if (e.PropertyName == nameof(IMapDisplayViewModel.Bitmap))
			{
				BitmapGridControl1.Image.Source = _vm.Bitmap;
			}

			else if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasControlOffset))
			{
				BitmapGridControl1.CanvasControlOffset = _vm.CanvasControlOffset;
			}
		}

		private void SelectionRectangle_AreaSelected(object? sender, AreaSelectedEventArgs e)
		{
			_vm.UpdateMapViewZoom(e);
		}

		private void SelectionRectangle_ImageDragged(object? sender, ImageDraggedEventArgs e)
		{
			_vm.UpdateMapViewPan(e);
		}

		#endregion
	}
}

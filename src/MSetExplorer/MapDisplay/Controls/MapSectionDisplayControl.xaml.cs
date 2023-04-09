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
			//_image = new Image();

			//_offset = new VectorInt(-1, -1);
			//_offsetZoom = 1;

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
				//_image = BitmapGridControl1.Image;

				_vm = (IMapDisplayViewModel)DataContext;

				_canvas.Children.Add(_vm.Image);
				_vm.CanvasSizeInBlocks = BitmapGridControl1.ViewPortInBlocks;

				//var ourSize = new SizeDbl(ActualWidth, ActualHeight);
				//var ourSize = BitmapGridControl1.ViewPort;
				var ourSize = BitmapGridControl1.ContainerSize;

				//_vm.ContainerSize = ourSize;
				_vm.CanvasSize = ourSize;
				_vm.LogicalDisplaySize = ourSize;

				//_image.Source = _vm.Bitmap;

				_vm.PropertyChanged += ViewModel_PropertyChanged;

				BitmapGridControl1.ViewPortSizeInBlocksChanged += OnViewPortSizeInBlocksChanged;
				BitmapGridControl1.ViewPortSizeChanged += OnViewPortSizeChanged;

				_canvas.ClipToBounds = CLIP_IMAGE_BLOCKS;
				ReportSizes("Loading");

				//_image.SetValue(Panel.ZIndexProperty, 5);
				//_image.SetValue(Canvas.LeftProperty, 0d);
				//_image.SetValue(Canvas.RightProperty, 0d);

				_selectionRectangle = new SelectionRectangle(_canvas, _vm, _vm.BlockSize);
				_selectionRectangle.AreaSelected += SelectionRectangle_AreaSelected;
				_selectionRectangle.ImageDragged += SelectionRectangle_ImageDragged;

				// A border is helpful for troubleshooting.
				//_border = SHOW_BORDER && (!CLIP_IMAGE_BLOCKS) ? BuildBorder(_canvas) : null;
				//_border = null;

				//SetCanvasOffset(new VectorInt(), 1);

				ReportSizes("Loaded.");
				Debug.WriteLine("The MapSectionDisplay Control is now loaded");
			}
		}

		private void MapSectionDisplayControl_Unloaded(object sender, RoutedEventArgs e)
		{
			//SizeChanged -= MapDisplay_SizeChanged;
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

			ReportSizes("Display Sized Changed");

			var ourSize = ScreenTypeHelper.ConvertToSizeDbl(newSize);

			//_vm.ContainerSize = ourSize;
			_vm.LogicalDisplaySize = ourSize;
			_vm.CanvasSize = ourSize;

			ReportSizes("After Updating VM with new Sizes");
		}

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//// Bitmap 
			//if (e.PropertyName is nameof(IMapDisplayViewModel.Bitmap))
			//{
			//	_image.Source = _vm.Bitmap;
			//}

			//// Canvas Control Offset
			//if (e.PropertyName is nameof(IMapDisplayViewModel.CanvasControlOffset))
			//{
			//	SetCanvasOffset(_vm.CanvasControlOffset, _vm.DisplayZoom);
			//}

			//if (e.PropertyName is nameof(IMapDisplayViewModel.DisplayZoom))
			//{
			//	var scale = new PointDbl(_vm.DisplayZoom, _vm.DisplayZoom);
			//	SetCanvasTransform(scale);
			//}

			// Enable SelectionRectangle when ever we have a non-null "Job."
			if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings) && _selectionRectangle != null)
			{
				_selectionRectangle.Enabled = _vm.CurrentAreaColorAndCalcSettings != null;
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

		#region Private Methods

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
			var bmgcSize = new SizeInt(BitmapGridControl1.ActualWidth, BitmapGridControl1.ActualHeight);

			//var cSize = new SizeInt(MainCanvas.ActualWidth, MainCanvas.ActualHeight);
			var cSize = new SizeInt();

			//var iSize = new SizeInt(myImage.ActualWidth, myImage.ActualHeight);
			var iSize = new SizeInt();

			//var bSize = _vm == null ? new SizeInt() : new SizeInt(_vm.Bitmap.Width, _vm.Bitmap.Height);
			var bSize = new SizeInt();

			Debug.WriteLine($"At {label}, the sizes are BmGrid: {bmgcSize}, Canvas: {cSize}, Image: {iSize}, Bitmap: {bSize}.");
		}

		#endregion
	}
}

using MSS.Types;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapDisplay.xaml
	/// </summary>
	public partial class MapDisplayControl : UserControl
	{
		private bool _showBorder;
		private bool _clipImageBlocks;

		private IMapDisplayViewModel _vm;
		
		private Canvas _canvas;
		private VectorInt _offset;
		private double _offsetZoom;

		private SelectionRectangle? _selectionRectangle;
		private Border? _border;

		#region Constructor

		public MapDisplayControl()
		{
			_canvas = new Canvas();
		
			_showBorder = false;
			_clipImageBlocks = true;
			_offset = new VectorInt(-1, -1);
			_offsetZoom = 1;
			
			_vm = (IMapDisplayViewModel)DataContext;

			Loaded += MapDisplay_Loaded;
			Initialized += MapDisplayControl_Initialized;
			Unloaded += MapDisplayControl_Unloaded;
			InitializeComponent();
		}

		private void MapDisplayControl_Initialized(object? sender, EventArgs e)
		{
			Debug.WriteLine("The MapDisplayControl is initialized.");
		}

		private void MapDisplay_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the MapDisplay UserControl is being loaded.");
				return;
			}
			else
			{
				_canvas = MainCanvas;
				_vm = (IMapDisplayViewModel) DataContext;

				_vm.PropertyChanged += ViewModel_PropertyChanged;
				SizeChanged += MapDisplay_SizeChanged;

				_canvas.ClipToBounds = _clipImageBlocks;

				ReportSizes("Loading");

				myImage.SetValue(Panel.ZIndexProperty, 5);
				myImage.SetValue(Canvas.LeftProperty, 0d);
				myImage.SetValue(Canvas.RightProperty, 0d);

				_selectionRectangle = new SelectionRectangle(_canvas, _vm, _vm.BlockSize);
				_selectionRectangle.AreaSelected += SelectionRectangle_AreaSelected;
				_selectionRectangle.ImageDragged += SelectionRectangle_ImageDragged;

				// A border is helpful for troubleshooting.
				_border = _showBorder && (!_clipImageBlocks) ? BuildBorder(_canvas) : null;

				SetCanvasOffset(new VectorInt(), 1);

				UpdateTheVmWithOurSize(new SizeDbl(ActualWidth, ActualHeight));

				Debug.WriteLine("The MapDisplay Control is now loaded.");
			}
		}

		private Border BuildBorder(Canvas canvas)
		{
			var result = new Border
			{
				Width = canvas.Width + 4,
				Height = canvas.Width + 4,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				BorderThickness = new Thickness(1),
				BorderBrush = Brushes.BlueViolet,
				Visibility = Visibility.Visible
			};

			_ = canvas.Children.Add(result);
			result.SetValue(Canvas.LeftProperty, -2d);
			result.SetValue(Canvas.TopProperty, -2d);
			result.SetValue(Panel.ZIndexProperty, 100);

			return result;
		}

		private void MapDisplayControl_Unloaded(object sender, RoutedEventArgs e)
		{
			SizeChanged -= MapDisplay_SizeChanged;

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

		#endregion

		#region Event Handlers

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName is nameof(IMapDisplayViewModel.CanvasControlOffset))
			{
				SetCanvasOffset(_vm.CanvasControlOffset, _vm.DisplayZoom);
			}

			if (e.PropertyName is nameof(IMapDisplayViewModel.DisplayZoom))
			{
				var scale = new PointDbl(_vm.DisplayZoom, _vm.DisplayZoom);
				SetCanvasTransform(scale);
			}

			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				ReportSizes("Canvas Size Changing");
				UpdateTheCanvasSize(_vm.CanvasSize.Round());
				ReportSizes("Canvas Sized Changed");
			}

			else if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings) && _selectionRectangle != null)
			{
				_selectionRectangle.Enabled = _vm.CurrentAreaColorAndCalcSettings != null;
			}
		}

		private void MapDisplay_SizeChanged(object? sender, SizeChangedEventArgs e)
		{
			//Debug.WriteLine($"The MapDisplay Size is changing. The new size is {e.NewSize}, the old size is {e.PreviousSize}");

			ReportSizes("Display Sized Changed");
			UpdateTheVmWithOurSize(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize));
			ReportSizes("After Updating VM with new Sizes");
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

		private void UpdateTheVmWithOurSize(SizeDbl size)
		{
			if (_border != null)
			{
				size = size.Inflate(8);
			}

			_vm.ContainerSize = size;
		}

		private void UpdateTheCanvasSize(SizeInt size)
		{
			_canvas.Width = size.Width;
			_canvas.Height = size.Height;

			if (_border != null)
			{
				_border.Width = size.Width + 4;
				_border.Height = size.Height + 4;
			}
		}

		/// <summary>
		/// The position of the canvas' origin relative to the Image Block Data
		/// </summary>
		private void SetCanvasOffset(VectorInt value, double displayZoom)
		{
			if (value != _offset || Math.Abs(displayZoom - _offsetZoom) > 0.001)
			{
				//Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}. The ScreenCollection Index is {_vm.ScreenCollectionIndex}");
				Debug.WriteLine($"CanvasOffset is being set to {value} with zoom: {displayZoom}.");
				Debug.Assert(value.X >= 0 && value.Y >= 0, "Setting offset to negative value.");

				_offset = value;
				_offsetZoom = displayZoom;

				// For a postive offset, we "pull" the image down and to the left.
				var invertedOffset = value.Invert();

				var scaledInvertedOffset = invertedOffset.Scale(1/displayZoom);

				myImage.SetValue(Canvas.LeftProperty, (double)scaledInvertedOffset.X);
				myImage.SetValue(Canvas.BottomProperty, (double)scaledInvertedOffset.Y);

				ReportSizes("SetCanvasOffset");
			}

			//_vm.ClipRegion = new SizeDbl(
			//	(double)_mapDisplayImage.GetValue(Canvas.LeftProperty),
			//	(double)_mapDisplayImage.GetValue(Canvas.BottomProperty)
			//	);
		}

		private void SetCanvasTransform(PointDbl scale)
		{
			_canvas.RenderTransformOrigin = new Point(0.5, 0.5);
			_canvas.RenderTransform = new ScaleTransform(scale.X, scale.Y);
		}

		private void ReportSizes(string label)
		{
			var cSize = new SizeInt(_canvas.Width, _canvas.Height);
			var iSize = new SizeInt(myImage.ActualWidth, myImage.ActualHeight);
			//var bSize = new SizeInt(_vm.Bitmap.Width, _vm.Bitmap.Height);

			var bSize = new SizeInt();
			Debug.WriteLine($"At {label}, the sizes are Canvas: {cSize}, Image: {iSize}, Bitmap: {bSize}.");
		}

		#endregion
	}
}

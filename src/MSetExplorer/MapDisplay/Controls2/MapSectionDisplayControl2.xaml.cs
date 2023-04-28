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
	public partial class MapSectionDisplayControl2 : UserControl
	{
		#region Private Properties

		//private readonly static bool SHOW_BORDER = false;
		private readonly static bool CLIP_IMAGE_BLOCKS = true;

		private IMapDisplayViewModel2 _vm;

		private Canvas _canvas;

		private SelectionRectangle? _selectionRectangle;
		//private Border? _border;

		#endregion

		#region Constructor

		public MapSectionDisplayControl2()
		{
			_canvas = new Canvas();
			_vm = (IMapDisplayViewModel2)DataContext;

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

				_vm = (IMapDisplayViewModel2)DataContext;
				_vm.ViewPortSize = BitmapGridControl1.ViewPortSize;

				BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;
				BitmapGridControl1.ContentOffsetXChanged += BitmapGridControl1_ContentOffsetXChanged;
				BitmapGridControl1.ContentOffsetYChanged += BitmapGridControl1_ContentOffsetYChanged;

				_vm.PropertyChanged += MapDisplayViewModel_PropertyChanged;

				_selectionRectangle = new SelectionRectangle(_canvas, _vm.ViewPortSize, _vm.BlockSize);
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
			BitmapGridControl1.ViewPortSizeChanged -= BitmapGridControl1_ViewPortSizeChanged;
			BitmapGridControl1.ContentOffsetXChanged -= BitmapGridControl1_ContentOffsetXChanged;
			BitmapGridControl1.ContentOffsetYChanged -= BitmapGridControl1_ContentOffsetYChanged;

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

		private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		{
			Debug.WriteLine($"The {nameof(MapSectionDisplayControl)} is handling ViewPort Size Changed. Prev: {e.Item1}, New: {e.Item2}.");

			_vm.ViewPortSize = BitmapGridControl1.ViewPortSize;

			if (_selectionRectangle != null)
			{
				_selectionRectangle.DisplaySize = _vm.ViewPortSize;
			}
		}

		private void BitmapGridControl1_ContentOffsetYChanged(object? sender, EventArgs e)
		{
			_vm.VerticalPosition = BitmapGridControl1.ContentOffsetY;
		}

		private void BitmapGridControl1_ContentOffsetXChanged(object? sender, EventArgs e)
		{
			_vm.HorizontalPosition = BitmapGridControl1.ContentOffsetX;
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == nameof(IMapDisplayViewModel2.PosterSize))
			//{
			//	BitmapGridControl1.UnscaledExtent = ScreenTypeHelper.ConvertToSize(_vm.PosterSize ?? new SizeInt());
			//}

			// TODO: Only for diagnostics
			if (e.PropertyName == nameof(IMapDisplayViewModel2.CurrentAreaColorAndCalcSettings) && _selectionRectangle != null)
			{
				_selectionRectangle.MapAreaInfo = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo;
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

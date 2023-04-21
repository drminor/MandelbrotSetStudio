﻿using MSetExplorer.XPoc;
using MSS.Types;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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

			this.LayoutUpdated += MapSectionDisplayControl_LayoutUpdated;

			InitializeComponent();
		}

		private void MapSectionDisplayControl_LayoutUpdated(object? sender, System.EventArgs e)
		{
			Debug.WriteLine("MapSectionDisplayControl_LayoutUpdated.");
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

				BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;

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
			BitmapGridControl1.ViewPortSizeChanged -= BitmapGridControl1_ViewPortSizeChanged;

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
			Debug.WriteLine($"The {nameof(BitmapGridTestWindow)} is handling ViewPort Size Changed. Prev: {e.Item1}, New: {e.Item2}.");

			BitmapGridControl1.ReportSizes("ViewPortSizeChanged");
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

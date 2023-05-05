using MSS.Types;
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
				_vm.ViewPortSize = BitmapGridControl1.ViewPortSize;
				_vm.PropertyChanged += MapDisplayViewModel_PropertyChanged;

				BitmapGridControl1.ViewPortSizeChanged += BitmapGridControl1_ViewPortSizeChanged;

				var canvas = BitmapGridControl1.Canvas;
				_selectionRectangle = new SelectionRectangle(canvas, _vm.ViewPortSize, _vm.BlockSize);
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

		#region Event Handlers

		//private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		//{
		//	_vm.ViewPortSize = e.Item2;
		//}

		private void BitmapGridControl1_ViewPortSizeChanged(object? sender, (SizeDbl, SizeDbl) e)
		{
			var previousValue = e.Item1;
			var newValue = e.Item2;

			Debug.WriteLine($"The {nameof(MapSectionDisplayControl)} is handling ViewPort Size Changed. Prev: {previousValue}, New: {newValue}, CurrentVM: {_vm.ViewPortSize}.");

			_vm.ViewPortSize = newValue;
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.ViewPortSize))
			{
				_selectionRectangle.DisplaySize = _vm.ViewPortSize;
			}
			else if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentAreaColorAndCalcSettings))
			{
				_selectionRectangle.IsEnabled = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo != null;

				// Just for Diagnostics
				_selectionRectangle.MapAreaInfo = _vm.CurrentAreaColorAndCalcSettings?.MapAreaInfo;
			}
		}

		private void SelectionRectangle_AreaSelected(object? sender, AreaSelectedEventArgs e)
		{
			_vm.RaiseMapViewZoomUpdate(e);
		}

		private void SelectionRectangle_ImageDragged(object? sender, ImageDraggedEventArgs e)
		{
			_vm.RaiseMapViewPanUpdate(e);
		}

		#endregion
	}
}

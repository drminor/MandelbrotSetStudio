using MSS.Types;
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
		private Image _mapDisplayImage;
		private SelectionRectangle? _selectionRectangle;
		private Border? _border;

		#region Constructor

		public MapDisplayControl()
		{
			_canvas = new Canvas();
			_mapDisplayImage = new Image();

			_vm = (IMapDisplayViewModel)DataContext;

			Loaded += MapDisplay_Loaded;
			InitializeComponent();
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
				_showBorder = false;
				_clipImageBlocks = true;

				_canvas = MainCanvas;
				_vm = (IMapDisplayViewModel) DataContext;

				UpdateTheVmWithOurSize(new SizeDbl(ActualWidth, ActualHeight));

				_vm.PropertyChanged += ViewModel_PropertyChanged;
				SizeChanged += MapDisplay_SizeChanged;

				_canvas.ClipToBounds = _clipImageBlocks;
				_mapDisplayImage = new Image { Source = _vm.ImageSource };
				_ = _canvas.Children.Add(_mapDisplayImage);
				CanvasOffset = new VectorInt();
				_mapDisplayImage.SetValue(Panel.ZIndexProperty, 5);

				_selectionRectangle = new SelectionRectangle(_canvas, _vm.BlockSize);
				_selectionRectangle.AreaSelected += SelectionRectangle_AreaSelected;
				_selectionRectangle.ImageDragged += SelectionRectangle_ImageDragged;

				_border = _showBorder && (!_clipImageBlocks) ? BuildBorder(_canvas) : null;

				Debug.WriteLine("The MapDisplay is now loaded.");
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

		#endregion

		#region Event Handlers

		private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasControlOffset))
			{
				CanvasOffset = _vm.CanvasControlOffset;
			}

			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				UpdateTheCanvasSize(_vm.CanvasSize);
			}
		}

		private void MapDisplay_SizeChanged(object? sender, SizeChangedEventArgs e)
		{
			UpdateTheVmWithOurSize(ScreenTypeHelper.ConvertToSizeDbl(e.NewSize));
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
			if (!(_border is null))
			{
				size = size.Inflate(8);
			}

			_vm.ContainerSize = size;
		}

		private void UpdateTheCanvasSize(SizeInt size)
		{
			_canvas.Width = size.Width;
			_canvas.Height = size.Height;

			if (!(_border is null))
			{
				_border.Width = size.Width + 4;
				_border.Height = size.Height + 4;
			}
		}

		#endregion

		#region Private Properties

		/// <summary>
		/// The position of the canvas' origin relative to the Image Block Data
		/// </summary>
		private VectorInt CanvasOffset
		{
			get
			{
				var l = (double)_mapDisplayImage.GetValue(Canvas.LeftProperty);
				var b = (double)_mapDisplayImage.GetValue(Canvas.BottomProperty);
				var pointDbl = new PointDbl(l, b);

				return new VectorInt(pointDbl.Round()).Invert();
			}

			set
			{
				var curVal = CanvasOffset;
				if (value != curVal)
				{
					Debug.WriteLine($"CanvasOffset is being set to {value}.");
					Debug.Assert(value.X >= 0 && value.Y >= 0, "Setting offset to negative value.");

					var inverted = value.Invert();
					_mapDisplayImage.SetValue(Canvas.LeftProperty, (double) inverted.X);
					_mapDisplayImage.SetValue(Canvas.BottomProperty, (double) inverted.Y);
				}
			}
		}

		#endregion
	}
}

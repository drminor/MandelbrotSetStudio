using MSetExplorer.MapWindow;
using MSS.Common;
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
	public partial class MapDisplay : UserControl
	{
		private static readonly bool _showBorder;
		private static readonly bool _clipImageBlocks = true;
		private static readonly bool _keepDisplaySquare = true;

		private IMapDisplayViewModel _vm;
		private Canvas _canvas;
		private Image _mapDisplayImage;
		private SelectionRectangle _selectionRectangle;
		private Border _border;

		#region Constructor

		public MapDisplay()
		{
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
				_canvas = MainCanvas;
				var vmProvider = (IMainWindowViewModel)DataContext;
				_vm = vmProvider.MapDisplayViewModel;
				_vm.PropertyChanged += ViewModel_PropertyChanged;

				vmProvider.PropertyChanged += MainWindow_PropertyChanged;

				SetCanvasSize(new Size(ActualWidth, ActualHeight));

				_canvas.ClipToBounds = _clipImageBlocks;
				SizeChanged += MapDisplay_SizeChanged;

				_mapDisplayImage = new Image { Source = _vm.ImageSource };
				_ = _canvas.Children.Add(_mapDisplayImage);
				_mapDisplayImage.SetValue(Canvas.LeftProperty, 0d);
				_mapDisplayImage.SetValue(Canvas.BottomProperty, 0d);
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

		private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(_vm.CanvasControlOffset))
			{
				CanvasOffset = _vm.CanvasControlOffset;
			}
		}

		private void MapDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			SetCanvasSize(e.NewSize);
		}

		private void SelectionRectangle_AreaSelected(object sender, AreaSelectedEventArgs e)
		{
			_vm.UpdateMapViewZoom(e);
		}

		private void SelectionRectangle_ImageDragged(object sender, ImageDraggedEventArgs e)
		{
			_vm.UpdateMapViewPan(e);
		}

		// TODO: Use Dependency Property to Bind to the CurrentProject
		private void MainWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CurrentProject")
			{
				_vm.CurrentProject = ((IMainWindowViewModel)DataContext).CurrentProject;
			}
		}

		#endregion

		#region Private Methods

		private void SetCanvasSize(Size controlSize)
		{
			var size = GetCanvasSize(controlSize, _keepDisplaySquare);

			Debug.WriteLine($"The MapDisplay size is now {controlSize}. Setting the canvas size to {size}.");

			_canvas.Width = size.Width;
			_canvas.Height = size.Height;

			if (!(_border is null))
			{
				_border.Width = size.Width + 4;
				_border.Height = size.Height + 4;
			}

			_vm.SetCanvasSize(size);
		}

		private SizeInt GetCanvasSize(Size size, bool makeSquare)
		{
			var sizeDbl = new SizeDbl(size.Width, size.Height);

			if (!(_border is null))
			{
				sizeDbl = sizeDbl.Inflate(8);
			}

			var canvasSizeInWholeBlocks = RMapHelper.GetCanvasSizeWholeBlocks(sizeDbl, _vm.BlockSize, makeSquare);
			var result = canvasSizeInWholeBlocks.Scale(_vm.BlockSize);

			return result;
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
				var pointDbl = new PointDbl(
					(double)_mapDisplayImage.GetValue(Canvas.LeftProperty),
					(double)_mapDisplayImage.GetValue(Canvas.BottomProperty)
					);

				return new VectorInt(pointDbl.Round()).Invert();
			}

			set
			{
				var curVal = CanvasOffset;
				if (value != curVal)
				{
					Debug.WriteLine($"CanvasOffset is being set to {value}.");
					var offset = value.Invert();
					_mapDisplayImage.SetValue(Canvas.LeftProperty, (double)offset.X);
					_mapDisplayImage.SetValue(Canvas.BottomProperty, (double)offset.Y);
				}
			}
		}

		#endregion
	}
}

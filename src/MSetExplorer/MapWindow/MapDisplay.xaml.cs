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
		private static readonly bool _showBorder = true;
		private static readonly bool _clipImageBlocks = false;
		private static readonly bool _keepDisplaySquare = true;

		private IMapDisplayViewModel _vm;
		private Canvas _canvas;
		private Image _mapDisplayImage;
		private SelectionRectangle _selectedArea;
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
				//_vm.PropertyChanged += ViewModel_PropertyChanged;

				vmProvider.PropertyChanged += MainWindow_PropertyChanged;

				var canvasSize = GetCanvasSize(new Size(ActualWidth, ActualHeight), _keepDisplaySquare);
				_vm.CanvasSize = canvasSize;
				SetCanvasSize(canvasSize);

				_canvas.ClipToBounds = _clipImageBlocks;
				SizeChanged += MapDisplay_SizeChanged;

				_mapDisplayImage = new Image { Source = _vm.ImageSource };
				_ = _canvas.Children.Add(_mapDisplayImage);
				_mapDisplayImage.SetValue(Canvas.LeftProperty, 0d);
				_mapDisplayImage.SetValue(Canvas.BottomProperty, 0d);
				_mapDisplayImage.SetValue(Panel.ZIndexProperty, 5);

				_selectedArea = new SelectionRectangle(_canvas, _vm.BlockSize);
				_selectedArea.AreaSelected += SelectedArea_AreaSelected;
				_selectedArea.ScreenPanned += SelectedArea_ScreenPanned;

				_border = _showBorder && (!_clipImageBlocks) ? BuildBorder(_canvas, canvasSize) : null;

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
		}

		private Border BuildBorder(Canvas canvas, SizeInt canvasSize)
		{
			var result = new Border
			{
				Width = canvasSize.Width + 4,
				Height = canvasSize.Width + 4,
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

		private void MainWindow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == "TestingScreenSections")
			//{
			//	_mapSectionCollectionBinder.Test();
			//}

			if (e.PropertyName == "CurrentProject")
			{
				_vm.CurrentProject = ((IMainWindowViewModel)DataContext).CurrentProject;
			}
		}

		private void MapDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var canvasSize = GetCanvasSize(e.NewSize, _keepDisplaySquare);
			_vm.CanvasSize = canvasSize;
			SetCanvasSize(canvasSize);
		}

		private void SelectedArea_AreaSelected(object sender, AreaSelectedEventArgs e)
		{
			_vm.UpdateMapViewZoom(e);
		}

		private void SelectedArea_ScreenPanned(object sender, ScreenPannedEventArgs e)
		{
			_vm.UpdateMapViewPan(e);
		}

		#endregion

		#region Private Methods

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

		private void SetCanvasSize(SizeInt value)
		{
			_canvas.Width = value.Width;
			_canvas.Height = value.Height;

			if (!(_border is null))
			{
				_border.Width = value.Width + 4;
				_border.Height = value.Height + 4;
			}
		}

		#endregion

		#region Private Properties

		///// <summary>
		///// The position of the canvas' origin relative to the Image Block Data
		///// </summary>
		//private VectorInt CanvasOffset
		//{
		//	get
		//	{
		//		var pointDbl = new PointDbl(
		//			(double)_mapDisplayImage.GetValue(Canvas.LeftProperty),
		//			(double)_mapDisplayImage.GetValue(Canvas.BottomProperty)
		//			);

		//		return new VectorInt(pointDbl.Round()).Invert();
		//	}

		//	set
		//	{
		//		var curVal = CanvasOffset;
		//		if (value != curVal)
		//		{
		//			Debug.WriteLine($"CanvasOffset is being set to {value}.");
		//			var offset = value.Invert();
		//			_mapDisplayImage.SetValue(Canvas.LeftProperty, (double)offset.X);
		//			_mapDisplayImage.SetValue(Canvas.BottomProperty, (double)offset.Y);
		//		}
		//	}
		//}

		#endregion

	}
}

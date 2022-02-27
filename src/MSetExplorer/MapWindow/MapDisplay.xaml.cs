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
		private static readonly bool _showBorder = false;
		private static readonly bool _clipImageBlocks = true;
		private static readonly bool _keepDisplaySquare = true;

		private IMapDisplayViewModel _vm;

		private SelectionRectangle _selectedArea;
		private IMapSectionCollectionBinder _mapSectionCollectionBinder;
		private Border _border;

		internal event EventHandler<AreaSelectedEventArgs> AreaSelected;
		internal event EventHandler<ScreenPannedEventArgs> ScreenPanned;

		#region Constructor

		public MapDisplay()
		{
			//_selectedArea = null;
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
				var vmProvider = (IMainWindowViewModel)DataContext;
				_vm = vmProvider.MapDisplayViewModel;
				_vm.PropertyChanged += ViewModel_PropertyChanged;

				CanvasSize = GetCanvasSize(new Size(ActualWidth, ActualHeight), _keepDisplaySquare);

				MainCanvas.ClipToBounds = _clipImageBlocks;
				SizeChanged += MapDisplay_SizeChanged;

				_mapSectionCollectionBinder = new MapSectionCollectionBinder(MainCanvas, _vm.BlockSize, _vm.MapSections);

				var canvasControlOffset = _vm.CanvasControlOffset;
				_selectedArea = new SelectionRectangle(MainCanvas, canvasControlOffset, _vm.BlockSize);
				_selectedArea.AreaSelected += SelectedArea_AreaSelected;
				_selectedArea.ScreenPanned += SelectedArea_ScreenPanned;

				_border = _showBorder && (!_clipImageBlocks) ? BuildBorder(MainCanvas) : null;

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
		}

		private Border BuildBorder(Canvas canvas)
		{
			var result = new Border
			{
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
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
			if (e.PropertyName == "CanvasControlOffset")
			{
				var offset = _vm.CanvasControlOffset;
				_mapSectionCollectionBinder.CanvasOffset = offset;
				_selectedArea.CanvasControlOffset = offset;

				return;
			}
		}

		private void MapDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			CanvasSize = GetCanvasSize(e.NewSize, _keepDisplaySquare);
		}

		private void SelectedArea_AreaSelected(object sender, AreaSelectedEventArgs e)
		{
			AreaSelected?.Invoke(this, e);
		}

		private void SelectedArea_ScreenPanned(object sender, ScreenPannedEventArgs e)
		{
			ScreenPanned?.Invoke(this, e);
		}

		#endregion

		#region Dependency Properties

		public static readonly DependencyProperty CanvasSizeProperty = DependencyProperty.Register
			(
			"CanvasSize",
			typeof(SizeInt),
			typeof(MapDisplay), 
			new FrameworkPropertyMetadata(new SizeInt(), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, CanvasSizeChanged)
			);

		public SizeInt CanvasSize
		{
			get => (SizeInt)GetValue(CanvasSizeProperty);
			set { if (value != CanvasSize) { SetValue(CanvasSizeProperty, value); } }
		}

		private static void CanvasSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is MapDisplay x)
			{
				Debug.WriteLine("The CanvasSize is being set on the MapDisplay Control.");
				x.SetCanvasSize((SizeInt) e.NewValue);
			}
			else
			{
				Debug.WriteLine($"CanvasSizeChanged was raised from sender: {sender}");
			}
		}

		private SizeInt GetCanvasSize(Size size, bool makeSquare)
		{
			var sizeDbl = new SizeDbl(size.Width, size.Height);
			var canvasSizeInWholeBlocks = RMapHelper.GetCanvasSizeWholeBlocks(sizeDbl, _vm.BlockSize, makeSquare);
			var result = canvasSizeInWholeBlocks.Scale(_vm.BlockSize);

			return result;
		}

		private void SetCanvasSize(SizeInt value)
		{
			MainCanvas.Width = value.Width;
			MainCanvas.Height = value.Height;

			if (_showBorder && !(_border is null))
			{
				_border.Width = value.Width + 4;
				_border.Height = value.Height + 4;
			}
		}

		#endregion
	}
}

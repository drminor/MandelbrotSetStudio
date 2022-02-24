using MSetExplorer.MapWindow;
using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		private static readonly bool _clipImageBlocks = true;
		private static readonly bool _keepDisplaySquare = true;

		private IMapDisplayViewModel _vm;
		//private IMapLoaderJobStack _mapLoaderJobStack;
		//private Job _currentJob => _mapLoaderJobStack.CurrentJob;


		private SelectionRectangle _selectedArea;
		private IScreenSectionCollection _screenSections;
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

				var canvasControlOffset = _vm.CanvasControlOffset;
				CanvasSize = GetCanvasSize(new Size(ActualWidth, ActualHeight), _keepDisplaySquare);

				MainCanvas.ClipToBounds = _clipImageBlocks;
				SizeChanged += MapDisplay_SizeChanged;

				_screenSections = new ScreenSectionCollection(MainCanvas, _vm.BlockSize, _vm.MapSections);

				_selectedArea = new SelectionRectangle(MainCanvas, canvasControlOffset, _vm.BlockSize);
				_selectedArea.AreaSelected += SelectedArea_AreaSelected;
				_selectedArea.ScreenPanned += SelectedArea_ScreenPanned;

				_border = _showBorder ? BuildBorder(MainCanvas) : null;

				//_vm.MapSections.CollectionChanged += MapSections_CollectionChanged;

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
		}

		private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "CanvasControlOffset")
			{
				var offset = _vm.CanvasControlOffset;

				_screenSections.CanvasOffset = offset;
				_selectedArea.CanvasControlOffset = offset;

				return;
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
			result.SetValue(Canvas.LeftProperty, 2d);
			result.SetValue(Canvas.TopProperty, 2d);
			result.SetValue(Panel.ZIndexProperty, 100);

			return result;
		}

		#endregion

		#region Event Handlers

		private void MapDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var newCanvasSize = GetCanvasSize(e.NewSize, _keepDisplaySquare);
			var diff = CanvasSize.Diff(newCanvasSize).Abs();

			if (diff.Width > 0 || diff.Height > 0)
			{
				CanvasSize = newCanvasSize;
			}
		}

		private SizeInt GetCanvasSize(Size size, bool makeSquare)
		{
			var sizeDbl = new SizeDbl(size.Width, size.Height);
			var canvasSizeInWholeBlocks = RMapHelper.GetCanvasSizeWholeBlocks(sizeDbl, _vm.BlockSize, makeSquare);
			var result = canvasSizeInWholeBlocks.Scale(_vm.BlockSize);

			return result;
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
			set => SetValue(CanvasSizeProperty, value);
		}

		private static void CanvasSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is MapDisplay x)
			{
				Debug.WriteLine("The CanvasSize is being set on the MapDisplay Control.");
				x.SetCanvasSizeBackDoor((SizeInt) e.NewValue);
			}
			else
			{
				Debug.WriteLine($"CanvasSizeChanged was raised from sender: {sender}");
			}
		}

		private void SetCanvasSizeBackDoor(SizeInt value)
		{
			//_vm.CanvasSize = value;
			MainCanvas.Width = value.Width;
			MainCanvas.Height = value.Height;

			if (_showBorder && !(_border is null))
			{
				_border.Width = value.Width - 4;
				_border.Height = value.Height - 4;
			}
		}

		#endregion
	}
}

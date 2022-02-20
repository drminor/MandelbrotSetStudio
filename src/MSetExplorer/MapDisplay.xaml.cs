using MSetExplorer.MapWindow;
using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapDisplay.xaml
	/// </summary>
	public partial class MapDisplay : UserControl
	{
		private IMapJobViewModel _vm;
		private SelectionRectangle _selectedArea;
		private IScreenSectionCollection _screenSections;

		internal event EventHandler<AreaSelectedEventArgs> AreaSelected;
		internal event EventHandler<ScreenPannedEventArgs> ScreenPanned;

		#region Constructor

		public MapDisplay()
		{
			_selectedArea = null;
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
				_vm = (IMapJobViewModel)DataContext;
				var canvasControlOffset = _vm.CurrentJob?.CanvasControlOffset ?? new SizeDbl();

				CanvasSize = GetCanvasSize(new Size(ActualWidth, ActualHeight));
				SizeChanged += MapDisplay_SizeChanged;

				_vm.MapSections.CollectionChanged += MapSections_CollectionChanged;
				_screenSections = new ScreenSectionCollection(MainCanvas, _vm.BlockSize);

				_selectedArea = new SelectionRectangle(MainCanvas, canvasControlOffset, _vm.BlockSize);
				_selectedArea.AreaSelected += SelectedArea_AreaSelected;
				_selectedArea.ScreenPanned += SelectedArea_ScreenPanned;

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
		}

		private void MapDisplay_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			var newCanvasSize = GetCanvasSize(e.NewSize);
			var diff = CanvasSize.Diff(newCanvasSize).Abs();

			if (diff.Width > 0 || diff.Height > 0)
			{
				CanvasSize = newCanvasSize;
			}
		}

		private SizeInt GetCanvasSize(Size size)
		{
			var sizeDbl = new SizeDbl(size.Width, size.Height);
			var canvasSizeInWholeBlocks = RMapHelper.GetCanvasSizeWholeBlocks(sizeDbl, _vm.BlockSize);
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

		#region Map Sections

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				_screenSections.HideScreenSections();

				var offset = _vm.CurrentJob.CanvasControlOffset;
				_screenSections.CanvasOffset = offset;
				_selectedArea.CanvasControlOffset = offset;
			}
			else if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				IList<MapSection> newItems = e.NewItems is null ? new List<MapSection>() : e.NewItems.Cast<MapSection>().ToList();

				foreach(var mapSection in newItems)
				{
					//Debug.WriteLine($"Writing Pixels for section at {mapSection.CanvasPosition}.");
					_screenSections.Draw(mapSection);
				}
			}
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
			MainCanvas.Width = value.Width;
			MainCanvas.Height = value.Height;

			canvasBorder.Width = value.Width - 4;
			canvasBorder.Height = value.Height - 4;

			if (!(_vm is null))
			{
				_screenSections = new ScreenSectionCollection(MainCanvas, _vm.BlockSize);
			}
		}

		#endregion
	}
}

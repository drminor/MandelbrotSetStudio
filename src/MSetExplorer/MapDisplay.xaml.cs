using MSetExplorer.MapWindow;
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
				MainCanvas.SizeChanged += Canvas_SizeChanged;
				TriggerCanvasSizeUpdate();

				_vm = (IMapJobViewModel)DataContext;
				_vm.MapSections.CollectionChanged += MapSections_CollectionChanged;
				_screenSections = new ScreenSectionCollection(MainCanvas, _vm.BlockSize);

				var canvasControlOffset = _vm.CurrentJob?.CanvasControlOffset ?? new SizeDbl();

				_selectedArea = new SelectionRectangle(MainCanvas, canvasControlOffset, _vm.BlockSize);
				_selectedArea.AreaSelected += SelectedArea_AreaSelected;
				_selectedArea.ScreenPanned += SelectedArea_ScreenPanned;

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
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

		#region Canvas Handlers

		private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			TriggerCanvasSizeUpdate();
		}

		private void TriggerCanvasSizeUpdate()
		{
			CanvasSize = new SizeInt();
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
			get
			{
				var result = new SizeDbl(MainCanvas.ActualWidth, MainCanvas.ActualHeight).Round();
				return result;
			}
			
			// This property is not updatable. This is used to update any bindings that may have this as a target.
			set
			{
				var curSize = CanvasSize;
				SetValue(CanvasSizeProperty, curSize);
			}
		}

		private static void CanvasSizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			if (sender is MapDisplay)
			{
				Debug.WriteLine("The CanvasSize is being set on the MapDisplay Control.");
			}
			else
			{
				Debug.WriteLine($"CanvasSizeChanged was raised from sender: {sender}");
			}
		}

		#endregion
	}
}

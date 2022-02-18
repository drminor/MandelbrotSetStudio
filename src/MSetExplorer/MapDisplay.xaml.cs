﻿using MSetExplorer.MapWindow;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
				MainCanvas.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
				MainCanvas.SizeChanged += Canvas_SizeChanged;
				TriggerCanvasSizeUpdate();

				_vm = (IMapJobViewModel)DataContext;
				_vm.MapSections.CollectionChanged += MapSections_CollectionChanged;
				_screenSections = new ScreenSectionCollection(MainCanvas, _vm.BlockSize);
				_selectedArea = new SelectionRectangle(MainCanvas, _vm.BlockSize);

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
		}

		#endregion

		#region Map Sections

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				_screenSections.HideScreenSections();
				Position = new PointDbl(_vm.CurrentJob.CanvasControlOffset);
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

		#region Drag and Selection Logic

		private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: MainCanvas);

			if (_selectedArea.InDrag)
			{
				_selectedArea.InDrag = false;
				HandleDragComplete(controlPos);
			}
			else
			{
				HandleSelectionRect(controlPos);
			}
		}

		private void HandleDragComplete(Point controlPos)
		{
			var offset = _selectedArea.GetDragOffset(controlPos).Round();
			Debug.WriteLine($"We are handling a DragComplete with offset:{offset}.");

			ScreenPanned?.Invoke(this, new ScreenPannedEventArgs(TransformType.Pan, offset));
		}

		private void HandleSelectionRect(Point controlPos)
		{
			// The canvas has coordinates where the y value increases from top to bottom.
			var posYInverted = new Point(controlPos.X, MainCanvas.ActualHeight - controlPos.Y);

			// Get the center of the block on which the mouse is over.
			var blockPosition = _vm.GetBlockPosition(posYInverted);

			Debug.WriteLine($"The canvas is getting a Mouse Left Button Down at {controlPos}. ");

			if (!_selectedArea.IsActive)
			{
				_selectedArea.Activate(blockPosition);
			}
			else
			{
				if (_selectedArea.Contains(posYInverted))
				{
					Debug.WriteLine($"Will start job here with position: {blockPosition}.");

					// Add the Canvas Control Offset to convert from canvas to screen coordinates.
					var adjArea = _selectedArea.Area.Translate(Position);
					var adjAreaInt = adjArea.Round();

					_selectedArea.Deactivate();
					AreaSelected?.Invoke(this, new AreaSelectedEventArgs(TransformType.Zoom, adjAreaInt));
				}
			}
		}

		private PointDbl Position
		{
			get => _screenSections.Position.Scale(-1d);
			set => _screenSections.Position = value.Scale(-1d);
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

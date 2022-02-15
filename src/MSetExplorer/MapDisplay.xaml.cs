﻿using MSetExplorer.MapWindow;
using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for MapDisplay.xaml
	/// </summary>
	public partial class MapDisplay : UserControl
	{
		private IMapJobViewModel _vm;
		private SelectionRectangle _selectedArea;
		private IDictionary<PointInt, ScreenSection> _screenSections;

		private bool _inDrag;
		private Point _dragAnchor;
		private Line _dragLine;

		internal event EventHandler<AreaSelectedEventArgs> AreaSelected;

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

				_screenSections = BuildScreenSections(CanvasSize);

				_selectedArea = new SelectionRectangle(MainCanvas, _vm.BlockSize);

				_dragLine = AddDragLine();

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
		}

		private Dictionary<PointInt, ScreenSection> BuildScreenSections(SizeInt canvasSize)
		{
			var result = new Dictionary<PointInt, ScreenSection>();

			// Create the screen sections to cover the canvas
			// Include an additional block to accomodate when the CanvasControlOffset is non-zero.
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, _vm.BlockSize);
			for (var yBlockPtr = 0; yBlockPtr < canvasSizeInBlocks.Height + 1; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < canvasSizeInBlocks.Width + 1; xBlockPtr++)
				{
					var position = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = new ScreenSection(MainCanvas, _vm.BlockSize);
					result.Add(position, screenSection);
				}
			}

			return result;
		}

		private Line AddDragLine()
		{
			var dragLine = new Line()
			{
				Fill = Brushes.DarkGray,
				Stroke = Brushes.DarkGreen,
				StrokeThickness = 2,
				Visibility = Visibility.Hidden
			};

			_ = MainCanvas.Children.Add(dragLine);
			dragLine.SetValue(Panel.ZIndexProperty, 20);

			MainCanvas.MouseEnter += Canvas_MouseEnter;
			MainCanvas.MouseLeave += Canvas_MouseLeave;

			return dragLine;
		}

		#endregion

		#region Map Sections

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				HideScreenSections(MainCanvas);
			}
			else if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				IList<MapSection> newItems = e.NewItems is null ? new List<MapSection>() : e.NewItems.Cast<MapSection>().ToList();

				foreach(var mapSection in newItems)
				{
					//Debug.WriteLine($"Writing Pixels for section at {mapSection.CanvasPosition}.");
					var screenSection = GetScreenSection(MainCanvas, mapSection.BlockPosition, mapSection.Size);
					screenSection.Place(mapSection.CanvasPosition);
					screenSection.WritePixels(mapSection.Pixels1d);
				}
			}
		}

		private void HideScreenSections(Canvas canvas)
		{
			foreach (UIElement c in canvas.Children.OfType<Image>())
			{
				c.Visibility = Visibility.Hidden;
			}
		}

		private ScreenSection GetScreenSection(Canvas canvas, PointInt blockPosition, SizeInt blockSize)
		{
			if (!_screenSections.TryGetValue(blockPosition, out var screenSection))
			{
				screenSection = new ScreenSection(canvas, blockSize);
				_screenSections.Add(blockPosition, screenSection);
			}

			return screenSection;
		}

		#endregion

		#region Drag and Selection Logic

		private void MseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			_dragAnchor = e.GetPosition(relativeTo: MainCanvas);
		}

		private void MseMove(object sender, MouseEventArgs e)
		{
			if (e.LeftButton == MouseButtonState.Pressed)
			{
				var controlPos = e.GetPosition(relativeTo: MainCanvas);

				if (!_inDrag)
				{
					var dist = _dragAnchor - controlPos;
					if (Math.Abs(dist.Length) > 5)
					{
						_inDrag = true;
						_dragLine.Visibility = Visibility.Visible;
					}
				}

				if (_inDrag)
				{
					_dragLine.X1 = _dragAnchor.X;
					_dragLine.Y1 = _dragAnchor.Y;
					_dragLine.X2 = controlPos.X;
					_dragLine.Y2 = controlPos.Y;
				}
			}
		}

		private void MseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			var controlPos = e.GetPosition(relativeTo: MainCanvas);

			if (_inDrag)
			{
				_inDrag = false;
				_dragLine.Visibility = Visibility.Hidden;
				HandleDragComplete(controlPos);
			}
			else
			{
				HandleSelectionRect(controlPos);
			}
		}

		private void HandleDragComplete(Point controlPos)
		{
			// The canvas has coordinates where the y value increases from top to bottom.
			var posYInverted = new Point(controlPos.X, MainCanvas.ActualHeight - controlPos.Y);

			Debug.WriteLine($"We are handling a DragComplete at pos:{posYInverted}.");
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

					var rect = _selectedArea.Area;
					_selectedArea.Deactivate();

					var area = new RectangleInt(
						new PointInt((int)Math.Round(rect.X), (int)Math.Round(rect.Y)),
						new SizeInt((int)Math.Round(rect.Width), (int)Math.Round(rect.Height))
					);

					AreaSelected?.Invoke(this, new AreaSelectedEventArgs(TransformType.Zoom, area));
				}
			}
		}

		#endregion

		#region Canvas Handlers

		private void Canvas_MouseLeave(object sender, MouseEventArgs e)
		{
			if (_inDrag)
			{
				_dragLine.Visibility = Visibility.Hidden;
			}
		}

		private void Canvas_MouseEnter(object sender, MouseEventArgs e)
		{
			if (_inDrag)
			{
				_dragLine.Visibility = Visibility.Visible;
			}
		}

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
				var result = new SizeInt(
					(int)Math.Round(MainCanvas.ActualWidth),
					(int)Math.Round(MainCanvas.ActualHeight));

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

		private class ScreenSection
		{
			public Image Image { get; init; }
			public Canvas Canvas { get; init; }
			public int ChildIndex { get; init; }

			public ScreenSection(Canvas canvas, SizeInt size)
			{
				Image = CreateImage(size.Width, size.Height);
				ChildIndex = canvas.Children.Add(Image);
				Image.SetValue(Panel.ZIndexProperty, 0);
			}

			public void Place(PointInt position)
			{
				Image.SetValue(Canvas.LeftProperty, (double)position.X);
				Image.SetValue(Canvas.BottomProperty, (double)position.Y);
			}

			public void WritePixels(byte[] pixels)
			{
				var bitmap = (WriteableBitmap)Image.Source;

				var w = (int)Math.Round(Image.Width);
				var h = (int)Math.Round(Image.Height);

				var rect = new Int32Rect(0, 0, w, h);
				var stride = 4 * w;
				bitmap.WritePixels(rect, pixels, stride, 0);

				Image.Visibility = Visibility.Visible;
			}

			private Image CreateImage(int w, int h)
			{
				var result = new Image
				{
					Width = w,
					Height = h,
					Stretch = Stretch.None,
					Margin = new Thickness(0),
					Source = new WriteableBitmap(w, h, 96, 96, PixelFormats.Bgra32, null)
				};

				return result;
			}

		}
	}
}

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
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

		public MapDisplay()
		{
			_selectedArea = null;
			Loaded += MapDisplay_Loaded;
			InitializeComponent();
		}

		internal event EventHandler<AreaSelectedEventArgs> AreaSelected;

		private void MapDisplay_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext is null)
			{
				//throw new InvalidOperationException("The DataContext is null as the MapDisplay UserControl is being loaded.");
				return;
			}
			else
			{
				_screenSections = new Dictionary<PointInt, ScreenSection>();

				MainCanvas.SizeChanged += MainCanvas_SizeChanged;
				TriggerCanvasSizeUpdate();

				_vm = (IMapJobViewModel)DataContext;
				_vm.MapSections.CollectionChanged += MapSections_CollectionChanged;
				_selectedArea = new SelectionRectangle(MainCanvas, _vm.BlockSize);

				Debug.WriteLine("The MapDisplay is now loaded.");
			}
		}

		private void MainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			TriggerCanvasSizeUpdate();
		}

		private void TriggerCanvasSizeUpdate()
		{
			CanvasSize = new SizeInt();
		}

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				HideScreenSections();
			}
			else if(e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				IList<MapSection> newItems = e.NewItems is null ? new List<MapSection>() : e.NewItems.Cast<MapSection>().ToList();

				foreach(var mapSection in newItems)
				{
					var screenSection = GetScreenSection(mapSection);

					Debug.WriteLine($"Writing Pixels for section at {mapSection.CanvasPosition}.");

					screenSection.WritePixels(mapSection.Pixels1d);
				}
			}
		}

		private void HideScreenSections()
		{
			foreach (UIElement c in MainCanvas.Children.OfType<Image>())
			{
				c.Visibility = Visibility.Hidden;
			}
		}

		private ScreenSection GetScreenSection(MapSection mapSection)
		{
			if (!_screenSections.TryGetValue(mapSection.CanvasPosition, out var screenSection))
			{
				screenSection = CreateScreenSection(mapSection.CanvasPosition, mapSection.Size);
			}

			return screenSection;
		}

		private ScreenSection CreateScreenSection(PointInt canvasPosition, SizeInt size)
		{
			var result = new ScreenSection(size);
			var cIndex = MainCanvas.Children.Add(result.Image);

			var element = MainCanvas.Children[cIndex];
			element.SetValue(Canvas.LeftProperty, (double)canvasPosition.X);
			element.SetValue(Canvas.BottomProperty, (double)canvasPosition.Y);
			element.SetValue(Panel.ZIndexProperty, 0);

			return result;
		}

		private void MainCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			// Get position of mouse relative to the main canvas and invert the y coordinate.
			var controlPos = e.GetPosition(relativeTo: MainCanvas);

			// The canvas has coordinates where the y value increases from  bottom to top.
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

					_selectedArea.IsActive = false;
					var rect = _selectedArea.Area;

					var area = new RectangleInt(
						new PointInt((int)Math.Round(rect.X), (int)Math.Round(rect.Y)),
						new SizeInt((int)Math.Round(rect.Width), (int)Math.Round(rect.Height))
					);

					AreaSelected?.Invoke(this, new AreaSelectedEventArgs(TransformType.Zoom, area));
				}
			}
		}

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
			public Histogram Histogram { get; init; }

			public ScreenSection(SizeInt size)
			{
				Image = CreateImage(size.Width, size.Height);
				Histogram = null;
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
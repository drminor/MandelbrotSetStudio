using MSetExplorer.MapWindow;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
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
		private Progress<MapSection> _mapLoadingProgress;
		private IDictionary<PointInt, ScreenSection> _screenSections;

		private int _jobNameCounter;

		public MapDisplay()
		{
			_selectedArea = null;
			Loaded += MapDisplay_Loaded;
			InitializeComponent();
		}

		private void MapDisplay_Loaded(object sender, RoutedEventArgs e)
		{
			if (DataContext != null)
			{
				_vm = (IMapJobViewModel)DataContext;

				_selectedArea = new SelectionRectangle(MainCanvas, _vm.BlockSize);
				_mapLoadingProgress = new Progress<MapSection>(HandleMapSectionReady);
				_vm.OnMapSectionReady = ((IProgress<MapSection>)_mapLoadingProgress).Report;
				_screenSections = new Dictionary<PointInt, ScreenSection>();

				//var canvasSize = GetCanvasControlSize(MainCanvas);
				var maxIterations = 700;
				var mSetInfo = MapWindowHelper.BuildInitialMSetInfo(maxIterations);
				
				MapCalcSettings = mSetInfo.MapCalcSettings;
				ColorMapEntries = mSetInfo.ColorMapEntries;
				Coords = mSetInfo.Coords;

				//_vm.LoadMap("initial job", canvasSize, mSetInfo, canvasSize);
			}
		}

		public void GoBack()
		{
			HideScreenSections();
			var canvasSize = GetCanvasControlSize(MainCanvas);
			_vm.GoBack(canvasSize);
		}

		private SizeInt GetCanvasControlSize(Canvas canvas)
		{
			var width = (int)Math.Round(canvas.Width);
			var height = (int)Math.Round(canvas.Height);
			return new SizeInt(width, height);
		}

		private void HandleMapSectionReady(MapSection mapSection)
		{
			//Debug.WriteLine($"Drawing a bit map at {mapSection.CanvasPosition}.");

			var screenSection = GetScreenSection(mapSection);
			screenSection.WritePixels(mapSection.Pixels1d);
		}

		private ScreenSection GetScreenSection(MapSection mapSection)
		{
			if (!_screenSections.TryGetValue(mapSection.CanvasPosition, out var screenSection))
			{
				screenSection = new ScreenSection(mapSection.Size);
				var cIndex = MainCanvas.Children.Add(screenSection.Image);

				MainCanvas.Children[cIndex].SetValue(Canvas.LeftProperty, (double)mapSection.CanvasPosition.X);
				MainCanvas.Children[cIndex].SetValue(Canvas.BottomProperty, (double)mapSection.CanvasPosition.Y);
				MainCanvas.Children[cIndex].SetValue(Panel.ZIndexProperty, 0);
			}

			return screenSection;
		}

		private void MainCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

					var curJob = _vm.CurrentJob;
					var position = curJob.MSetInfo.Coords.LeftBot;
					var canvasControlOffset = curJob.CanvasControlOffset;
					var samplePointDelta = curJob.Subdivision.SamplePointDelta;

					_selectedArea.IsActive = false;
					var rect = _selectedArea.Area;

					// Adjust the selected area's origin to account for the portion of the start block that is off screen.
					var area = new RectangleInt(
						new PointInt((int)Math.Round(rect.X + canvasControlOffset.Width), (int)Math.Round(rect.Y + canvasControlOffset.Height)),
						new SizeInt((int)Math.Round(rect.Width), (int)Math.Round(rect.Height))
					);

					var coords = RMapHelper.GetMapCoords(area, position, samplePointDelta);

					Debug.WriteLine($"Starting Job with new coords: {coords}.");
					//LoadMap(coords, area.Size);
					Coords = coords;
				}
			}
		}

		//private void LoadMap(RRectangle coords, SizeInt newArea)
		//{
		//	HideScreenSections();

		//	var canvasSize = GetCanvasControlSize(MainCanvas);
		//	//var curMSetInfo = _vm.CurrentJob.MSetInfo;
		//	//var mSetInfo = MSetInfo.UpdateWithNewCoords(curMSetInfo, coords);

		//	if (!(coords is null))
		//	{
		//		var label = "Zoom:" + _jobNameCounter++.ToString();
		//		_vm.LoadMap(label, canvasSize, mSetInfo, newArea);
		//	}
		//}

		private void LoadMap(MSetInfo mSetInfo)
		{
			HideScreenSections();

			var canvasSize = GetCanvasControlSize(MainCanvas);
			var label = "Zoom:" + _jobNameCounter++.ToString();
			_vm.LoadMap(label, canvasSize, mSetInfo, new SizeInt());
		}

		private void HideScreenSections()
		{
			foreach (UIElement c in MainCanvas.Children.OfType<Image>())
			{
				c.Visibility = Visibility.Hidden;
			}
		}

		#region Dependency Properties

		public static readonly DependencyProperty CoordsProperty = DependencyProperty.Register(
			"Coords",
			typeof(RRectangle),
			typeof(MapDisplay), new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				CoordsChanged));

		public RRectangle Coords
		{
			get => (RRectangle)GetValue(CoordsProperty);
			set => SetValue(CoordsProperty, value);
		}

		private static void CoordsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			Debug.WriteLine($"The Coords are being changed.");

			if (sender is MapDisplay md)
			{
				Debug.WriteLine("The CoordsDP is being set on the MapDisplay Control.");
				var oldCoords = e.OldValue as RRectangle;
				var newCoords = e.NewValue as RRectangle;

				if (EqualityComparer<RRectangle>.Default.Equals(oldCoords, newCoords))
				{
					Debug.WriteLine("The old and new Coord values are the same.");
				}
				else
				{
					if (!(newCoords is null) && !(md.MapCalcSettings is null) && !(md.ColorMapEntries is null))
					{
						var mSetInfo = new MSetInfo(newCoords, md.MapCalcSettings, md.ColorMapEntries);
						md.LoadMap(mSetInfo);
					}
				}
			}
			else
			{
				Debug.WriteLine($"CoordsChanged was raised from sender: {sender}");
			}
		}

		public static readonly DependencyProperty MapCalcSettingsProperty = DependencyProperty.Register(
			"MapCalcSettings",
			typeof(MapCalcSettings),
			typeof(MapDisplay), new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				MapCalcSettingsChanged));

		public MapCalcSettings MapCalcSettings
		{
			get => (MapCalcSettings)GetValue(MapCalcSettingsProperty);
			set => SetValue(MapCalcSettingsProperty, value);
		}

		private static void MapCalcSettingsChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			Debug.WriteLine($"The MapCalcSettings are being changed.");
		}

		public static readonly DependencyProperty ColorMapEntriesProperty = DependencyProperty.Register(
			"ColorMapEntries",
			typeof(ColorMapEntry[]),
			typeof(MapDisplay), new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
				ColorMapEntriesChanged));

		public ColorMapEntry[] ColorMapEntries
		{
			get => (ColorMapEntry[])GetValue(ColorMapEntriesProperty);
			set => SetValue(ColorMapEntriesProperty, value);
		}

		private static void ColorMapEntriesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
		{
			Debug.WriteLine($"The ColorMapEntries are being changed.");
		}


		#endregion

		private class ScreenSection
		{
			public Image Image { get; init; }
			//public Histogram Histogram { get; init; }

			public ScreenSection(SizeInt size)
			{
				Image = CreateImage(size);
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

			private Image CreateImage(SizeInt size)
			{
				var result = new Image
				{
					Width = size.Width,
					Height = size.Height,
					Stretch = Stretch.None,
					Margin = new Thickness(0),
					Source = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Bgra32, null)
				};

				return result;
			}

		}
	}
}

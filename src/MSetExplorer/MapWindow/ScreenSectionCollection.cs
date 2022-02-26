using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class ScreenSectionCollection : IScreenSectionCollection
	{
		private readonly ObservableCollection<MapSection> _mapSections;

		private readonly ScreenSection[,] _screenSections;
		private readonly DrawingGroup _drawingGroup;
		private readonly Image _image;

		#region Constructor

		public ScreenSectionCollection(Canvas canvas, SizeInt blockSize, ObservableCollection<MapSection> mapSections)
		{
			_mapSections = mapSections;
			var canvasSize = new Size(canvas.Width, canvas.Height);
			var sizeInBlocks = GetSizeInBlocks(canvasSize, blockSize);

			_screenSections = BuildScreenSections(sizeInBlocks, blockSize);

			_drawingGroup = new DrawingGroup();
			_image = new Image { Source = new DrawingImage(_drawingGroup) };
			_ = canvas.Children.Add(_image);

			CanvasOffset = new VectorInt();

			_mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		private ScreenSection[,] BuildScreenSections(SizeInt sizeInBlocks, SizeInt blockSize)
		{
			Debug.WriteLine($"Building {sizeInBlocks} Screen Sections.");

			// Create the screen sections to cover the canvas
			var result = new ScreenSection[sizeInBlocks.Height, sizeInBlocks.Width];

			var maxYPtr = sizeInBlocks.Height - 1;

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var position = new PointInt(xBlockPtr, maxYPtr - yBlockPtr);
					var screenSection = new ScreenSection(position, blockSize);

					result[yBlockPtr,xBlockPtr] = screenSection;
				}
			}

			return result;
		}

		private SizeInt GetSizeInBlocks(Size canvasSize, SizeInt blockSize)
		{
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.
			var canvasSizeInt = new SizeDbl(canvasSize.Width, canvasSize.Height).Round();
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSizeInt, blockSize);
			var result = new SizeInt(canvasSizeInBlocks.Width + 2, canvasSizeInBlocks.Height + 2);

			// Always overide the above calculation and allocate 400 sections.
			if (result.Width > 0)
			{
				result = new SizeInt(20, 20);
			}

			return result;
		}

		#endregion

		/// <summary>
		/// The position of the canvas' origin relative to the Image Block Data
		/// </summary>
		public VectorInt CanvasOffset
		{
			get
			{
				var pointDbl = new PointDbl(
					(double)_image.GetValue(Canvas.LeftProperty),
					(double)_image.GetValue(Canvas.BottomProperty)
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
					_image.SetValue(Canvas.LeftProperty, (double)offset.X);
					_image.SetValue(Canvas.BottomProperty, (double)offset.Y);
				}
			}
		}

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				HideScreenSections();
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				// Adding new items
				foreach (var mapSection in GetList(e.NewItems))
				{
					Draw(mapSection);
				}
			}
		}

		private IList<MapSection> GetList(IList lst)
		{
			return lst?.Cast<MapSection>().ToList() ?? new List<MapSection>();
		}

		public void HideScreenSections()
		{
			_drawingGroup.Children.Clear();
		}

		public void Draw(MapSection mapSection)
		{
			//Debug.WriteLine($"Writing Pixels for section at {mapSection.CanvasPosition}.");

			if (!(mapSection.Pixels1d is null))
			{
				var screenSection = Get(mapSection.BlockPosition);
				screenSection.WritePixels(mapSection.Pixels1d);
				_drawingGroup.Children.Add(screenSection.ImageDrawing);
			}
		}

		private ScreenSection Get(PointInt blockPosition)
		{
			//Debug.WriteLine($"About to draw screen section at position: {blockPosition}. CanvasControlOff: {CanvasOffset}.");
			var result = _screenSections[blockPosition.Y, blockPosition.X];
			return result;
		}

		private class ScreenSection
		{
			public ImageDrawing ImageDrawing { get; }

			public ScreenSection(PointInt blockPosition, SizeInt blockSize)
			{
				var image = CreateImage(blockSize.Width, blockSize.Height);
				var position = blockPosition.Scale(blockSize);
				var rect = new Rect(new Point(position.X, position.Y), new Size(blockSize.Width, blockSize.Height));

				ImageDrawing = new ImageDrawing(image.Source, rect);
			}

			public void WritePixels(byte[] pixels)
			{
				var bitmap = (WriteableBitmap)ImageDrawing.ImageSource;

				var w = (int)Math.Round(bitmap.Width);
				var h = (int)Math.Round(bitmap.Height);

				var rect = new Int32Rect(0, 0, w, h);
				var stride = 4 * w;
				bitmap.WritePixels(rect, pixels, stride, 0);
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

using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class ScreenSectionCollection2 : IScreenSectionCollection
	{
		private readonly Canvas _canvas;
		private readonly ScreenSection[,] _screenSections;
		private readonly DrawingGroup _drawingGroup;
		private readonly Image _image;

		#region Constructor

		public ScreenSectionCollection2(Canvas canvas, SizeInt blockSize)
		{
			_canvas = canvas;
			_screenSections = BuildScreenSections(blockSize);
			_drawingGroup = new DrawingGroup();
			_image = new Image { Source = new DrawingImage(_drawingGroup) };
			_ = canvas.Children.Add(_image);
			Position = new PointDbl();
		}

		private ScreenSection[,] BuildScreenSections(SizeInt blockSize)
		{
			// Create the screen sections to cover the canvas
			var sizeInBlocks = GetSizeInBlocks(blockSize);
			var result = new ScreenSection[sizeInBlocks.Height, sizeInBlocks.Width];

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var position = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = new ScreenSection(new RectangleInt(position.Scale(blockSize), blockSize));
					result[yBlockPtr,xBlockPtr] = screenSection;
				}
			}

			return result;
		}

		private SizeInt GetSizeInBlocks(SizeInt blockSize)
		{
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.
			var canvasSize = new SizeInt((int)Math.Round(_canvas.Width), (int)Math.Round(_canvas.Height));
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, blockSize);
			var result = new SizeInt(canvasSizeInBlocks.Width + 1, canvasSizeInBlocks.Height + 1);

			return result;
		}

		#endregion

		public PointDbl Position
		{
			get => new((double)_image.GetValue(Canvas.LeftProperty), (double)_image.GetValue(Canvas.BottomProperty));

			set
			{
				_image.SetValue(Canvas.LeftProperty, value.X);
				_image.SetValue(Canvas.BottomProperty, value.Y);

			}
		}

		public void HideScreenSections()
		{
			_drawingGroup.Children.Clear();
		}

		public void Draw(MapSection mapSection)
		{
			var maxYIndex = _screenSections.GetLength(1) - 1;
			var screenSection = _screenSections[maxYIndex - mapSection.BlockPosition.Y, mapSection.BlockPosition.X];
			screenSection.WritePixels(mapSection.Pixels1d);
			_drawingGroup.Children.Add(screenSection.ImageDrawing);
		}

		private class ScreenSection
		{
			public ImageDrawing ImageDrawing { get; }

			public ScreenSection(RectangleInt rectangle)
			{
				var image = CreateImage(rectangle.Width, rectangle.Height);
				var rect = new Rect(new Point(rectangle.X1, rectangle.Y1), new Size(rectangle.Width, rectangle.Height));
				ImageDrawing = new ImageDrawing(image.Source, rect);
			}

			public void Place(PointInt position)
			{
				throw new NotImplementedException();
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

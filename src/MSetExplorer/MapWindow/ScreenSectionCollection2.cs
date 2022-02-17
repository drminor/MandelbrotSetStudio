﻿using MSS.Common;
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
		private readonly ScreenSection[,] _screenSections;
		private readonly DrawingGroup _drawingGroup;
		private readonly Image _image;

		#region Constructor

		public ScreenSectionCollection2(Canvas canvas, SizeInt blockSize)
		{
			var canvasSize = new Size(canvas.Width, canvas.Height);
			var sizeInBlocks = GetSizeInBlocks(canvasSize, blockSize);
			_screenSections = BuildScreenSections(sizeInBlocks, blockSize);

			_drawingGroup = new DrawingGroup();
			_image = new Image { Source = new DrawingImage(_drawingGroup) };
			_ = canvas.Children.Add(_image);

			Position = new PointDbl();
		}

		private ScreenSection[,] BuildScreenSections(SizeInt sizeInBlocks, SizeInt blockSize)
		{
			// Create the screen sections to cover the canvas
			var result = new ScreenSection[sizeInBlocks.Height, sizeInBlocks.Width];

			var maxYPtr = sizeInBlocks.Height - 1;

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var position = new PointInt(xBlockPtr, maxYPtr - yBlockPtr);

					//var screenSection = new ScreenSection(new RectangleInt(position.Scale(blockSize), blockSize));
					//var screenSection = new ScreenSection(position.Scale(blockSize), blockSize);
					var screenSection = new ScreenSection(position, blockSize);

					result[yBlockPtr,xBlockPtr] = screenSection;
				}
			}

			return result;
		}

		private SizeInt GetSizeInBlocks(Size canvasSize, SizeInt blockSize)
		{
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.
			var canvasSizeInt = new SizeInt(canvasSize.Width, canvasSize.Height);
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSizeInt, blockSize);
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
			var screenSection = _screenSections[mapSection.BlockPosition.Y, mapSection.BlockPosition.X];
			screenSection.WritePixels(mapSection.Pixels1d);
			_drawingGroup.Children.Add(screenSection.ImageDrawing);
		}

		private class ScreenSection
		{
			public ImageDrawing ImageDrawing { get; }

			//public ScreenSection(RectangleInt rectangle)
			//{
			//	var image = CreateImage(rectangle.Width, rectangle.Height);
			//	var rect = new Rect(new Point(rectangle.X1, rectangle.Y1), new Size(rectangle.Width, rectangle.Height));
			//	ImageDrawing = new ImageDrawing(image.Source, rect);
			//}

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

using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace MSetExplorer
{
	internal class ScreenSectionCollection2 : IScreenSectionCollection
	{
		private readonly Canvas _canvas;
		private readonly ScreenSection2[,] _screenSections;
		private readonly DrawingGroup _drawingGroup;
		private readonly Image _image;

		public ScreenSectionCollection2(Canvas canvas, SizeInt blockSize)
		{
			_canvas = canvas;
			_screenSections = BuildScreenSections(blockSize);
			_drawingGroup = new DrawingGroup();
			_image = new Image { Source = new DrawingImage(_drawingGroup) };
			_ = canvas.Children.Add(_image);
			Position = new PointDbl();
		}

		private ScreenSection2[,] BuildScreenSections(SizeInt blockSize)
		{
			// Create the screen sections to cover the canvas
			var sizeInBlocks = GetSizeInBlocks(blockSize);
			var result = new ScreenSection2[sizeInBlocks.Height, sizeInBlocks.Width];

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var position = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = new ScreenSection2(new RectangleInt(position.Scale(blockSize), blockSize));
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

		public PointDbl Position
		{
			get => new((double)_image.GetValue(Canvas.LeftProperty), (double)_image.GetValue(Canvas.BottomProperty));

			set
			{
				_image.SetValue(Canvas.LeftProperty, value.X);
				_image.SetValue(Canvas.BottomProperty, value.Y);

			}
		}

	}
}

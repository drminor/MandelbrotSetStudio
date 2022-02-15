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
		private readonly SizeInt _blockSize;
		private readonly ScreenSection2[,] _screenSections;

		private readonly DrawingGroup _drawingGroup;

		public ScreenSectionCollection2(Canvas canvas, SizeInt blockSize)
		{
			_canvas = canvas;
			_blockSize = blockSize;
			_screenSections = BuildScreenSections();

			_drawingGroup = new DrawingGroup();
			AddGroupToCanvas(canvas, _drawingGroup);
		}

		private ScreenSection2[,] BuildScreenSections()
		{
			// Create the screen sections to cover the canvas
			var sizeInBlocks = GetSizeInBlocks();
			var result = new ScreenSection2[sizeInBlocks.Height, sizeInBlocks.Width];

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var position = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = new ScreenSection2(position, _blockSize);
					result[yBlockPtr,xBlockPtr] = screenSection;
				}
			}

			return result;
		}

		private SizeInt GetSizeInBlocks()
		{
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.
			var canvasSize = new SizeInt((int)Math.Round(_canvas.Width), (int)Math.Round(_canvas.Height));
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, _blockSize);
			var result = new SizeInt(canvasSizeInBlocks.Width + 1, canvasSizeInBlocks.Height + 1);

			return result;
		}

		private void AddGroupToCanvas(Canvas canvas, DrawingGroup drawingGroup)
		{
			var image = new Image
			{
				Source = new DrawingImage(drawingGroup)
			};

			_ = canvas.Children.Add(image);

			image.SetValue(Canvas.LeftProperty, 0d);
			image.SetValue(Canvas.BottomProperty, 0d);
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

	}
}

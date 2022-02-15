using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MSetExplorer.MapWindow
{
	internal class ScreenSectionCollection
	{
		private readonly Canvas _canvas;
		private readonly SizeInt _blockSize;
		private readonly IDictionary<PointInt, ScreenSection> _screenSections;

		public ScreenSectionCollection(Canvas canvas, SizeInt blockSize)
		{
			_canvas = canvas;
			_blockSize = blockSize;
			_screenSections = BuildScreenSections();
		}

		private Dictionary<PointInt, ScreenSection> BuildScreenSections()
		{
			var result = new Dictionary<PointInt, ScreenSection>();

			// Create the screen sections to cover the canvas
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.

			var canvasSize = new SizeInt((int)Math.Round(_canvas.Width), (int)Math.Round(_canvas.Height));

			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, _blockSize);
			for (var yBlockPtr = 0; yBlockPtr < canvasSizeInBlocks.Height + 1; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < canvasSizeInBlocks.Width + 1; xBlockPtr++)
				{
					var position = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = new ScreenSection(_canvas, _blockSize);
					result.Add(position, screenSection);
				}
			}

			return result;
		}

		public void HideScreenSections()
		{
			foreach (UIElement c in _canvas.Children.OfType<Image>())
			{
				c.Visibility = Visibility.Hidden;
			}
		}

		public void Draw(MapSection mapSection)
		{
			var screenSection = GetScreenSection(mapSection.BlockPosition, mapSection.Size);
			screenSection.Place(mapSection.CanvasPosition);
			screenSection.WritePixels(mapSection.Pixels1d);
		}

		private ScreenSection GetScreenSection(PointInt blockPosition, SizeInt blockSize)
		{
			if (!_screenSections.TryGetValue(blockPosition, out var screenSection))
			{
				screenSection = new ScreenSection(_canvas, blockSize);
				_screenSections.Add(blockPosition, screenSection);
			}

			return screenSection;
		}

	}
}

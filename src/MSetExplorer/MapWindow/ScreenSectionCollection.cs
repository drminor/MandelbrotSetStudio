using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class ScreenSectionCollection : IScreenSectionCollection
	{
		private readonly Canvas _canvas;
		private readonly SizeInt _blockSize;
		private readonly IDictionary<PointInt, ScreenSection> _screenSections;

		#region Constructor

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

			//var canvasSize = new SizeInt((int)Math.Round(_canvas.Width), (int)Math.Round(_canvas.Height));
			var canvasSize = new SizeInt(_canvas.Width, _canvas.Height);

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

		#endregion

		public PointDbl Position
		{ 
			get => throw new NotImplementedException(); 
			set => throw new NotImplementedException();
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

		private class ScreenSection
		{
			private readonly Image _image;

			public ScreenSection(Canvas canvas, SizeInt size)
			{
				_image = CreateImage(size.Width, size.Height);
				_ = canvas.Children.Add(_image);
				_image.SetValue(Panel.ZIndexProperty, 0);
			}

			public void Place(PointInt position)
			{
				_image.SetValue(Canvas.LeftProperty, (double)position.X);
				_image.SetValue(Canvas.BottomProperty, (double)position.Y);
			}

			public void WritePixels(byte[] pixels)
			{
				var bitmap = (WriteableBitmap)_image.Source;

				var w = (int)Math.Round(_image.Width);
				var h = (int)Math.Round(_image.Height);

				var rect = new Int32Rect(0, 0, w, h);
				var stride = 4 * w;
				bitmap.WritePixels(rect, pixels, stride, 0);

				_image.Visibility = Visibility.Visible;
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

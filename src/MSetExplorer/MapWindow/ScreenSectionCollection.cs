using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	internal class ScreenSectionCollection : IScreenSectionCollection
	{
		private readonly ScreenSection[,] _screenSections;
		private readonly DrawingGroup _drawingGroup;

		#region Constructor

		public ScreenSectionCollection(SizeInt canvasSize, SizeInt blockSize)
		{
			var sizeInBlocks = GetSizeInBlocks(canvasSize, blockSize);
			_screenSections = new ScreenSection[sizeInBlocks.Height, sizeInBlocks.Width];
			PopulateScreenSections(_screenSections, sizeInBlocks, blockSize);

			_drawingGroup = new DrawingGroup();
			MapDisplayImage = new Image { Source = new DrawingImage(_drawingGroup) };
		}

		#endregion

		#region Public Properties

		public Image MapDisplayImage { get; }

		#endregion

		#region Public Methods

		public void HideScreenSections()
		{
			_drawingGroup.Children.Clear();
		}

		public void Draw(MapSection mapSection)
		{
			//Debug.WriteLine($"Writing Pixels for section at {mapSection.CanvasPosition}.");

			if (!(mapSection.Pixels1d is null))
			{
				var screenSection = GetScreenSection(mapSection.BlockPosition);
				screenSection.WritePixels(mapSection.Pixels1d);

				screenSection.BlockPosition = GetInvertedBlockPos(mapSection.BlockPosition, _screenSections);

				_drawingGroup.Children.Add(screenSection.ImageDrawing);
			}
		}

		#endregion

		#region Private Methods

		private ScreenSection GetScreenSection(PointInt blockPosition)
		{
			var result = _screenSections[blockPosition.Y, blockPosition.X];
			return result;
		}

		private void PutScreenSection(PointInt blockPosition, ScreenSection screenSection)
		{
			_screenSections[blockPosition.X, blockPosition.Y] = screenSection;
		}

		private void PopulateScreenSections(ScreenSection[,] screenSections, SizeInt sizeInBlocks, SizeInt blockSize)
		{
			Debug.WriteLine($"Populating {sizeInBlocks} Screen Sections.");

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var invertedPosition = GetInvertedBlockPos(blockPosition, screenSections);
					var screenSection = new ScreenSection(invertedPosition, blockSize);
					PutScreenSection(blockPosition, screenSection);
				}
			}
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition, ScreenSection[,] screenSections)
		{
			var maxYPtr = screenSections.GetUpperBound(0);
			var result = new PointInt(blockPosition.X, maxYPtr - blockPosition.Y);

			return result;
		}

		private SizeInt GetSizeInBlocks(SizeInt canvasSize, SizeInt blockSize)
		{
			// Include an additional block to accommodate when the CanvasControlOffset is non-zero.
			var canvasSizeInBlocks = RMapHelper.GetCanvasSizeInBlocks(canvasSize, blockSize);
			var result = canvasSizeInBlocks.Inflate(2);

			// Always overide the above calculation and allocate 400 sections.
			if (result.Width > 0)
			{
				result = new SizeInt(20, 20);
			}

			return result;
		}

		#endregion

		private class ScreenSection
		{
			private PointInt _blockPosition;
			private readonly SizeInt _blockSize;
			//private readonly Image _image;

			public ScreenSection(PointInt blockPosition, SizeInt blockSize)
			{
				_blockPosition = blockPosition;
				_blockSize = blockSize;
				var image = CreateImage(blockSize.Width, blockSize.Height);

				ImageDrawing = CreateImageDrawing(image.Source, _blockPosition, _blockSize);
			}

			public ImageDrawing ImageDrawing { get; private set; }

			public PointInt BlockPosition
			{
				get => _blockPosition;
				set
				{
					if (value != _blockPosition)
					{
						_blockPosition = value;
						ImageDrawing = CreateImageDrawing(ImageDrawing.ImageSource, _blockPosition, _blockSize);
					}
				}
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

			private ImageDrawing CreateImageDrawing(ImageSource imageSource, PointInt blockPosition, SizeInt blockSize )
			{
				var position = blockPosition.Scale(blockSize);
				var rect = new Rect(new Point(position.X, position.Y), new Size(blockSize.Width, blockSize.Height));
				var result = new ImageDrawing(imageSource, rect);

				return result;
			}
		}

	}
}

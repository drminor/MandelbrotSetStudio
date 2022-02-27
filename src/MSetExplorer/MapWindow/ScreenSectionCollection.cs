using MSS.Common;
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.ComponentModel;
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

			var dpd = DependencyPropertyDescriptor.FromProperty(DrawingGroup.ChildrenProperty, typeof(DrawingGroup));
			dpd.AddValueChanged(_drawingGroup, (o, e) => OnDrawingGroupChildrenChangedA(o, e));
			dpd.RemoveValueChanged(_drawingGroup, (o, e) => OnDrawingGroupChildrenChangedA(o, e));
			MapDisplayImage = new Image { Source = new DrawingImage(_drawingGroup) };
		}

		#endregion

		#region Public Properties

		public Image MapDisplayImage { get; }

		#endregion

		#region Public Methods

		public void HideScreenSections()
		{
			ClearDrawingGroup();
		}

		public void Draw(MapSection mapSection)
		{
			if (!(mapSection.Pixels1d is null))
			{
				var screenSection = GetScreenSection(mapSection.BlockPosition);
				Debug.WriteLine($"Drawing section: {mapSection.BlockPosition} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}.");
	
				//screenSection.BlockPosition = GetInvertedBlockPos(mapSection.BlockPosition, _screenSections);
					screenSection.WritePixels(mapSection.Pixels1d, _drawingGroup);

				//_drawingGroup.Children.Add(screenSection.ImageDrawing);
				//screenSection.AddToDrawingGroup(_drawingGroup);
				//screenSection.Active = true;
			}
			else
			{
				var screenSection = GetScreenSection(mapSection.BlockPosition);
				Debug.WriteLine($"Not Drawing section: {mapSection.BlockPosition} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}.");
			}
		}

		public void Test()
		{
			var cnt = 0;
			var sizeInBlocks = SizeInBlocks;

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = GetScreenSection(blockPosition);

					if (!screenSection.Active)
					{
						continue;
					}

					if (xBlockPtr == 0)
					{
						screenSection.RemoveFromDrawingGroup(_drawingGroup);
					}
					else
					{
						screenSection.RemoveFromDrawingGroup(_drawingGroup);

						var nPos = screenSection.BlockPosition.Translate(new VectorInt(-1, 0));
						////var invertedPosition = GetInvertedBlockPos(nPos, _screenSections);
						
						if (screenSection.ScreenPosition.X >= 0)
						{
							screenSection.BlockPosition = nPos;
							screenSection.AddToDrawingGroup(_drawingGroup);
						}

				}

					if (screenSection.Active)
					{
						cnt++;
					}
				}
			}

			Debug.WriteLine($"The ScreenSectionCollection is being tested. There are {cnt} active blocks.");

		}

		#endregion

		#region Event Handlers

		private void OnDrawingGroupChildrenChangedA(object sender, EventArgs e)
		{
			Debug.WriteLine($"Our Drawing Group now has {_drawingGroup.Children.Count} children.");
		}

		#endregion

		#region Private Methods

		private void ClearDrawingGroup()
		{
			_drawingGroup.Children.Clear();

			var sizeInBlocks = SizeInBlocks;
			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = GetScreenSection(blockPosition);
					screenSection.Active = false;
				}
			}
		}

		private ScreenSection GetScreenSection(PointInt blockPosition)
		{
			var result = _screenSections[blockPosition.Y, blockPosition.X];
			return result;
		}

		private void PutScreenSection(PointInt blockPosition, ScreenSection screenSection)
		{
			_screenSections[blockPosition.Y, blockPosition.X] = screenSection;
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

		#region Private Properties 

		private SizeInt SizeInBlocks => new SizeInt(_screenSections.GetUpperBound(1), _screenSections.GetUpperBound(0)).Inflate(1);

		#endregion

		private class ScreenSection
		{
			private PointInt _blockPosition;
			private readonly SizeInt _blockSize;
			private ImageDrawing ImageDrawing;

			#region Constructor

			public ScreenSection(PointInt blockPosition, SizeInt blockSize)
			{
				_blockPosition = blockPosition;
				_blockSize = blockSize;

				ImageDrawing = CreateImageDrawing(_blockPosition, _blockSize);
			}

			#endregion

			#region Public Properties

			//public ImageDrawing ImageDrawing { get; private set; }

			public bool Active { get; set; }

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

			public PointInt ScreenPosition => GetPointInt(ImageDrawing.Rect.Location);

			private PointInt GetPointInt(Point p) => new PointDbl(p.X, p.Y).Round();

			#endregion

			#region Public Methods

			public void AddToDrawingGroup(DrawingGroup drawingGroup)
			{
				drawingGroup.Children.Add(ImageDrawing);
				Active = true;
			}

			public void RemoveFromDrawingGroup(DrawingGroup drawingGroup)
			{
				drawingGroup.Children.Remove(ImageDrawing);
				Active = false;
			}

			public void WritePixels(byte[] pixels, DrawingGroup drawingGroup)
			{
				WritePixels(pixels);
				AddToDrawingGroup(drawingGroup);
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

			#endregion

			#region Private Methods

			private ImageDrawing CreateImageDrawing(PointInt blockPosition, SizeInt blockSize)
			{
				var image = CreateImage(blockSize.Width, blockSize.Height);
				var result = CreateImageDrawing(image.Source, blockPosition, blockSize);

				return result;
			}

			private ImageDrawing CreateImageDrawing(ImageSource imageSource, PointInt blockPosition, SizeInt blockSize )
			{
				var position = blockPosition.Scale(blockSize);
				var rect = new Rect(new Point(position.X, position.Y), new Size(blockSize.Width, blockSize.Height));
				var result = new ImageDrawing(imageSource, rect);

				return result;
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

			#endregion
		}

	}
}

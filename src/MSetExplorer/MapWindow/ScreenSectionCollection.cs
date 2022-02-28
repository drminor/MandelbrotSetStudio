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
		private readonly DrawingGroup _drawingGroup;
		private readonly ScreenSection[,] _screenSections;
		private readonly SizeInt _sizeInBlocks; // => new SizeInt(_screenSections.GetUpperBound(1), _screenSections.GetUpperBound(0)).Inflate(1);

		private VectorInt _startIndex;

		#region Constructor

		public ScreenSectionCollection(SizeInt canvasSize, SizeInt blockSize)
		{
			_drawingGroup = new DrawingGroup();
			MapDisplayImage = new Image { Source = new DrawingImage(_drawingGroup) };

			_sizeInBlocks = GetSizeInBlocks(canvasSize, blockSize);
			_screenSections = BuildScreenSections(_sizeInBlocks, blockSize, _drawingGroup);
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
			var screenSection = GetScreenSection(mapSection.BlockPosition);
			var desc = mapSection.Pixels1d is null ? "Not drawing" : "Drawing";
			Debug.WriteLine($"{desc} section: {mapSection.BlockPosition} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}.");

			if (!(mapSection.Pixels1d is null))
			{
				screenSection.WritePixels(mapSection.Pixels1d);
			}
		}

		public void Test()
		{
			var cnt = 0;
			var sizeInBlocks = _sizeInBlocks;

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var screenSection = GetScreenSection(blockPosition);

					//if (!screenSection.Active)
					//{
					//	continue;
					//}

					if (xBlockPtr == 0)
					{
						screenSection.Active = false;
					}
					else
					{
						//var nPos = screenSection.BlockPosition.Translate(new VectorInt(-1, 0));
						//if (screenSection.ScreenPosition.X >= 0)
						//{
						//	screenSection.BlockPosition = nPos;
						//	screenSection.Active = true;
						//}

						var nPos = screenSection.BlockPosition.Translate(new VectorInt(-1, 0));
						screenSection.BlockPosition = nPos;
						if (xBlockPtr < 9 &&  screenSection.HasPixelData)
						{
							screenSection.Active = true;
						}
					}

					if (screenSection.Active)
					{
						cnt++;
					}
				}
			}

			_startIndex = new VectorInt(_startIndex.X + 1, _startIndex.Y);
			_startIndex = _startIndex.Mod(_sizeInBlocks);

			Debug.WriteLine($"The ScreenSectionCollection is being tested. There are {cnt} active blocks. StartIndex: {_startIndex}.");
		}

		#endregion

		#region Private Methods

		private void ClearDrawingGroup()
		{
			_drawingGroup.Children.Clear();

			var sizeInBlocks = _sizeInBlocks;
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
			var adjPos = blockPosition.Translate(_startIndex).Mod(_sizeInBlocks);
			var result = _screenSections[adjPos.Y, adjPos.X];
			return result;
		}

		private void PutScreenSection(PointInt blockPosition, ScreenSection screenSection)
		{
			var adjPos = blockPosition.Translate(_startIndex).Mod(_sizeInBlocks);
			_screenSections[adjPos.Y, adjPos.X] = screenSection;
		}

		private ScreenSection[,] BuildScreenSections(SizeInt sizeInBlocks, SizeInt blockSize, DrawingGroup drawingGroup)
		{
			Debug.WriteLine($"Populating {sizeInBlocks} Screen Sections.");

			var result = new ScreenSection[sizeInBlocks.Height, sizeInBlocks.Width];

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var invertedPosition = GetInvertedBlockPos(blockPosition, result);
					var screenSection = new ScreenSection(invertedPosition, blockSize, drawingGroup);

					result[blockPosition.Y, blockPosition.X] = screenSection;
				}
			}

			return result;
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
				result = new SizeInt(12, 12);
			}

			return result;
		}

		#endregion

		private class ScreenSection
		{
			private PointInt _blockPosition;
			private readonly SizeInt _blockSize;
			private readonly Image _image;
			private readonly DrawingGroup _drawingGroup;

			private ImageDrawing _imageDrawing;
			private bool _active;

			#region Constructor

			public ScreenSection(PointInt blockPosition, SizeInt blockSize, DrawingGroup drawingGroup)
			{
				_blockPosition = blockPosition;
				_blockSize = blockSize;
				_drawingGroup = drawingGroup;
				_image = CreateImage(_blockSize.Width, _blockSize.Height);
				_imageDrawing = CreateImageDrawing(_blockPosition);
			}

			#endregion

			#region Public Properties

			public bool Active
			{
				get => _active;
				set
				{
					if (value != _active)
					{
						if (value)
						{
							if (!IsOnScreen)
							{
								throw new InvalidOperationException("Cannot activate a screen section if it not wholly contained with in the DrawingGroup.");
							}

							_drawingGroup.Children.Add(_imageDrawing);
							HasPixelData = true;
						}
						else
						{
							_drawingGroup.Children.Remove(_imageDrawing);
						}

						_active = value;
					}
				}
			}

			public PointInt BlockPosition
			{
				get => _blockPosition;
				set
				{
					if (value != _blockPosition)
					{
						_blockPosition = value;

						if (Active)
						{
							_drawingGroup.Children.Remove(_imageDrawing);
							_imageDrawing = CreateImageDrawing(_blockPosition);

							if (IsOnScreen)
							{
								_drawingGroup.Children.Add(_imageDrawing);
							}
						}
						else
						{
							_imageDrawing = CreateImageDrawing(_blockPosition);
						}
					}
				}
			}

			public PointInt ScreenPosition => GetPointInt(_imageDrawing.Rect.Location);

			public bool IsOnScreen => true;
			//{
				//get
				//{
					//bool result;
					//if (!double.IsInfinity(_drawingGroup.Bounds.Width) && _drawingGroup.Bounds.Width > 0)
					//{
					//	//var sp = ScreenPosition;
					//	result = sp.X > 0 && sp.X + _blockSize.Width <= _drawingGroup.Bounds.Width;
					//	//&& sp.Y > 0 && sp.Y + _blockSize.Height <= _drawingGroup.Bounds.Height;
					//}
					//else
					//{
					//	result = true;
					//}

					//return result;
				//}
			//}

			public bool HasPixelData { get; private set; }

			#endregion

			#region Public Methods

			public void WritePixels(byte[] pixels)
			{
				var bitmap = (WriteableBitmap)_imageDrawing.ImageSource;

				var w = (int)Math.Round(bitmap.Width);
				var h = (int)Math.Round(bitmap.Height);

				var rect = new Int32Rect(0, 0, w, h);
				var stride = 4 * w;
				bitmap.WritePixels(rect, pixels, stride, 0);

				Active = true;
			}

			#endregion

			#region Private Methods

			//private ImageDrawing CreateImageDrawing(PointInt blockPosition)
			//{
			//	var image = CreateImage(_blockSize.Width, _blockSize.Height);
			//	var result = CreateImageDrawing(image.Source, blockPosition);

			//	return result;
			//}

			private ImageDrawing CreateImageDrawing(PointInt blockPosition)
			{
				var position = blockPosition.Scale(_blockSize);
				var rect = new Rect(new Point(position.X, position.Y), GetSize(_blockSize));
				var result = new ImageDrawing(_image.Source, rect);

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

			private PointInt GetPointInt(Point p)
			{
				return new PointDbl(p.X, p.Y).Round();
			}

			private Size GetSize(SizeInt size)
			{
				return new Size(size.Width, size.Height);
			}

			#endregion
		}

	}
}

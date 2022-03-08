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

		private SizeInt _canvasSizeInBlocks;
		private int _maxYPtr;

		private VectorInt _startIndex;

		#region Constructor

		public ScreenSectionCollection(SizeInt canvasSizeInBlocks, SizeInt blockSize)
		{
			_canvasSizeInBlocks = canvasSizeInBlocks;
			_maxYPtr = _canvasSizeInBlocks.Height - 1;
			_drawingGroup = new DrawingGroup();

			var foundationRectangle = BuildFoundationRectangle(_canvasSizeInBlocks, blockSize);
			_drawingGroup.Children.Add(foundationRectangle);

			_screenSections = BuildScreenSections(_canvasSizeInBlocks, blockSize, _drawingGroup);
		}

		#endregion

		#region Public Properties

		public DrawingGroup DrawingGroup => _drawingGroup;

		public SizeInt CanvasSizeInWholeBlocks
		{
			get => _canvasSizeInBlocks;
			set
			{
				// TODO: Rebuild the _screenSections and the _foundationRectangle
				//_canvasSizeInBlocks = value;
			}
		}

		public VectorInt CanvasControlOffset { get; set; }

		#endregion

		#region Public Methods

		public void HideScreenSections()
		{
			//_drawingGroup.Children.Clear();

			foreach (var blockPosition in ScreenTypeHelper.Points(_canvasSizeInBlocks))
			{
				var screenSection = GetScreenSection(blockPosition, out var _);
				screenSection.Active = false;
			}
		}

		public void Draw(MapSection mapSection)
		{
			var screenSection = GetScreenSection(mapSection.BlockPosition, out var screenIndex);
			//var desc = mapSection.Pixels1d is null ? "Not drawing" : "Drawing";
			//Debug.WriteLine($"{desc} section: {mapSection.BlockPosition} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}.");

			if (mapSection.Pixels1d is null)
			{
				return;
			}

			var invertedPosition = GetInvertedBlockPos(mapSection.BlockPosition);
			screenSection.Draw(invertedPosition, mapSection.Pixels1d, screenIndex);
		}

		public void Redraw(MapSection mapSection)
		{
			var screenSection = GetScreenSection(mapSection.BlockPosition, out var screenIndex);
			//var desc = mapSection.Pixels1d is null ? "Not drawing" : "Drawing";
			//Debug.WriteLine($"{desc} section: {mapSection.BlockPosition} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}.");

			if (mapSection.Pixels1d is null)
			{
				return;
			}

			var invertedPosition = GetInvertedBlockPos(mapSection.BlockPosition);
			screenSection.ReDraw(invertedPosition, screenIndex);
		}

		public bool Hide(MapSection mapSection)
		{
			var screenSection = GetScreenSection(mapSection.BlockPosition, out var _);
			var result = screenSection.Hide();
			return result;
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

			return result;
		}

		public void Shift(VectorInt amount)
		{
			HideScreenSections();

			var oldStartIndex = _startIndex;
			_startIndex = IndexAdd(_startIndex, amount.Invert());
			//_startIndex = IndexAdd(_startIndex, amount);

			Debug.WriteLine($"The ScreenSectionCollection was shifted by {amount}. StartIndex old: {oldStartIndex}, new: {_startIndex}.");
		}

		#endregion

		#region Private Methods

		private ScreenSection GetScreenSection(PointInt blockPosition, out VectorInt screenIndex)
		{
			//// If the offset is zero, then the first block is completely off-screen. The MapDisplay code-behind sets the drawing group's position to be -1 * blocksize when the offset is zero.
			//var posX = CanvasControlOffset.X == 0 ? blockPosition.X + 1 : blockPosition.X;
			////var posY = CanvasControlOffset.Y == 0 ? blockPosition.Y + 1 : blockPosition.Y;
			//var posY = blockPosition.Y;

			var pos = new VectorInt(blockPosition);
			//var pos = new VectorInt(posX, posY);

			// Use the start index to find the "current" cell in the 2-D Ring Buffer
			var adjPos = IndexAdd(_startIndex, pos);
			var result = _screenSections[adjPos.Y, adjPos.X];

			screenIndex = adjPos;

			return result;
		}

		private VectorInt IndexAdd(VectorInt index, VectorInt amount)
		{
			index = index.Add(amount);
			index = index.Mod(_canvasSizeInBlocks);

			var result = new VectorInt
				(
					index.X += index.X < 0 ? _canvasSizeInBlocks.Width : 0,
					index.Y += index.Y < 0 ? _canvasSizeInBlocks.Height : 0
				);

			return result;
		}

		private ScreenSection[,] BuildScreenSections(SizeInt sizeInBlocks, SizeInt blockSize, DrawingGroup drawingGroup)
		{
			Debug.WriteLine($"Populating {sizeInBlocks} Screen Sections.");

			var result = new ScreenSection[sizeInBlocks.Height, sizeInBlocks.Width];

			foreach (var blockPosition in ScreenTypeHelper.Points(sizeInBlocks))
			{
				var invertedPosition = GetInvertedBlockPos(blockPosition);
				var screenSection = new ScreenSection(invertedPosition, blockSize, drawingGroup);
				result[blockPosition.Y, blockPosition.X] = screenSection;
			}

			return result;
		}

		private Drawing BuildFoundationRectangle(SizeInt sizeInBlocks, SizeInt blockSize)
		{
			var size = sizeInBlocks.Scale(blockSize);
			var rectangle = new RectangleGeometry()
			{
				Rect = new Rect(new Point(), ScreenTypeHelper.ConvertToSize(size))
			};

			var result = new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Black, 1), rectangle);

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
							AddToGroup("setting Active to true");
						}
						else
						{
							RemoveFromGroup("setting Active to false");
						}

						_active = value;
					}
				}
			}

			public PointInt BlockPosition
			{
				get => _blockPosition;
				private set
				{
					if (value != _blockPosition)
					{
						if (Active)
						{
							RemoveFromGroup("setting the BlockPosition");
						}

						_blockPosition = value;
					}
				}
			}

			public PointInt ScreenPosition => ScreenTypeHelper.ConvertToPointInt(_imageDrawing.Rect.Location);

			#endregion

			#region Public Methods

			public bool Hide()
			{
				if (Active)
				{
					Active = false;
					return true;
				}
				else
				{
					return false;
				}
			}

			public void Draw(PointInt position, byte[] pixels, VectorInt screenIndex)
			{
				if (BlockPosition != position)
				{
					//Debug.WriteLine($"Drawing image for {position}, the previous value is {BlockPosition}. SI = {screenIndex}.");

					BlockPosition = position;
					Active = false;
					_imageDrawing = CreateImageDrawing(BlockPosition);
				}
				else
				{
					//Debug.WriteLine($"Drawing image for {position}. SI = {screenIndex}.");
				}

				var bitmap = (WriteableBitmap)_imageDrawing.ImageSource;
				WritePixels(pixels, bitmap);
			}

			public void ReDraw(PointInt position, VectorInt screenIndex)
			{
				if (BlockPosition != position)
				{
					//Debug.WriteLine($"Redrawing image for {position}, the previous value is {BlockPosition}. SI = {screenIndex}.");

					BlockPosition = position;
					Active = false;
					_imageDrawing = CreateImageDrawing(BlockPosition);
				}
				else
				{
					//Debug.WriteLine($"Redrawing image for {position}. SI = {screenIndex}.");
				}

				Debug.Assert(!Active, "Attempting to refresh a screen section that is already active.");
				Active = true;
			}

			#endregion

			#region Private Methods

			private void WritePixels(byte[] pixels, WriteableBitmap bitmap)
			{
				var w = (int)Math.Round(bitmap.Width);
				var h = (int)Math.Round(bitmap.Height);

				var rect = new Int32Rect(0, 0, w, h);
				var stride = 4 * w;
				bitmap.WritePixels(rect, pixels, stride, 0);

				Active = true;
			}

			private ImageDrawing CreateImageDrawing(PointInt blockPosition)
			{
				var position = blockPosition.Scale(_blockSize);
				var rect = ScreenTypeHelper.CreateRect(position, _blockSize);
				var result = new ImageDrawing(_image.Source, rect);

				return result;
			}

			//private ImageDrawing GetDiagnosticVersion(ImageDrawing original)
			//{
			//	var result = original.CloneCurrentValue();
			//	var bitmap = (WriteableBitmap)result.ImageSource;
			//	var ar = new byte[_blockSize.NumberOfCells * 4];
			//	bitmap.CopyPixels(ar, 4 * _blockSize.Width, 0);

			//	for (var rowPtr = 0; rowPtr < _blockSize.Height; rowPtr++)
			//	{
			//		var curResultPtr = rowPtr * _blockSize.Width * 4;

			//		for (var colPtr = 0; colPtr < _blockSize.Width; colPtr++)
			//		{
			//			ar[curResultPtr + 3] = 100; // Alpha is set to 100, instead of 255
			//			curResultPtr += 4;
			//		}
			//	}

			//	WritePixels(ar, bitmap);

			//	return result;
			//}

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

			private void AddToGroup(string opDesc)
			{
				if (_drawingGroup.Children.Contains(_drawingGroup))
				{
					Debug.WriteLine($"While {opDesc}, found that the section with BlockPosition: {_blockPosition} is already added.");
				}
				else
				{
					_drawingGroup.Children.Add(_imageDrawing);
				}
			}

			private void RemoveFromGroup(string opDesc)
			{
				var result = _drawingGroup.Children.Remove(_imageDrawing);

				if (!result)
				{
					Debug.WriteLine($"While {opDesc}, cannot remove section with BlockPosition: {_blockPosition}.");
				}
			}

			#endregion
		}

	}
}

using MSS.Types;
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
		private readonly SizeInt _maxSizeInBlocks;

		private readonly SizeInt _blockSize;
		private readonly int _maxYPtr;

		private readonly DrawingGroup _drawingGroup;

		private readonly GeometryDrawing _foundationRectangle;
		private readonly ScreenSection[,] _screenSections;

		private SizeInt _canvasSizeInBlocks;
		private VectorInt _startIndex;

		#region Constructor

		public ScreenSectionCollection(DrawingGroup drawingGroup, SizeInt blockSize, SizeInt maxSizeInBlocks)
		{
			_drawingGroup = drawingGroup;
			_blockSize = blockSize;
			_maxSizeInBlocks = maxSizeInBlocks;

			_maxYPtr = maxSizeInBlocks.Height - 1;

			_foundationRectangle = BuildFoundationRectangle(maxSizeInBlocks, _blockSize);
			_drawingGroup.Children.Add(_foundationRectangle);

			_screenSections = new ScreenSection[maxSizeInBlocks.Height, maxSizeInBlocks.Width];

			//_canvasSizeInBlocks = new SizeInt(8);
			CanvasSizeInBlocks = new SizeInt(8);
			//Debug.WriteLine($"Allocating {_canvasSizeInBlocks} ScreenSections.");
			//BuildScreenSections(_screenSections, _canvasSizeInBlocks, _blockSize, _drawingGroup);
		}

		#endregion

		#region Public Properties

		private SizeInt _allocatedBlocks;

		public SizeInt CanvasSizeInBlocks
		{
			get => _canvasSizeInBlocks;
			set
			{
				if (value.Width < 0 || value.Height < 0)
				{
					return;
				}

				if (_canvasSizeInBlocks != value)
				{
					_canvasSizeInBlocks = value;
					var prevValue = _allocatedBlocks;
					_allocatedBlocks = _canvasSizeInBlocks.Inflate(2);

					Debug.WriteLine($"Allocating ScreenSections. Old size: {prevValue}, new size: {_allocatedBlocks}.");
					BuildScreenSections(_screenSections, _allocatedBlocks, _blockSize, _drawingGroup);
				}
			}
		}

		#endregion

		#region Public Methods

		public void HideScreenSections()
		{
			foreach (var blockPosition in ScreenTypeHelper.Points(_allocatedBlocks))
			{
				var screenSection = GetScreenSection(blockPosition, out var _);
				if (screenSection != null)
				{
					screenSection.Active = false;
				}
			}
		}

		public void Draw(PointInt position, byte[] pixels)
		{
			var screenSection = GetScreenSection(position, out var screenIndex);

			if (screenSection is null || pixels is null)
			{
				//Debug.WriteLine($"Not drawing section: {position} with screen index: {screenIndex}.");
				return;
			}
			else
			{
				var invertedPosition = GetInvertedBlockPos(position);
				//Debug.WriteLine($"Drawing section: {position} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}. ip = {invertedPosition}");
				screenSection.Draw(invertedPosition, pixels, screenIndex);
			}
		}

		public void Redraw(PointInt position)
		{
			var screenSection = GetScreenSection(position, out var screenIndex);

			if (screenSection != null)
			{
				//Debug.WriteLine($"Redrawing section: {position} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}. si = {screenIndex}");
				var invertedPosition = GetInvertedBlockPos(position);
				screenSection.ReDraw(invertedPosition, screenIndex);
			}
			else
			{
				//Debug.WriteLine($"Not redrwaing section: {position} with screen index: {screenIndex}.");
			}
		}

		public bool Hide(MapSection mapSection)
		{
			var screenSection = GetScreenSection(mapSection.BlockPosition, out var _);

			if (screenSection != null)
			{
				var result = screenSection.Hide();
				if (!result)
				{
					Debug.WriteLine("Cannot hide an inactive mapSection.");
				}
				return result;
			}
			else
			{
				Debug.WriteLine("Cannot hide a null mapSection.");
				return false;
			}
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

			return result;
		}

		public void Shift(VectorInt amount)
		{
			if (amount.X == 0 && amount.Y == 0)
			{
				return;
			}

			//HideScreenSections();

			var oldStartIndex = _startIndex;
			_startIndex = IndexAdd(_startIndex, amount.Invert());

			Debug.WriteLine($"The ScreenSectionCollection was shifted by {amount}. StartIndex old: {oldStartIndex}, new: {_startIndex}.");
		}

		#endregion

		#region Private Methods

		private ScreenSection GetScreenSection(PointInt blockPosition, out VectorInt screenIndex)
		{
			// Use the start index to find the "current" cell in the 2-D Ring Buffer
			screenIndex = IndexAdd(_startIndex, new VectorInt(blockPosition));
			var result = _screenSections[screenIndex.Y, screenIndex.X];

			return result;
		}

		private VectorInt IndexAdd(VectorInt index, VectorInt amount)
		{
			index = index.Add(amount).Mod(_allocatedBlocks);

			int nx = index.X;
			int ny = index.Y;

			if (nx < 0)
			{
				nx += _allocatedBlocks.Width; 
			}

			if (ny < 0)
			{
				ny += CanvasSizeInBlocks.Height;
			}

			var result = new VectorInt(nx, ny);

			return result;
		}

		private void BuildScreenSections(ScreenSection[,] currentSections, SizeInt blocksToAllocate, SizeInt blockSize, DrawingGroup drawingGroup)
		{

			if (blocksToAllocate.Width > _maxSizeInBlocks.Width || blocksToAllocate.Height > _maxSizeInBlocks.Height)
			{
				throw new ArgumentException($"The CanvasSizeInWholeBlocks cannot exceed the maximum supported value of {_maxSizeInBlocks}.");
			}


			foreach (var blockPosition in ScreenTypeHelper.Points(blocksToAllocate))
			{
				//if (currentSections[blockPosition.Y, blockPosition.X] == null)
				//{
					var invertedPosition = GetInvertedBlockPos(blockPosition);
					var screenSection = new ScreenSection(invertedPosition, blockSize, drawingGroup);
					currentSections[blockPosition.Y, blockPosition.X] = screenSection;
				//}
			}
		}

		private GeometryDrawing BuildFoundationRectangle(SizeInt sizeInBlocks, SizeInt blockSize)
		{
			var rectangle = new RectangleGeometry()
			{
				Rect = ScreenTypeHelper.CreateRect(new PointInt(), sizeInBlocks.Scale(blockSize))
			};

			var result = new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Transparent, 1), rectangle);

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
					//Debug.WriteLine($"Creating new ImageDrawing for {position}, the previous value is {BlockPosition}. SI = {screenIndex}.");

					BlockPosition = position;
					Active = false;
					_imageDrawing = CreateImageDrawing(BlockPosition);
				}
				else
				{
					//Debug.WriteLine($"Updating existing ImageDrawing for {position}. SI = {screenIndex}.");
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

				//Debug.Assert(!Active, "Attempting to refresh a screen section that is already active.");
				//Active = true;

				if (Active)
				{
					Debug.WriteLine("Attempting to refresh a screen section that is already active.");
				}
				else
				{
					Active = true;
				}
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
				if (_drawingGroup.Children.Contains(_imageDrawing))
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

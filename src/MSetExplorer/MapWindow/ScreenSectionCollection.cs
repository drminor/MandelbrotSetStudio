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
		private readonly SizeInt _canvasSizeInBlocks;
		private readonly int _maxYPtr;
		private readonly DrawingGroup _drawingGroup;
		private readonly ScreenSection[,] _screenSections;

		private VectorInt _startIndex;

		#region Constructor

		public ScreenSectionCollection(SizeInt canvasSizeInBlocks, SizeInt blockSize, DrawingGroup drawingGroup)
		{
			_canvasSizeInBlocks = canvasSizeInBlocks;
			_maxYPtr = _canvasSizeInBlocks.Height - 1;
			_drawingGroup = drawingGroup;
			_screenSections = BuildScreenSections(_canvasSizeInBlocks, blockSize, _drawingGroup);
		}

		#endregion

		#region Public Methods

		public void HideScreenSections()
		{
			ClearDrawingGroup();
		}

		public void Draw(MapSection mapSection)
		{
			var screenSection = GetScreenSection(mapSection.BlockPosition);
			//var desc = mapSection.Pixels1d is null ? "Not drawing" : "Drawing";
			//Debug.WriteLine($"{desc} section: {mapSection.BlockPosition} with screen pos: {screenSection.ScreenPosition} and dc: {screenSection.BlockPosition}.");

			if (mapSection.Pixels1d is null)
			{
				return;
			}

			var invertedPosition = GetInvertedBlockPos(mapSection.BlockPosition);
			screenSection.WritePixels(invertedPosition, mapSection.Pixels1d);
		}

		public bool Hide(MapSection mapSection)
		{
			var screenSection = GetScreenSection(mapSection.BlockPosition);
			var result = screenSection.Hide();
			return result;
		}

		/// <summary>
		/// Returns the number of sections that were removed from display.
		/// </summary>
		/// <param name="amount">The amount to shift each section.</param>
		/// <returns>The number of sections drawn.</returns>
		public void Shift(VectorInt amount)
		{
			HideScreenSections();

			var oldStartIndex = _startIndex;
			_startIndex = IndexAdd(_startIndex, amount.Invert());

			Debug.WriteLine($"The ScreenSectionCollection was shifted by {amount}. StartIndex old: {oldStartIndex}, new: {_startIndex}.");
		}

		#endregion

		#region Private Methods

		private void ClearDrawingGroup()
		{
			//_drawingGroup.Children.Clear();

			var sizeInBlocks = _canvasSizeInBlocks;
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
			var adjPos = IndexAdd(_startIndex, new VectorInt(blockPosition));

			var result = _screenSections[adjPos.Y, adjPos.X];
			return result;
		}

		//private void PutScreenSection(PointInt blockPosition, ScreenSection screenSection)
		//{
		//	var adjPos = blockPosition.Translate(_startIndex).Mod(CanvasSizeInBlocks);
		//  var adjPos = IndexAdd(_startIndex, new VectorInt(blockPosition));
		//	_screenSections[adjPos.Y, adjPos.X] = screenSection;
		//}

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

			for (var yBlockPtr = 0; yBlockPtr < sizeInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < sizeInBlocks.Width; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);
					var invertedPosition = GetInvertedBlockPos(blockPosition);
					var screenSection = new ScreenSection(invertedPosition, blockSize, drawingGroup);

					result[blockPosition.Y, blockPosition.X] = screenSection;
				}
			}

			return result;
		}

		private PointInt GetInvertedBlockPos(PointInt blockPosition)
		{
			var result = new PointInt(blockPosition.X, _maxYPtr - blockPosition.Y);

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

			public PointInt ScreenPosition => ConvertToPointInt(_imageDrawing.Rect.Location);

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

			public void WritePixels(PointInt position, byte[] pixels)
			{
				if (BlockPosition != position)
				{
					BlockPosition = position;
					_imageDrawing = CreateImageDrawing(BlockPosition);
				}

				var bitmap = (WriteableBitmap)_imageDrawing.ImageSource;
				WritePixels(pixels, bitmap);
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
				var rect = new Rect(new Point(position.X, position.Y), ConvertToSize(_blockSize));
				var result = new ImageDrawing(_image.Source, rect);

				return result;
			}

			private ImageDrawing GetDiagnosticVersion(ImageDrawing original)
			{
				var result = original.CloneCurrentValue();
				var bitmap = (WriteableBitmap)result.ImageSource;
				var ar = new byte[_blockSize.NumberOfCells * 4];
				bitmap.CopyPixels(ar, 4 * _blockSize.Width, 0);

				for (var rowPtr = 0; rowPtr < _blockSize.Height; rowPtr++)
				{
					var curResultPtr = rowPtr * _blockSize.Width * 4;

					for (var colPtr = 0; colPtr < _blockSize.Width; colPtr++)
					{
						ar[curResultPtr + 3] = 100; // Alpha is set to 100, instead of 255
						curResultPtr += 4;
					}
				}

				WritePixels(ar, bitmap);

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

			private PointInt ConvertToPointInt(Point p)
			{
				return new PointDbl(p.X, p.Y).Round();
			}

			private Size ConvertToSize(SizeInt size)
			{
				return new Size(size.Width, size.Height);
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

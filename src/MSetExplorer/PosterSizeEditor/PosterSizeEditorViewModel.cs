using MSS.Common;
using MSS.Types;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;

namespace MSetExplorer
{
	public class PosterSizeEditorViewModel : ViewModelBase, IDataErrorInfo
	{
		private bool _preserveAspectRatio;
		private SizeInt _currentSize;

		private bool _preserveWidth;
		private int _beforeX;
		private int _afterX;

		private bool _preserveHeight;
		private int _beforeY;
		private int _afterY;

		private PreviewImageLayoutInfo _layoutInfo;

		private SizeInt _originalSize;

		#region Constructor

		public PosterSizeEditorViewModel(ImageSource previewImage, SizeInt posterSize, SizeDbl? displaySize = null)
		{
			PreviewImage = previewImage;

			var previewImageSize = new SizeDbl(previewImage.Width, previewImage.Height);
			var containerSize = displaySize ?? new SizeDbl(300, 300);
			_layoutInfo = new PreviewImageLayoutInfo(new SizeDbl(posterSize), previewImageSize, containerSize);

			_currentSize = new SizeInt(2, 1);
			Width = posterSize.Width;
			Height = posterSize.Height;

			_beforeX = 0;
			_afterX = 0;
			_beforeY = 0;
			_afterY = 0;

			_originalSize = new SizeInt(2, 1);
			OriginalWidth = Width;
			OriginalHeight = Height;
			OriginalAspectRatio = _originalSize.AspectRatio;

			_preserveWidth = true;
			_preserveHeight = true;
		}

		#endregion

		#region Public Properties Bound to UI

		public bool PreserveAspectRatio
		{
			get => _preserveAspectRatio;
			set
			{
				if (value != _preserveAspectRatio)
				{
					_preserveAspectRatio = value;

					if (value)
					{
						var previousSize = _currentSize;
						_currentSize = RestoreAspectRatio(new SizeDbl(_currentSize), _originalSize.AspectRatio).Round();

						if (previousSize.Width != _currentSize.Width)
						{
							OnPropertyChanged(nameof(Width));
						}

						if (previousSize.Height != _currentSize.Height)
						{
							OnPropertyChanged(nameof(Height));
						}

						if (previousSize.Width != _currentSize.Width || previousSize.Height != _currentSize.Height)
						{
							OnPropertyChanged(nameof(AspectRatio));
						}
					}

					OnPropertyChanged();
				}
			}
		}

		public int Width
		{
			get => _currentSize.Width;
			set
			{
				if (value != _currentSize.Width)
				{
					var previousSize = _currentSize;
					if (PreserveAspectRatio)
					{
						_currentSize = new SizeInt(value, (int)Math.Round(value / OriginalAspectRatio));
						OnPropertyChanged();

						if (_currentSize.Height != previousSize.Height)
						{
							OnPropertyChanged(nameof(Height));
						}

						SetOffsetsForNewSize(previousSize, _currentSize);
					}
					else
					{
						_currentSize = new SizeInt(value, _currentSize.Height);
						OnPropertyChanged();
						OnPropertyChanged(nameof(AspectRatio));

						SetOffsetsForNewSize(previousSize, _currentSize);
					}

					NewMapArea = new RectangleDbl(new PointDbl(BeforeX, BeforeY), new SizeDbl(_currentSize));
				}
			}
		}

		public int Height
		{
			get => _currentSize.Height;
			set
			{
				if (value != _currentSize.Height)
				{
					var previousSize = _currentSize;

					if (PreserveAspectRatio)
					{
						_currentSize = new SizeInt((int)Math.Round(value * OriginalAspectRatio), value);
						OnPropertyChanged();

						if (_currentSize.Width != previousSize.Width)
						{
							OnPropertyChanged(nameof(Width));
						}

						SetOffsetsForNewSize(previousSize, _currentSize);
					}
					else
					{
						_currentSize = new SizeInt(_currentSize.Width, value);
						OnPropertyChanged();
						OnPropertyChanged(nameof(AspectRatio));

						SetOffsetsForNewSize(previousSize, _currentSize);
					}

					NewMapArea = new RectangleDbl(new PointDbl(BeforeX, BeforeY), new SizeDbl(_currentSize));
				}
			}
		}

		public bool PreserveWidth
		{
			get => _preserveWidth;
			set
			{
				if (value != _preserveWidth)
				{
					_preserveWidth = value;
					OnPropertyChanged();
				}
			}
		}

		public int BeforeX
		{
			get => _beforeX;
			set
			{
				if (value != _beforeX && value >= 0 && !(PreserveWidth && value > _currentSize.Width - _originalSize.Width))
				{
					var previous = _beforeX;

					_beforeX = value;
					OnPropertyChanged();

					if (PreserveWidth)
					{
						_afterX = _afterX - (value - previous);
						OnPropertyChanged(nameof(AfterX));
					}

					NewMapArea = HandleBeforeXUpdate(previous, value);
				}
			}
		}

		public int AfterX
		{
			get => _afterX;
			set
			{
				if (value != _afterX && value >= 0 && !(PreserveWidth && value > _currentSize.Width - _originalSize.Width))
				{
					var previous = _afterX;
					_afterX = value;
					OnPropertyChanged();

					NewMapArea = HandleAfterXUpdate(previous, value);
				}
			}
		}

		public bool PreserveHeight
		{
			get => _preserveHeight;
			set
			{
				if (value != _preserveHeight)
				{
					_preserveHeight = value;
					OnPropertyChanged();
				}
			}
		}

		public int BeforeY
		{
			get => _beforeY;
			set
			{
				if (value != _beforeY && value >= 0 && !(PreserveHeight && value > _currentSize.Height - _originalSize.Height)) 
				{
					var previous = _beforeY;
					_beforeY = value;
					OnPropertyChanged();

					if (PreserveHeight)
					{
						_afterY = _afterY - (value - previous);
						OnPropertyChanged(nameof(AfterY));
					}

					NewMapArea = HandleBeforeYUpdate(previous, value);
				}
			}
		}

		public int AfterY
		{
			get => _afterY;
			set
			{
				if (value != _afterY && value >= 0 && !(PreserveHeight && value > _currentSize.Height - _originalSize.Height))
				{
					var previous = _afterY;
					_afterY = value;
					OnPropertyChanged();

					NewMapArea = HandleAfterYUpdate(previous, value);
				}
			}
		}

		#endregion

		#region Public Properties - Calculation Support

		public SizeDbl ContainerSize
		{
			get => _layoutInfo.ContainerSize;
			set
			{
				if (value != _layoutInfo.ContainerSize)
				{
					_layoutInfo.ContainerSize = value;
					_layoutInfo.Update();
					OnPropertyChanged(nameof(LayoutInfo));

					Debug.WriteLine($"The container size is now {value}.");
					OnPropertyChanged();
				}
			}
		}

		public RectangleDbl NewMapArea
		{
			get => _layoutInfo.NewMapArea;
			set
			{
				if (value != _layoutInfo.NewMapArea)
				{
					_layoutInfo.NewMapArea = value;
					_layoutInfo.Update();
					OnPropertyChanged(nameof(LayoutInfo));

					OnPropertyChanged();

					var previousAspect = _currentSize.AspectRatio;
					var newSize = value.Size.Round();

					if (newSize.Width != _currentSize.Width && newSize.Height != _currentSize.Height) 
					{
						_currentSize = newSize;
						OnPropertyChanged(nameof(Width));
						OnPropertyChanged(nameof(Height));
					}
					else if (newSize.Width != _currentSize.Width)
					{
						_currentSize = newSize;
						OnPropertyChanged(nameof(Width));
					}
					else if (newSize.Height != _currentSize.Height)
					{
						OnPropertyChanged(nameof(Height));
					}

					if (_currentSize.AspectRatio != previousAspect)
					{
						OnPropertyChanged(nameof(AspectRatio));
					}

					//var newPos = value.Position.Round();
					//BeforeX = newPos.X;
					//BeforeY = newPos.Y;
				}
			}
		}

		public PreviewImageLayoutInfo LayoutInfo => _layoutInfo;

		#endregion

		#region Public Properties - UI Display

		public ImageSource PreviewImage { get; init; }

		public double AspectRatio => _currentSize.AspectRatio;

		public int OriginalWidth
		{
			get => _originalSize.Width;
			set
			{
				if (value != _originalSize.Width)
				{
					_originalSize = new SizeInt(value, _originalSize.Height);
					OnPropertyChanged();
					OnPropertyChanged(nameof(OriginalAspectRatio));
				}
			}
		}

		public int OriginalHeight
		{
			get => _originalSize.Height;
			set
			{
				if (value != _originalSize.Height)
				{
					_originalSize = new SizeInt(_originalSize.Width, value);
					OnPropertyChanged();
					OnPropertyChanged(nameof(OriginalAspectRatio));
				}
			}
		}

		public double OriginalAspectRatio { get; init; }


		#endregion

		#region IDataErrorInfo Support

		public string this[string columnName]
		{
			get
			{
				switch (columnName)
				{
					case nameof(BeforeX):
						if (BeforeX < 0 || (PreserveWidth && BeforeX > _currentSize.Width - _originalSize.Width))
						{
							return $"BeforeX must be between 0 and {_currentSize.Width - _originalSize.Width}";
						}
						break;

					case nameof(AfterX):
						if (AfterX < 0 || (PreserveWidth && AfterX > _currentSize.Width - _originalSize.Width))
						{
							return $"AfterX must be between 0 and {_currentSize.Width - _originalSize.Width}";
						}
						break;

					case nameof(BeforeY):
						if (BeforeY < 0 || (PreserveHeight && BeforeY > _currentSize.Height - _originalSize.Height))
						{
							return $"BeforeY must be between 0 and {_currentSize.Height - _originalSize.Height}";
						}
						break;

					case nameof(AfterY):
						if (AfterY < 0 || (PreserveHeight && AfterY > _currentSize.Height - _originalSize.Height))
						{
							return $"AfterY must be between 0 and {_currentSize.Height - _originalSize.Height}";
						}
						break;
				}

				return string.Empty;
			}
		}

		public string Error => string.Empty;

		#endregion

		#region Private Methods

		private SizeDbl RestoreAspectRatio(SizeDbl newSize, double aspectRatio)
		{
			SizeDbl result;

			if (newSize.Width >= newSize.Height)
			{
				result = new SizeDbl(newSize.Width, newSize.Width / aspectRatio);
			}
			else
			{
				result = new SizeDbl(newSize.Height * aspectRatio, newSize.Height);
			}

			return result;
		}

		private void SetOffsetsForNewSize(SizeInt previousSize, SizeInt size)
		{
			var delta = size.Sub(previousSize);

			if (delta.Width != 0)
			{
				var halfDeltaW = Math.DivRem(delta.Width, 2, out var remainderW);
				_beforeX += halfDeltaW;
				_afterX += halfDeltaW + remainderW;

				OnPropertyChanged(nameof(BeforeX));
				OnPropertyChanged(nameof(AfterX));
			}

			if (delta.Height != 0)
			{
				var halfDeltaH = Math.DivRem(delta.Height, 2, out var remainderH);
				_beforeY += halfDeltaH;
				_afterY += halfDeltaH + remainderH;

				OnPropertyChanged(nameof(BeforeY));
				OnPropertyChanged(nameof(AfterY));
			}
		}

		private RectangleDbl HandleBeforeXUpdate(int previous, int val)
		{
			if (val < 0 || (PreserveWidth && val > _currentSize.Width - _originalSize.Width))
			{
				throw new InvalidOperationException($"The before or after horizontal offset must be between 0 and {_currentSize.Width - _originalSize.Width}.");
			}

			var delta = val - previous;

			RectangleDbl result;

			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveWidth)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else if (PreserveAspectRatio)
			{
				var width = NewMapArea.Size.Width + delta;
				var height = width / AspectRatio;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}
			else
			{
				var width = NewMapArea.Size.Width + delta;
				result = new RectangleDbl(newPos, new SizeDbl(width, NewMapArea.Size.Height));
			}

			return result;
		}

		private RectangleDbl HandleAfterXUpdate(int previous, int val)
		{
			if (val < 0 || (PreserveWidth && val > _currentSize.Width - _originalSize.Width))
			{
				throw new InvalidOperationException($"The before or after horizontal offset must be between 0 and {_currentSize.Width - _originalSize.Width}.");
			}

			var delta = val - previous;

			RectangleDbl result;

			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveWidth)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else if (PreserveAspectRatio)
			{
				var width = NewMapArea.Size.Width + delta;
				var height = width / AspectRatio;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}
			else
			{
				var width = NewMapArea.Size.Width + delta;
				result = new RectangleDbl(newPos, new SizeDbl(width, NewMapArea.Size.Height));
			}

			return result;
		}

		private RectangleDbl HandleBeforeYUpdate(int previous, int val)
		{
			if (val < 0 || (PreserveHeight && val > _currentSize.Height - _originalSize.Height))
			{
				throw new InvalidOperationException($"The before or after vertical offset must be between 0 and {_currentSize.Height - _originalSize.Height}.");
			}

			var delta = val - previous;

			RectangleDbl result;

			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveHeight)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else if (PreserveAspectRatio)
			{
				var height = NewMapArea.Size.Height + delta;
				var width = height * AspectRatio;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}
			else
			{
				var height = NewMapArea.Size.Height + delta;
				result = new RectangleDbl(newPos, new SizeDbl(NewMapArea.Size.Width, height));
			}

			return result;
		}

		private RectangleDbl HandleAfterYUpdate(int previous, int val)
		{
			if (val < 0 || (PreserveHeight && val > _currentSize.Height - _originalSize.Height))
			{
				throw new InvalidOperationException($"The before or after vertical offset must be between 0 and {_currentSize.Height - _originalSize.Height}.");
			}

			var delta = val - previous;

			RectangleDbl result;

			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveHeight)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else if (PreserveAspectRatio)
			{
				var height = NewMapArea.Size.Height + delta;
				var width = height * AspectRatio;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}
			else
			{
				var height = NewMapArea.Size.Height + delta;
				result = new RectangleDbl(newPos, new SizeDbl(NewMapArea.Size.Width, height));
			}

			return result;
		}

		#endregion
	}
}

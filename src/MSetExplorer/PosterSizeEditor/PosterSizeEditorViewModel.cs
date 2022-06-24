using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	public class PosterSizeEditorViewModel : ViewModelBase //, IDataErrorInfo
	{
		private readonly DrawingGroup _drawingGroup;
		private readonly ScaleTransform _scaleTransform;

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

		private ImageDrawing _previewImageDrawing;
		private ImageSource _previewImage;

		private readonly LazyMapPreviewImageProvider _lazyMapPreviewImageProvider;

		#region Constructor

		public PosterSizeEditorViewModel(LazyMapPreviewImageProvider lazyMapPreviewImageProvider)
		{
			_lazyMapPreviewImageProvider = lazyMapPreviewImageProvider;
			_scaleTransform = new ScaleTransform();

			_drawingGroup = new DrawingGroup
			{
				Transform = _scaleTransform
			};

			_previewImageDrawing = new ImageDrawing();
			_drawingGroup.Children.Add(_previewImageDrawing);

			_previewImage = new DrawingImage(_drawingGroup);
			_layoutInfo = new PreviewImageLayoutInfo();

			_lazyMapPreviewImageProvider.BitmapHasBeenLoaded += MapPreviewImageProvider_BitmapHasBeenLoaded;
		}

		private void MapPreviewImageProvider_BitmapHasBeenLoaded(object? sender, EventArgs e)
		{
			var previewImage = _lazyMapPreviewImageProvider.Bitmap;

			_ = _drawingGroup.Children.Remove(_previewImageDrawing);
			_previewImageDrawing = CreateImageDrawing(previewImage);
			_drawingGroup.Children.Add(_previewImageDrawing);
			_previewImage = new DrawingImage(_drawingGroup);
		}

		public void Initialize(JobAreaInfo posterMapAreaInfo, SizeDbl containerSize)
		{
			UpdateWithChangesInternal(posterMapAreaInfo, containerSize);

			_preserveAspectRatio = true;
			_preserveWidth = true;
			_preserveHeight = true;

			OnPropertyChanged(nameof(PreserveAspectRatio));
			OnPropertyChanged(nameof(PreserveWidth));
			OnPropertyChanged(nameof(PreserveHeight));

			var newMapArea = new RectangleDbl(new PointDbl(), new SizeDbl(posterMapAreaInfo.CanvasSize));
			InvertAndSetNewMapArea(newMapArea);
		}

		public void UpdateWithNewMapInfo(JobAreaInfo posterMapAreaInfo)
		{
			UpdateWithChangesInternal(posterMapAreaInfo, ContainerSize);

			var newMapArea = new RectangleDbl(new PointDbl(), new SizeDbl(posterMapAreaInfo.CanvasSize));
			InvertAndSetNewMapArea(newMapArea);
		}

		private void UpdateWithChangesInternal(JobAreaInfo posterMapAreaInfo, SizeDbl containerSize)
		{
			PosterMapAreaInfo = posterMapAreaInfo;

			var previewImage = _lazyMapPreviewImageProvider.Bitmap;
			var previewImageSize = new SizeDbl(previewImage.Width, previewImage.Height);

			//_drawingGroup.Children.Remove(_previewImageDrawing);
			//_previewImageDrawing = CreateImageDrawing(previewImage);
			//_drawingGroup.Children.Add(_previewImageDrawing);
			//_previewImage = new DrawingImage(_drawingGroup);

			var posterSize = posterMapAreaInfo.CanvasSize;
			_lazyMapPreviewImageProvider.MapAreaInfo = posterMapAreaInfo;

			_layoutInfo = new PreviewImageLayoutInfo(posterSize, previewImageSize, containerSize);

			_originalSize = posterSize;
			OnPropertyChanged(nameof(OriginalWidth));
			OnPropertyChanged(nameof(OriginalHeight));
			OnPropertyChanged(nameof(OriginalAspectRatio));
		}

		private ImageDrawing CreateImageDrawing(ImageSource previewImage)
		{
			var rect = new Rect(new Size(previewImage.Width, previewImage.Height));
			var result = new ImageDrawing(previewImage, rect);
			return result;
		}

		#endregion

		#region Public Properties Bound to UI

		public bool PreserveAspectRatio
		{
			get => _preserveAspectRatio;
			set
			{
				if (!_layoutInfo.IsEmpty && value != _preserveAspectRatio)
				{
					_preserveAspectRatio = value;

					if (value)
					{
						var newSize = RestoreAspectRatio(new SizeDbl(_currentSize), _originalSize.AspectRatio).Round();
						var newMapArea = new RectangleDbl(new PointDbl(), new SizeDbl(newSize));
						InvertAndSetNewMapArea(newMapArea);

						_beforeX = 0; _afterX = 0; _beforeY = 0; _afterY = 0;
						OnPropertyChanged(nameof(BeforeX));
						OnPropertyChanged(nameof(AfterX));
						OnPropertyChanged(nameof(BeforeY));
						OnPropertyChanged(nameof(AfterY));
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
				if (!_layoutInfo.IsEmpty && value != _currentSize.Width)
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
					}
					else
					{
						_currentSize = new SizeInt(value, _currentSize.Height);
						OnPropertyChanged();
						OnPropertyChanged(nameof(AspectRatio));

						SetOffsetsForNewSize(previousSize, _currentSize);
					}

					var newMapArea = new RectangleDbl(new PointDbl(BeforeX, BeforeY), new SizeDbl(_currentSize));
					InvertAndSetNewMapArea(newMapArea);
				}
			}
		}

		public int Height
		{
			get => _currentSize.Height;
			set
			{
				if (!_layoutInfo.IsEmpty && value != _currentSize.Height)
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
					}
					else
					{
						_currentSize = new SizeInt(_currentSize.Width, value);
						OnPropertyChanged();
						OnPropertyChanged(nameof(AspectRatio));

						SetOffsetsForNewSize(previousSize, _currentSize);
					}

					var newMapArea = new RectangleDbl(new PointDbl(BeforeX, BeforeY), new SizeDbl(_currentSize));
					InvertAndSetNewMapArea(newMapArea);
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
				if (!_layoutInfo.IsEmpty && value != _beforeX)
				{
					var previous = _beforeX;

					_beforeX = value;
					OnPropertyChanged();

					if (PreserveWidth)
					{
						_afterX = _afterX - (value - previous);
						OnPropertyChanged(nameof(AfterX));
					}

					var newMapArea = HandleBeforeXUpdate(previous, value);
					InvertAndSetNewMapArea(newMapArea);
				}
			}
		}

		public int AfterX
		{
			get => _afterX;
			set
			{
				if (!_layoutInfo.IsEmpty && value != _afterX)
				{
					var previous = _afterX;
					_afterX = value;
					OnPropertyChanged();

					if (PreserveWidth)
					{
						_beforeX = _beforeX - (value - previous);
						OnPropertyChanged(nameof(BeforeX));
					}

					var newMapArea = HandleAfterXUpdate(previous, value);
					InvertAndSetNewMapArea(newMapArea);
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
				if (!_layoutInfo.IsEmpty && value != _beforeY)
				{
					var previous = _beforeY;
					_beforeY = value;
					OnPropertyChanged();

					if (PreserveHeight)
					{
						_afterY = _afterY - (value - previous);
						OnPropertyChanged(nameof(AfterY));
					}

					var newMapArea = HandleBeforeYUpdate(previous, value);
					InvertAndSetNewMapArea(newMapArea);
				}
			}
		}

		public int AfterY
		{
			get => _afterY;
			set
			{
				if (!_layoutInfo.IsEmpty && value != _afterY)
				{
					var previous = _afterY;
					_afterY = value;
					OnPropertyChanged();

					if (PreserveHeight)
					{
						_beforeY = _beforeY - (value - previous);
						OnPropertyChanged(nameof(BeforeY));
					}

					var newMapArea = HandleAfterYUpdate(previous, value);
					InvertAndSetNewMapArea(newMapArea);
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
				if (!_layoutInfo.IsEmpty && value != _layoutInfo.ContainerSize)
				{
					_layoutInfo.ContainerSize = value;
					_layoutInfo.Update();
					_scaleTransform.ScaleX = _layoutInfo.ScaleFactorForPreviewImage;
					_scaleTransform.ScaleY = _layoutInfo.ScaleFactorForPreviewImage;
					OnPropertyChanged(nameof(LayoutInfo));

					Debug.WriteLine($"The container size is now {value}.");
					OnPropertyChanged();
				}
			}
		}

		private void InvertAndSetNewMapArea(RectangleDbl newMapAreaInverted)
		{
			var unInverted = new RectangleDbl(newMapAreaInverted.Position.Invert(), newMapAreaInverted.Size);
			NewMapArea = unInverted;
		}

		public RectangleDbl NewMapArea
		{
			get => _layoutInfo.NewMapArea;
			
			private set
			{
				if (!_layoutInfo.IsEmpty && value != _layoutInfo.NewMapArea)
				{
					_layoutInfo.NewMapArea = value;
					_layoutInfo.Update();
					_scaleTransform.ScaleX = _layoutInfo.ScaleFactorForPreviewImage;
					_scaleTransform.ScaleY = _layoutInfo.ScaleFactorForPreviewImage;

					OnPropertyChanged(nameof(LayoutInfo));

					//OnPropertyChanged();

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
						_currentSize = newSize;
						OnPropertyChanged(nameof(Height));
					}

					if (_currentSize.AspectRatio != previousAspect)
					{
						OnPropertyChanged(nameof(AspectRatio));
					}
				}
			}
		}

		public PreviewImageLayoutInfo LayoutInfo => _layoutInfo;

		public JobAreaInfo? PosterMapAreaInfo { get; private set; }

		public ImageSource PreviewImage => _previewImage;
		//{
		//	get => _previewImage;
		//	set
		//	{
		//		_drawingGroup.Children.Remove(_previewImageDrawing);
		//		_previewImageDrawing = CreateImageDrawing(value);
		//		_drawingGroup.Children.Add(_previewImageDrawing);

		//		_layoutInfo.PreviewImageSize = new SizeDbl(value.Width, value.Height);
		//	}
		//}

		#endregion

		#region Public Properties - UI Display

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

		public double OriginalAspectRatio => _originalSize.AspectRatio;

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
			RectangleDbl result;
			var delta = val - previous;
			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveWidth)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else
			{
				var width = NewMapArea.Size.Width + delta;
				var height = PreserveAspectRatio ? width / AspectRatio : NewMapArea.Size.Height;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}

			return result;
		}

		private RectangleDbl HandleAfterXUpdate(int previous, int val)
		{
			RectangleDbl result;
			var delta = val - previous;
			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveWidth)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else
			{
				var width = NewMapArea.Size.Width + delta;
				var height = PreserveAspectRatio ? width / AspectRatio : NewMapArea.Size.Height;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}

			return result;
		}

		private RectangleDbl HandleBeforeYUpdate(int previous, int val)
		{
			RectangleDbl result;
			var delta = val - previous;
			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveHeight)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else
			{
				var height = NewMapArea.Size.Height + delta;
				var width = PreserveAspectRatio ? height * AspectRatio : NewMapArea.Size.Width;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}

			return result;
		}

		private RectangleDbl HandleAfterYUpdate(int previous, int val)
		{
			RectangleDbl result;
			var delta = val - previous;
			var newPos = new PointDbl(BeforeX, BeforeY);

			if (PreserveHeight)
			{
				result = new RectangleDbl(newPos, NewMapArea.Size);
			}
			else
			{
				var height = NewMapArea.Size.Height + delta;
				var width = PreserveAspectRatio ? height * AspectRatio : NewMapArea.Size.Width;
				result = new RectangleDbl(newPos, new SizeDbl(width, height));
			}

			return result;
		}

		#endregion
	}
}

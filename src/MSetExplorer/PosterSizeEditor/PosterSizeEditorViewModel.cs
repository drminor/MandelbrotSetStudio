using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	public class PosterSizeEditorViewModel : ViewModelBase //, IDataErrorInfo
	{
		private readonly DrawingGroup _drawingGroup;
		private readonly ScaleTransform _scaleTransform;

		private bool _preserveAspectRatio;
		private SizeDbl _currentSize;
		private double _scaleFactorCurrentToOrginal;
		private SizeDbl _currentSizeScaled;

		private bool _preserveWidth;
		private double _beforeX;
		private double _afterX;

		private bool _preserveHeight;
		private double _beforeY;
		private double _afterY;

		private PreviewImageLayoutInfo _layoutInfo;

		private SizeDbl _originalSize;

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
			_previewImage = new DrawingImage(_drawingGroup);
			_layoutInfo = new PreviewImageLayoutInfo();

			// Load Placeholder preview image (until the real preview is generated.)
			LoadPreviewImage(_lazyMapPreviewImageProvider.Bitmap);

			_lazyMapPreviewImageProvider.BitmapHasBeenLoaded += MapPreviewImageProvider_BitmapHasBeenLoaded;
		}

		private void MapPreviewImageProvider_BitmapHasBeenLoaded(object? sender, EventArgs e)
		{
			var previewImage = _lazyMapPreviewImageProvider.Bitmap;
			LoadPreviewImage(previewImage);
		}

		private void LoadPreviewImage(WriteableBitmap previewImage)
		{
			_ = _drawingGroup.Children.Remove(_previewImageDrawing);
			_previewImageDrawing = CreateImageDrawing(previewImage);
			_drawingGroup.Children.Add(_previewImageDrawing);
			_previewImage = new DrawingImage(_drawingGroup);
		}

		public void Initialize(JobAreaInfo posterMapAreaInfo, SizeDbl containerSize)
		{
			_preserveAspectRatio = true;
			_preserveWidth = true;
			_preserveHeight = true;

			OnPropertyChanged(nameof(PreserveAspectRatio));
			OnPropertyChanged(nameof(PreserveWidth));
			OnPropertyChanged(nameof(PreserveHeight));
			
			UpdateWithChangesInternal(posterMapAreaInfo, containerSize);

			BeforeOffset = new PointDbl(BeforeX, BeforeY);
			AfterOffset = new PointDbl(AfterX, AfterY);

			PerformLayout();
		}

		public void UpdateWithNewMapInfo(JobAreaInfo posterMapAreaInfo)
		{
			UpdateWithChangesInternal(posterMapAreaInfo, ContainerSize);

			_beforeX = 0; _afterX = 0; _beforeY = 0; _afterY = 0;
			OnPropertyChanged(nameof(BeforeX));
			OnPropertyChanged(nameof(AfterX));
			OnPropertyChanged(nameof(BeforeY));
			OnPropertyChanged(nameof(AfterY));

			BeforeOffset = new PointDbl(BeforeX, BeforeY);
			AfterOffset = new PointDbl(AfterX, AfterY);

			PerformLayout();
		}

		private void UpdateWithChangesInternal(JobAreaInfo posterMapAreaInfo, SizeDbl containerSize)
		{
			PosterMapAreaInfo = posterMapAreaInfo;
			_lazyMapPreviewImageProvider.MapAreaInfo = posterMapAreaInfo;

			var previewImage = _lazyMapPreviewImageProvider.Bitmap;

			var posterSize = new SizeDbl(posterMapAreaInfo.CanvasSize);
			var previewImageSize = new SizeDbl(previewImage.Width, previewImage.Height);
			_layoutInfo = new PreviewImageLayoutInfo(posterSize, previewImageSize, containerSize);

			_originalSize = posterSize;
			_currentSize = posterSize;
			_scaleFactorCurrentToOrginal = 1;
			_currentSizeScaled = posterSize;

			OnPropertyChanged(nameof(Width));
			OnPropertyChanged(nameof(Height));
			NewMapSize = _currentSize;

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
						_currentSizeScaled = RestoreAspectRatio(_currentSizeScaled, _originalSize.AspectRatio);

						_scaleFactorCurrentToOrginal = _currentSizeScaled.Width / _originalSize.Width;

						_currentSize = _currentSizeScaled.Scale(1 / _scaleFactorCurrentToOrginal);
						OnPropertyChanged(nameof(Width));
						OnPropertyChanged(nameof(Height));
						NewMapSize = _currentSize;

						_beforeX = 0; _afterX = 0; _beforeY = 0; _afterY = 0;
						OnPropertyChanged(nameof(BeforeX));
						OnPropertyChanged(nameof(AfterX));
						OnPropertyChanged(nameof(BeforeY));
						OnPropertyChanged(nameof(AfterY));

						BeforeOffset = new PointDbl(BeforeX, BeforeY);
						AfterOffset = new PointDbl(AfterX, AfterY);
						PerformLayout();
					}

					OnPropertyChanged();
				}
			}
		}

		public int Width
		{
			get => (int)Math.Round(_currentSize.Width);
			set
			{
				if (!_layoutInfo.IsEmpty && value != _currentSize.Width)
				{
					var previousSize = _currentSize;
					if (PreserveAspectRatio)
					{
						_currentSize = new SizeDbl(value, value / OriginalAspectRatio);

						_scaleFactorCurrentToOrginal = _originalSize.Width / _currentSize.Width;
						_currentSizeScaled = _currentSize.Scale(_scaleFactorCurrentToOrginal);

						OnPropertyChanged();

						if (_currentSize.Height != previousSize.Height)
						{
							OnPropertyChanged(nameof(Height));
						}
					}
					else
					{
						_currentSize = new SizeDbl(value, _currentSize.Height);

						var newSizeScaled = SetOffsetsForNewSize(previousSize.Scale(_scaleFactorCurrentToOrginal), _currentSize.Scale(_scaleFactorCurrentToOrginal));
						_scaleFactorCurrentToOrginal = _originalSize.Width / newSizeScaled.Width;
						_currentSizeScaled = newSizeScaled;

						_currentSize = _currentSizeScaled.Scale(1 / _scaleFactorCurrentToOrginal);

						OnPropertyChanged();

						if (_currentSize.Height != previousSize.Height)
						{
							OnPropertyChanged(nameof(Height));
						}

						OnPropertyChanged(nameof(AspectRatio));

						BeforeOffset = new PointDbl(BeforeX, BeforeY);
						AfterOffset = new PointDbl(AfterX, AfterY);
					}

					Debug.WriteLine($"User is changing the Width from {previousSize.Width} to {_currentSize.Width}.");
					NewMapSize = _currentSize;
					PerformLayout();
				}
			}
		}

		public int Height
		{
			get => (int)Math.Round(_currentSize.Height);
			set
			{
				if (!_layoutInfo.IsEmpty && value != _currentSize.Height)
				{
					var previousSize = _currentSize;
					if (PreserveAspectRatio)
					{
						_currentSize = new SizeDbl(value * OriginalAspectRatio, value);
						_scaleFactorCurrentToOrginal = _originalSize.Width / _currentSize.Width;
						_currentSizeScaled = _currentSize.Scale(_scaleFactorCurrentToOrginal);

						OnPropertyChanged();

						if (_currentSize.Width != previousSize.Width)
						{
							OnPropertyChanged(nameof(Width));
						}
					}
					else
					{
						_currentSize = new SizeDbl(_currentSize.Width, value);

						var newSizeScaled = SetOffsetsForNewSize(previousSize.Scale(_scaleFactorCurrentToOrginal), _currentSize.Scale(_scaleFactorCurrentToOrginal));
						_scaleFactorCurrentToOrginal = _originalSize.Width / newSizeScaled.Width;
						_currentSizeScaled = newSizeScaled;

						_currentSize = _currentSizeScaled.Scale(1 / _scaleFactorCurrentToOrginal);

						OnPropertyChanged();

						if (_currentSize.Width != previousSize.Width)
						{
							OnPropertyChanged(nameof(Width));
						}

						OnPropertyChanged(nameof(AspectRatio));

						BeforeOffset = new PointDbl(BeforeX, BeforeY);
						AfterOffset = new PointDbl(AfterX, AfterY);
					}

					Debug.WriteLine($"User is changing the Height from {previousSize.Height} to {_currentSize.Height}.");
					NewMapSize = _currentSize;
					PerformLayout();
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
			get => (int)Math.Round(_beforeX);
			set
			{
				if (!_layoutInfo.IsEmpty && value != _beforeX)
				{
					var previous = _beforeX;

					_beforeX = value;
					OnPropertyChanged();

					if (PreserveWidth)
					{
						_afterX = _afterX + (value - previous);
						OnPropertyChanged(nameof(AfterX));
					}

					BeforeOffset = new PointDbl(BeforeX, BeforeY);
					AfterOffset = new PointDbl(AfterX, AfterY);
					_currentSize = HandleBeforeXUpdate(previous, value);
					NewMapSize = _currentSize;
					PerformLayout();
				}
			}
		}

		public int AfterX
		{
			get => (int)Math.Round(_afterX);
			set
			{
				if (!_layoutInfo.IsEmpty && value != _afterX)
				{
					var previous = _afterX;
					_afterX = value;
					OnPropertyChanged();

					if (PreserveWidth)
					{
						_beforeX = _beforeX + (value - previous);
						OnPropertyChanged(nameof(BeforeX));
					}
					
					BeforeOffset = new PointDbl(BeforeX, BeforeY);
					AfterOffset = new PointDbl(AfterX, AfterY);
					_currentSize = HandleAfterXUpdate(previous, value);
					NewMapSize = _currentSize;
					PerformLayout();
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
			get => (int)Math.Round(_beforeY);
			set
			{
				if (!_layoutInfo.IsEmpty && value != _beforeY)
				{
					var previous = _beforeY;
					_beforeY = value;
					OnPropertyChanged();

					if (PreserveHeight)
					{
						_afterY = _afterY + (value - previous);
						OnPropertyChanged(nameof(AfterY));
					}

					BeforeOffset = new PointDbl(BeforeX, BeforeY);
					AfterOffset = new PointDbl(AfterX, AfterY);
					_currentSize = HandleBeforeYUpdate(previous, value);
					NewMapSize = _currentSize;
					PerformLayout();
				}
			}
		}

		public int AfterY
		{
			get => (int)Math.Round(_afterY);
			set
			{
				if (!_layoutInfo.IsEmpty && value != _afterY)
				{
					var previous = _afterY;
					_afterY = value;
					OnPropertyChanged();

					if (PreserveHeight)
					{
						_beforeY = _beforeY + (value - previous);
						OnPropertyChanged(nameof(BeforeY));
					}

					BeforeOffset = new PointDbl(BeforeX, BeforeY);
					AfterOffset = new PointDbl(AfterX, AfterY);
					_currentSize = HandleAfterYUpdate(previous, value);
					NewMapSize = _currentSize;
					PerformLayout();
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

					BeforeOffset = new PointDbl(BeforeX, BeforeY);
					AfterOffset = new PointDbl(AfterX, AfterY);
					NewMapSize = _currentSize;
					PerformLayout();

					Debug.WriteLine($"The container size is now {value}.");
					OnPropertyChanged();
				}
			}
		}

		//private void InvertAndSetNewMapArea(RectangleDbl newMapAreaInverted)
		//{
		//	//var unInverted = new RectangleDbl(newMapAreaInverted.Position.Invert(), newMapAreaInverted.Size);
		//	//NewMapArea = unInverted;

		//	BeforeOffset = newMapAreaInverted.Position.Invert();
		//	NewMapSize = newMapAreaInverted.Size;
		//}

		private PointDbl BeforeOffset
		{
			get => _layoutInfo.BeforeOffset;
			set
			{
				if (!_layoutInfo.IsEmpty && value != BeforeOffset)
				{
					_layoutInfo.BeforeOffset = value;
				}
			}
		}

		private PointDbl AfterOffset
		{
			get => _layoutInfo.AfterOffset;
			set
			{
				if (!_layoutInfo.IsEmpty && value != AfterOffset)
				{
					_layoutInfo.AfterOffset = value;
				}
			}
		}

		public SizeDbl NewMapSize
		{
			get => _layoutInfo.NewMapSize;
			private set
			{
				if (!_layoutInfo.IsEmpty && value != NewMapSize)
				{
					_layoutInfo.NewMapSize = value;
					OnPropertyChanged(nameof(Width));
					OnPropertyChanged(nameof(Height));
				}
			}
		}

		public RectangleDbl NewMapArea => _layoutInfo.ResultNewMapArea;

		private void PerformLayout()
		{
			_layoutInfo.Update(_scaleFactorCurrentToOrginal);
			_scaleTransform.ScaleX = _layoutInfo.ScaleFactorForPreviewImage;
			_scaleTransform.ScaleY = _layoutInfo.ScaleFactorForPreviewImage;

			OnPropertyChanged(nameof(LayoutInfo));

			////OnPropertyChanged();

			//var previousAspect = _currentSize.AspectRatio;
			//var newSize = value.Size.Round();

			//if (newSize.Width != _currentSize.Width && newSize.Height != _currentSize.Height)
			//{
			//	_currentSize = newSize;
			//	OnPropertyChanged(nameof(Width));
			//	OnPropertyChanged(nameof(Height));
			//}
			//else if (newSize.Width != _currentSize.Width)
			//{
			//	_currentSize = newSize;
			//	OnPropertyChanged(nameof(Width));
			//}
			//else if (newSize.Height != _currentSize.Height)
			//{
			//	_currentSize = newSize;
			//	OnPropertyChanged(nameof(Height));
			//}

			//if (_currentSize.AspectRatio != previousAspect)
			//{
			//	OnPropertyChanged(nameof(AspectRatio));
			//}

		}

		public PreviewImageLayoutInfo LayoutInfo => _layoutInfo;

		public JobAreaInfo? PosterMapAreaInfo { get; private set; }

		public ImageSource PreviewImage => _previewImage;

		#endregion

		#region Public Properties - UI Display

		public double AspectRatio => _currentSize.AspectRatio;

		public int OriginalWidth
		{
			get => (int) Math.Round(_originalSize.Width);
			set
			{
				if (value != _originalSize.Width)
				{
					_originalSize = new SizeDbl(value, _originalSize.Height);
					OnPropertyChanged();
					OnPropertyChanged(nameof(OriginalAspectRatio));
				}
			}
		}

		public int OriginalHeight
		{
			get => (int)Math.Round(_originalSize.Height);
			set
			{
				if (value != _originalSize.Height)
				{
					_originalSize = new SizeDbl(_originalSize.Width, value);
					OnPropertyChanged();
					OnPropertyChanged(nameof(OriginalAspectRatio));
				}
			}
		}

		public double OriginalAspectRatio => _originalSize.AspectRatio;

		#endregion

		//#region Public Methods

		//public void RefreshPreview()
		//{
		//	LoadPreviewImage(_lazyMapPreviewImageProvider.Bitmap);
		//}

		//#endregion

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

			var newWidthSameHeight = newSize.Height * aspectRatio;
			var newHeightSameWidth = newSize.Width / aspectRatio;

			var deltaWidth = Math.Abs(newWidthSameHeight - newSize.Width);
			var deltaHeight = Math.Abs(newHeightSameWidth - newSize.Height);

			if (deltaWidth <= deltaHeight)
			{
				result = new SizeDbl(newWidthSameHeight, newSize.Height);
			}
			else
			{
				result = new SizeDbl(newSize.Width, newHeightSameWidth);
			}

			return result;
		}

		private SizeDbl SetOffsetsForNewSize(SizeDbl previousSize, SizeDbl size)
		{
			var newWidthSameHeight = previousSize.Width / previousSize.AspectRatio * size.AspectRatio;
			var newHeightSameWidth = previousSize.Height * previousSize.AspectRatio / size.AspectRatio;

			var deltaWidth = newWidthSameHeight - previousSize.Width;
			var deltaHeight = newHeightSameWidth - previousSize.Height;

			if (Math.Abs(deltaWidth) <= Math.Abs(deltaHeight))
			{
				var halfDeltaW = deltaWidth / 2;
				_beforeX -= halfDeltaW;
				_afterX += halfDeltaW;

				OnPropertyChanged(nameof(BeforeX));
				OnPropertyChanged(nameof(AfterX));
			}
			else
			{
				var halfDeltaH = deltaHeight / 2;
				_beforeY -= halfDeltaH;
				_afterY += halfDeltaH;

				OnPropertyChanged(nameof(BeforeY));
				OnPropertyChanged(nameof(AfterY));
			}

			var result = new SizeDbl(_afterX - _beforeX, _afterY - _beforeY);
			return result;

		}

		private SizeDbl HandleBeforeXUpdate(double previous, int val)
		{
			SizeDbl result;

			if (PreserveWidth)
			{
				result = _currentSize;
			}
			else
			{
				var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
				var delta = val - previous;
				var width = scaledSize.Width - delta;
				var height = PreserveAspectRatio ? width / AspectRatio : scaledSize.Height;
				result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);
			}

			return result;
		}

		private SizeDbl HandleAfterXUpdate(double previous, int val)
		{
			SizeDbl result;

			if (PreserveWidth)
			{
				result = _currentSize;
			}
			else
			{
				var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
				var delta = val - previous;
				var width = scaledSize.Width + delta;
				var height = PreserveAspectRatio ? width / AspectRatio : scaledSize.Height;
				result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);
			}

			return result;
		}

		private SizeDbl HandleBeforeYUpdate(double previous, int val)
		{
			SizeDbl result;

			if (PreserveHeight)
			{
				result = _currentSize;
			}
			else
			{
				var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
				var delta = val - previous;
				var height = scaledSize.Height - delta;
				var width = PreserveAspectRatio ? height * AspectRatio : scaledSize.Width;
				result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);
			}

			return result;
		}

		private SizeDbl HandleAfterYUpdate(double previous, int val)
		{
			SizeDbl result;

			if (PreserveHeight)
			{
				result = _currentSize;
			}
			else
			{
				var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
				var delta = val - previous;
				var height = scaledSize.Height + delta;
				var width = PreserveAspectRatio ? height * AspectRatio : scaledSize.Width;
				result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);
			}

			return result;
		}

		#endregion
	}
}

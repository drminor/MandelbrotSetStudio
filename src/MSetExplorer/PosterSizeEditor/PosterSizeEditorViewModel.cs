using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.UI.WebUI;

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
		private int _beforeX;
		private int _afterX;

		private bool _preserveHeight;
		private int _beforeY;
		private int _afterY;

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

		public void Initialize(MapAreaInfo2 posterMapAreaInfo, SizeDbl containerSize)
		{
			_preserveAspectRatio = true;
			_preserveWidth = true;
			_preserveHeight = true;

			OnPropertyChanged(nameof(PreserveAspectRatio));
			OnPropertyChanged(nameof(PreserveWidth));
			OnPropertyChanged(nameof(PreserveHeight));
			
			UpdateWithChangesInternal(posterMapAreaInfo, containerSize);

			BeforeOffset = new VectorDbl(BeforeX, BeforeY);
			AfterOffset = new VectorDbl(AfterX, AfterY);

			PerformLayout();
		}

		public void UpdateWithNewMapInfo(MapAreaInfo2 posterMapAreaInfo)
		{
			UpdateWithChangesInternal(posterMapAreaInfo, ContainerSize);

			_beforeX = 0; _afterX = 0; _beforeY = 0; _afterY = 0;
			OnPropertyChanged(nameof(BeforeX));
			OnPropertyChanged(nameof(AfterX));
			OnPropertyChanged(nameof(BeforeY));
			OnPropertyChanged(nameof(AfterY));

			BeforeOffset = new VectorDbl(BeforeX, BeforeY);
			AfterOffset = new VectorDbl(AfterX, AfterY);

			PerformLayout();
		}

		private void UpdateWithChangesInternal(MapAreaInfo2 posterMapAreaInfo, SizeDbl containerSize)
		{
			PosterMapAreaInfo = posterMapAreaInfo;
			_lazyMapPreviewImageProvider.MapAreaInfo = posterMapAreaInfo;

			var previewImage = _lazyMapPreviewImageProvider.Bitmap;

			//var posterSize = new SizeDbl(posterMapAreaInfo.CanvasSize);

			var posterSize = new SizeDbl(1024);

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
				if (value != _preserveAspectRatio)
				{
					_preserveAspectRatio = value;

					//if (value)
					//{
					//	_currentSizeScaled = RestoreAspectRatio(_currentSizeScaled, _originalSize.AspectRatio);

					//	_scaleFactorCurrentToOrginal = _currentSizeScaled.Width / _originalSize.Width;

					//	_currentSize = _currentSizeScaled.Scale(1 / _scaleFactorCurrentToOrginal);
					//	OnPropertyChanged(nameof(Width));
					//	OnPropertyChanged(nameof(Height));
					//	NewMapSize = _currentSize;

					//	_beforeX = 0; _afterX = 0; _beforeY = 0; _afterY = 0;
					//	OnPropertyChanged(nameof(BeforeX));
					//	OnPropertyChanged(nameof(AfterX));
					//	OnPropertyChanged(nameof(BeforeY));
					//	OnPropertyChanged(nameof(AfterY));

					//	BeforeOffset = new PointDbl(BeforeX, BeforeY);
					//	AfterOffset = new PointDbl(AfterX, AfterY);
					//	PerformLayout();
					//}

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
						_currentSize = new SizeDbl(value, value / _currentSize.AspectRatio);

						_scaleFactorCurrentToOrginal = _originalSize.Width / _currentSize.Width;
						_currentSizeScaled = _currentSize.Scale(_scaleFactorCurrentToOrginal);

						//OnPropertyChanged();

						//if (_currentSize.Height != previousSize.Height)
						//{
						//	OnPropertyChanged(nameof(Height));
						//}
					}
					else
					{
						_currentSize = new SizeDbl(value, _currentSize.Height);

						var newBeforeOffset = GetOffsetsForNewWidth(previousSize, _currentSize, BeforeOffset, AfterOffset, out var newAfterOffset);
						//var newAreaScaled = new RectangleDbl(new PointDbl().Translate(newBeforeOffset), new PointDbl(_originalSize.Width + newAfterOffset.X, _originalSize.Height + newAfterOffset.Y));
						var newAreaScaled = ScreenTypeHelper.GetNewBoundingArea(_originalSize, newBeforeOffset, newAfterOffset);
						_currentSizeScaled = newAreaScaled.Size;
						_currentSize = _currentSizeScaled.Scale(1 / _scaleFactorCurrentToOrginal);

						//OnPropertyChanged();

						//if (_currentSize.Height != previousSize.Height)
						//{
						//	OnPropertyChanged(nameof(Height));
						//}

						//OnPropertyChanged(nameof(AspectRatio));

						BeforeOffset = newBeforeOffset; // new PointDbl(BeforeX, BeforeY);
						AfterOffset = newAfterOffset; //new PointDbl(AfterX, AfterY);
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
						_currentSize = new SizeDbl(value * _currentSize.AspectRatio, value);
						_scaleFactorCurrentToOrginal = _originalSize.Width / _currentSize.Width;
						_currentSizeScaled = _currentSize.Scale(_scaleFactorCurrentToOrginal);

						//OnPropertyChanged();

						//if (_currentSize.Width != previousSize.Width)
						//{
						//	OnPropertyChanged(nameof(Width));
						//}
					}
					else
					{
						_currentSize = new SizeDbl(_currentSize.Width, value);

						var newBeforeOffset = GetOffsetsForNewHeight(previousSize, _currentSize, BeforeOffset, AfterOffset, out var newAfterOffset);
						//var newAreaScaled = new RectangleDbl(newBeforeOffset, new PointDbl(_originalSize.Width + newAfterOffset.X, _originalSize.Height + newAfterOffset.Y));
						var newAreaScaled = ScreenTypeHelper.GetNewBoundingArea(_originalSize, newBeforeOffset, newAfterOffset);
						_currentSizeScaled = newAreaScaled.Size;
						_currentSize = _currentSizeScaled.Scale(1 / _scaleFactorCurrentToOrginal);

						//OnPropertyChanged();

						//if (_currentSize.Width != previousSize.Width)
						//{
						//	OnPropertyChanged(nameof(Width));
						//}

						//OnPropertyChanged(nameof(AspectRatio));

						BeforeOffset = newBeforeOffset; // new PointDbl(BeforeX, BeforeY);
						AfterOffset = newAfterOffset; //new PointDbl(AfterX, AfterY);
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
			get => _beforeX;
			set						// => _beforeX = value;
			{
				if (value != _beforeX)
				{
					var previous = _beforeX;
					_beforeX = value;
					//OnPropertyChanged();

					BeforeOffset = new VectorDbl(BeforeX, BeforeY);

					if (PreserveWidth)
					{
						_afterX = _afterX + (value - previous);
						//OnPropertyChanged(nameof(AfterX));
						AfterOffset = new VectorDbl(AfterX, AfterY);
					}
					else
					{
						_currentSize = HandleBeforeXUpdate(previous, value);
						NewMapSize = _currentSize;
					}

					PerformLayout();
				}
			}
		}

		public int AfterX
		{
			get => _afterX;
			set					// => _afterX = value;
			{
				if (value != _afterX)
				{
					var previous = _afterX;
					_afterX = value;
					//OnPropertyChanged();
					AfterOffset = new VectorDbl(AfterX, AfterY);

					if (PreserveWidth)
					{
						_beforeX = _beforeX + (value - previous);
						//OnPropertyChanged(nameof(BeforeX));
						BeforeOffset = new VectorDbl(BeforeX, BeforeY);
					}
					else
					{
						_currentSize = HandleAfterXUpdate(previous, value);
						NewMapSize = _currentSize;
					}

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
					//OnPropertyChanged();
				}
			}
		}

		public int BeforeY
		{
			get => _beforeY;
			set						// => _beforeY = value;
			{
				if (value != _beforeY)
				{
					var previous = _beforeY;
					_beforeY = value;
					//OnPropertyChanged();
					BeforeOffset = new VectorDbl(BeforeX, BeforeY);

					if (PreserveHeight)
					{
						_afterY = _afterY + (value - previous);
						//OnPropertyChanged(nameof(AfterY));
						AfterOffset = new VectorDbl(AfterX, AfterY);
					}
					else
					{
						_currentSize = HandleBeforeYUpdate(previous, value);
						NewMapSize = _currentSize;
					}

					PerformLayout();
				}
			}
		}

		public int AfterY
		{
			get => _afterY;
			set						//=> _afterY = value;
			{
				if (value != _afterY)
				{
					var previous = _afterY;
					_afterY = value;
					//OnPropertyChanged();
					AfterOffset = new VectorDbl(AfterX, AfterY);

					if (PreserveHeight)
					{
						_beforeY = _beforeY + (value - previous);
						//OnPropertyChanged(nameof(BeforeY));
						BeforeOffset = new VectorDbl(BeforeX, BeforeY);
					}
					else
					{
						_currentSize = HandleAfterYUpdate(previous, value);
						NewMapSize = _currentSize;
					}

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

					BeforeOffset = new VectorDbl(BeforeX, BeforeY);
					AfterOffset = new VectorDbl(AfterX, AfterY);
					NewMapSize = _currentSize;
					PerformLayout();

					Debug.WriteLine($"The container size is now {value}.");
					OnPropertyChanged();
				}
			}
		}

		private VectorDbl BeforeOffset
		{
			get => _layoutInfo.BeforeOffset;
			set
			{
				if (!_layoutInfo.IsEmpty && value != BeforeOffset)
				{
					_layoutInfo.BeforeOffset = value;

					var roundedOffset = _layoutInfo.BeforeOffset.Round();

					if (_beforeX != roundedOffset.X)
					{
						_beforeX = roundedOffset.X;
					}

					if (_beforeY != roundedOffset.Y)
					{
						_beforeY = roundedOffset.Y;
					}

					OnPropertyChanged(nameof(BeforeY));
					OnPropertyChanged(nameof(BeforeX));

					//OnPropertyChanged(nameof(Width));
					//OnPropertyChanged(nameof(Height));
					//OnPropertyChanged(nameof(AspectRatio));
				}
			}
		}

		private VectorDbl AfterOffset
		{
			get => _layoutInfo.AfterOffset;
			set
			{
				if (!_layoutInfo.IsEmpty && value != AfterOffset)
				{
					_layoutInfo.AfterOffset = value;

					var roundedOffset = _layoutInfo.AfterOffset.Round();

					if (_afterX != roundedOffset.X)
					{
						_afterX = roundedOffset.X;
					}

					if (_afterY != roundedOffset.Y)
					{
						_afterY = roundedOffset.Y;
					}

					OnPropertyChanged(nameof(AfterX));
					OnPropertyChanged(nameof(AfterY));

					//OnPropertyChanged(nameof(Width));
					//OnPropertyChanged(nameof(Height));
					//OnPropertyChanged(nameof(AspectRatio));
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
					OnPropertyChanged(nameof(AspectRatio));
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

		public MapAreaInfo2? PosterMapAreaInfo { get; private set; }

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

		#region Public Methods

		//public void RefreshPreview()
		//{
		//	LoadPreviewImage(_lazyMapPreviewImageProvider.Bitmap);
		//}

		public void CancelPreviewImageGeneration()
		{
			_lazyMapPreviewImageProvider.CancelBitmapGeneration();
		}

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

		private VectorDbl GetOffsetsForNewWidth(SizeDbl previousSize, SizeDbl size, VectorDbl beforeOffsets, VectorDbl afterOffsets, out VectorDbl newAfterOffsets)
		{
			var ps = previousSize.Scale(_scaleFactorCurrentToOrginal);
			var cs = size.Scale(_scaleFactorCurrentToOrginal);

			var newWidthSameHeight = ps.Width / ps.AspectRatio * cs.AspectRatio;
			var deltaWidth = newWidthSameHeight - ps.Width;
			var halfDeltaW = deltaWidth / 2;

			var newBeforeOffsets = new VectorDbl(beforeOffsets.X -= halfDeltaW, beforeOffsets.Y);
			newAfterOffsets = new VectorDbl(afterOffsets.X += halfDeltaW, afterOffsets.Y);

			return newBeforeOffsets;
		}

		private VectorDbl GetOffsetsForNewHeight(SizeDbl previousSize, SizeDbl size, VectorDbl beforeOffsets, VectorDbl afterOffsets, out VectorDbl newAfterOffsets)
		{
			var ps = previousSize.Scale(_scaleFactorCurrentToOrginal);
			var cs = size.Scale(_scaleFactorCurrentToOrginal);

			var newHeightSameWidth = ps.Height * ps.AspectRatio / cs.AspectRatio;
			var deltaHeight = newHeightSameWidth - ps.Height;
			var halfDeltaH = deltaHeight / 2;

			var newBeforeOffsets = new VectorDbl(beforeOffsets.X, beforeOffsets.Y - halfDeltaH);
			newAfterOffsets = new VectorDbl(afterOffsets.X, afterOffsets.Y + halfDeltaH);

			return newBeforeOffsets;
		}

		private SizeDbl HandleBeforeXUpdate(double previous, int val)
		{

			var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
			var delta = val - previous;
			var width = scaledSize.Width + delta;
			var height = PreserveAspectRatio ? width / AspectRatio : scaledSize.Height;
			var result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);

			return result;
		}

		private SizeDbl HandleAfterXUpdate(double previous, int val)
		{
			var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
			var delta = val - previous;
			var width = scaledSize.Width + delta;
			var height = PreserveAspectRatio ? width / AspectRatio : scaledSize.Height;
			var	result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);

			return result;
		}

		private SizeDbl HandleBeforeYUpdate(double previous, int val)
		{
			var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
			var delta = val - previous;
			var height = scaledSize.Height + delta;
			var width = PreserveAspectRatio ? height * AspectRatio : scaledSize.Width;
			var result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);

			return result;
		}

		private SizeDbl HandleAfterYUpdate(double previous, int val)
		{
			var scaledSize = _currentSize.Scale(_scaleFactorCurrentToOrginal);
			var delta = val - previous;
			var height = scaledSize.Height + delta;
			var width = PreserveAspectRatio ? height * AspectRatio : scaledSize.Width;
			var result = new SizeDbl(width, height).Scale(1 / _scaleFactorCurrentToOrginal);

			return result;
		}

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

		/*

		private PointDbl GetOffsetsForNewWidth(SizeDbl previousSize, SizeDbl size, PointDbl beforeOffsets, PointDbl afterOffsets, out PointDbl newAfterOffsets)
		{

			var ps = previousSize.Scale(_scaleFactorCurrentToOrginal);
			var cs = _currentSize.Scale(_scaleFactorCurrentToOrginal);

			var newWidthSameHeight = previousSize.Width / previousSize.AspectRatio * size.AspectRatio;
			var newHeightSameWidth = previousSize.Height * previousSize.AspectRatio / size.AspectRatio;

			var deltaWidth = newWidthSameHeight - previousSize.Width;
			var deltaHeight = newHeightSameWidth - previousSize.Height;

			PointDbl newBeforeOffsets;

			if (Math.Abs(deltaWidth) <= Math.Abs(deltaHeight))
			{
				var halfDeltaW = deltaWidth / 2;

				newBeforeOffsets = new PointDbl(beforeOffsets.X -= halfDeltaW, beforeOffsets.Y);
				newAfterOffsets = new PointDbl(afterOffsets.X += halfDeltaW, afterOffsets.Y);
				//_beforeX -= halfDeltaW;
				//_afterX += halfDeltaW;

				//OnPropertyChanged(nameof(BeforeX));
				//OnPropertyChanged(nameof(AfterX));
			}
			else
			{
				var halfDeltaH = deltaHeight / 2;

				newBeforeOffsets = new PointDbl(beforeOffsets.X, beforeOffsets.Y - halfDeltaH);
				newAfterOffsets = new PointDbl(afterOffsets.X, afterOffsets.Y + halfDeltaH);

				//_beforeY -= halfDeltaH;
				//_afterY += halfDeltaH;

				//OnPropertyChanged(nameof(BeforeY));
				//OnPropertyChanged(nameof(AfterY));
			}

			//var result = new SizeDbl(_afterX - _beforeX, _afterY - _beforeY);
			return newBeforeOffsets;

		}

		*/

		#endregion
	}
}

using MSS.Common;
using MSS.Types;
using System.Diagnostics;
using System.Windows.Media;

namespace MSetExplorer
{
	public class PosterSizeEditorViewModel : ViewModelBase
	{

		private SizeDbl _containerSize;

		private RRectangle _coords;
		private int _width;
		private int _height;
		private double _aspectRatio;

		private int _originalWidth;
		private int _originalHeight;
		private double _originalAspectRatio;

		private RectangleDbl _newImageArea;
		private PreviewImageLayoutInfo _layoutInfo;


		public PosterSizeEditorViewModel(ImageSource previewImage, RRectangle coords, SizeInt canvasSize)
		{
			PreviewImage = previewImage;
			_coords = coords;

			_layoutInfo = new PreviewImageLayoutInfo(new SizeDbl(canvasSize), new SizeDbl(previewImage.Width, previewImage.Height), new SizeDbl(canvasSize));

			_width = 2;
			_height = 1;

			Width = canvasSize.Width;
			Height = canvasSize.Height;

			OriginalWidth = Width;
			OriginalHeight = Height;
		}

		public ImageSource PreviewImage { get; init; }

		public SizeDbl ContainerSize
		{
			get => _containerSize;
			set
			{
				_containerSize = value;
				_layoutInfo.ContainerSize = value;
				_layoutInfo.Update();
				OnPropertyChanged(nameof(LayoutInfo));

				Debug.WriteLine($"The container size is now {value}.");
				OnPropertyChanged();
			}
		}

		public RectangleDbl NewImageArea
		{
			get => _newImageArea;
			set
			{
				if (value != _newImageArea)
				{
					_newImageArea = value;
					_layoutInfo.NewImageArea = value;
					_layoutInfo.Update();
					OnPropertyChanged(nameof(LayoutInfo));

					OnPropertyChanged();
				}
			}
		}

		public PreviewImageLayoutInfo LayoutInfo => _layoutInfo;

		public RRectangle Coords
		{
			get => _coords;
			set
			{
				if (value != _coords)
				{
					_coords = value;
					OnPropertyChanged();
				}
			}
		}

		public int Width
		{
			get => _width;
			set
			{
				if (value != _width)
				{
					_width = value;
					OnPropertyChanged();
					AspectRatio = _width / (double)_height;
				}
			}
		}

		public int Height
		{
			get => _height;
			set
			{
				if (value != _height)
				{
					_height = value;
					OnPropertyChanged();
					AspectRatio = _width / (double)_height;
				}
			}
		}

		public double AspectRatio
		{
			get => _aspectRatio;
			set
			{
				if (value != _aspectRatio)
				{
					_aspectRatio = value;
					//var originalImageSize = new SizeDbl(OriginalWidth, OriginalHeight);
					//var newImageSize = new SizeDbl(Width, Height);
					//var newImageSizeSameAspect = PreviewImageLayoutInfo.GetNewImageSizePreserveAspect(newImageSize, originalImageSize);

					// Center the original Size within the new Size => ImageArea
					// Center the new Size within the container => NewPreviewImageArea, relative to the PreviewImageArea that it encloses => this is a ratio

					// Calculate the scale transform required to fit the NewPreviewImageArea within the container ==> New

					//var newImageArea = 

					OnPropertyChanged();
				}
			}

		}

		public int OriginalWidth
		{
			get => _originalWidth;
			set
			{
				if (value != _originalWidth)
				{
					_originalWidth = value;
					OnPropertyChanged();
					OriginalAspectRatio = _originalWidth / (double)_originalHeight;

				}
			}
		}

		public int OriginalHeight
		{
			get => _originalHeight;
			set
			{
				if (value != _originalHeight)
				{
					_originalHeight = value;
					OnPropertyChanged();
					OriginalAspectRatio = _originalWidth / (double)_originalHeight;
				}
			}
		}

		public double OriginalAspectRatio
		{
			get => _originalAspectRatio;
			set
			{
				if (value != _originalAspectRatio)
				{
					_originalAspectRatio = value;
					OnPropertyChanged();
				}
			}

		}



	}
}

using MSS.Types;
using System.Windows.Media;

namespace MSetExplorer
{
	public class PosterSizeEditorViewModel : ViewModelBase
	{
		private RRectangle _coords;
		private int _width;
		private int _height;

		public PosterSizeEditorViewModel(ImageSource previewImage, RRectangle coords, SizeInt canvasSize)
		{
			PreviewImage = previewImage;
			_coords = coords;
			_width = canvasSize.Width;
			_height = canvasSize.Height;
		}

		public ImageSource PreviewImage { get; init; }


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
				}
			}
		}




	}
}

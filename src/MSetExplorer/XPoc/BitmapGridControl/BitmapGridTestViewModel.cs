using MSS.Types;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSetExplorer.XPoc.BitmapGridControl
{
	internal class BitmapGridTestViewModel : ViewModelBase
	{
		private WriteableBitmap _bitmap;

		#region Constructor

		public BitmapGridTestViewModel()
		{
			_bitmap = CreateBitmap(new SizeInt(10));
		}

		#endregion

		#region Public Properties

		public WriteableBitmap Bitmap
		{
			get => _bitmap;
			set
			{
				_bitmap = value;
				OnPropertyChanged();
			}
		}


		#endregion

		#region Private Methods

		private WriteableBitmap CreateBitmap(SizeInt size)
		{
			var result = new WriteableBitmap(size.Width, size.Height, 96, 96, PixelFormats.Pbgra32, null);
			//var result = new WriteableBitmap(size.Width, size.Height, 0, 0, PixelFormats.Pbgra32, null);

			return result;
		}

		#endregion
	}

}

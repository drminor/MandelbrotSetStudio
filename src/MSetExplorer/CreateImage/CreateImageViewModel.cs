using System.Collections.Generic;
using System.IO;

namespace MSetExplorer
{
	public class CreateImageViewModel : ViewModelBase
	{
		private string _folderPath;
		private string? _imageFileName;
		private string _selectedImageType;

		public CreateImageViewModel(string folderPath, string? initialName)
		{
			ImageTypes = new[] { "PNG", "WMP" };
			_folderPath = folderPath;

			_selectedImageType = "PNG";

			_imageFileName = GetImageFilename(initialName, SelectedImageType);
		}

		#region Public Properties

		public string FolderPath
		{
			get => _folderPath;
			set
			{
				_folderPath = value;
				OnPropertyChanged();
			}
		}

		public string? ImageFileName
		{
			get => _imageFileName;
			set
			{
				_imageFileName = value;
				OnPropertyChanged();
			}
		}

		public IEnumerable<string> ImageTypes { get; init; }

		public string SelectedImageType
		{
			get => _selectedImageType;
			set
			{
				_selectedImageType = value;
				ImageFileName = GetImageFilename(ImageFileName, SelectedImageType);
				OnPropertyChanged();
			}
		}

		#endregion

		private string? GetImageFilename(string? filename, string imageFileType)
		{
			if (imageFileType == null)
			{
				return null;
			}
			else
			{
				var ext = imageFileType == "PNG" ? "png" : "wdp";
				var result = Path.ChangeExtension(filename, ext);
				return result;
			}
		}
	}
}

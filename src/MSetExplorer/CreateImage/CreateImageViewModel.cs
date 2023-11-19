
using System.Collections.Generic;

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
			_imageFileName = initialName;
			_selectedImageType = "PNG";

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
				OnPropertyChanged();
			}
		}

		#endregion

	}
}

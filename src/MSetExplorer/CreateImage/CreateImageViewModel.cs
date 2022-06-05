
namespace MSetExplorer
{
	public class CreateImageViewModel : ViewModelBase
	{
		private string? _imageFileName;
		private string _folderPath;

		public CreateImageViewModel(string folderPath, string? initialName)
		{
			_folderPath = folderPath;
			_imageFileName = initialName;
		}

		#region Public Properties

		public string? ImageFileName
		{
			get => _imageFileName;
			set
			{
				_imageFileName = value;
				OnPropertyChanged();
			}
		}

		public string FolderPath
		{
			get => _folderPath;
			set
			{
				_folderPath = value;
				OnPropertyChanged();
			}
		}

		#endregion

	}
}

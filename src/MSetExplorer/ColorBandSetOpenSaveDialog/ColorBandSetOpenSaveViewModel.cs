using MSetRepo;
using MSS.Types;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ColorBandSetOpenSaveViewModel : ViewModelBase, IColorBandSetOpenSaveViewModel
	{
		private readonly ProjectAdapter _projectAdapter;
		private ColorBandSetInfo _selectedColorBandSetInfo;

		private string _selectedName;
		private string _selectedDescription;
		private int _selectedVersionNumber;

		private bool _userIsSettingTheName;

		#region Constructor

		public ColorBandSetOpenSaveViewModel(ProjectAdapter projectAdapter, string initialName, DialogType dialogType)
		{
			_projectAdapter = projectAdapter;
			DialogType = dialogType;

			ColorBandSetInfos = new ObservableCollection<ColorBandSetInfo>(_projectAdapter.GetAllColorBandSetInfos());
			_selectedColorBandSetInfo = ColorBandSetInfos.FirstOrDefault(x => x.Name == initialName);

			var view = CollectionViewSource.GetDefaultView(ColorBandSetInfos);
			_ = view.MoveCurrentTo(SelectedColorBandSetInfo);
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public ObservableCollection<ColorBandSetInfo> ColorBandSetInfos { get; init; }

		public string SelectedName
		{
			get => _selectedName;
			set
			{
				_selectedName = value;
				OnPropertyChanged();
			}
		}

		public bool UserIsSettingTheName
		{
			get => _userIsSettingTheName;
			set { _userIsSettingTheName = value; OnPropertyChanged(); }
		}


		public string SelectedDescription
		{
			get => _selectedDescription;
			set
			{
				_selectedDescription = value;

				//if (SelectedColorBandSet != null && SelectedColorBandSet.Project.Id != ObjectId.Empty && SelectedColorBandSet.Description != value)
				//{
				//	_projectAdapter.UpdateProjectDescription(SelectedColorBandSet.Project.Id, SelectedDescription);
				//	SelectedColorBandSet.Description = value;
				//}

				OnPropertyChanged();
			}
		}

		public int SelectedVersionNumber
		{
			get => _selectedVersionNumber;
			set
			{
				_selectedVersionNumber = value;
				OnPropertyChanged();
			}
		}

		public ColorBandSetInfo SelectedColorBandSetInfo
		{
			get => _selectedColorBandSetInfo;

			set
			{
				_selectedColorBandSetInfo = value;
				OnPropertyChanged();
			}
		}

		#endregion

		public bool IsNameTaken(string name)
		{
			var result = _projectAdapter.TryGetProject(name, out var _);
			return result;
		}

	}
}

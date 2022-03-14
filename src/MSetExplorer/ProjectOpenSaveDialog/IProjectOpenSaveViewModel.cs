using MSetRepo;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public interface IProjectOpenSaveViewModel
	{
		DialogType DialogType { get; }

		ObservableCollection<IProjectInfo> ProjectInfos { get; }
		IProjectInfo SelectedProject { get; set; }

		string SelectedName { get; set; }
		string SelectedDescription { get; set; }

		bool UserIsSettingTheName { get; set; }

		bool IsNameTaken(string name);
	}

	public enum DialogType
	{
		Open,
		Save
	}
}
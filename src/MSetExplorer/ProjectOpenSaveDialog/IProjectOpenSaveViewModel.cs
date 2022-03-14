using MSetRepo;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IProjectOpenSaveViewModel
	{
		bool IsOpenDialog { get; }

		DialogType DialogType { get; }

		ObservableCollection<IProjectInfo> ProjectInfos { get; }
		IProjectInfo SelectedProject { get; set; }

		string SelectedName { get; set; }
		string SelectedDescription { get; set; }

		bool UserIsSettingTheName { get; set; }

	}

	public enum DialogType
	{
		Open,
		Save
	}
}
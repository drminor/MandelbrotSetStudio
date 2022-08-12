using MSS.Common;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public interface IProjectOpenSaveViewModel
	{
		DialogType DialogType { get; }

		ObservableCollection<IProjectInfo> ProjectInfos { get; }
		IProjectInfo? SelectedProject { get; set; }

		string? SelectedName { get; set; }
		string? SelectedDescription { get; set; }

		bool UserIsSettingTheName { get; set; }

		bool IsNameTaken(string? name);
		bool DeleteSelected(out long numberOfMapSectionsDeleted);
	}
}
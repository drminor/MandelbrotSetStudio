using MSetRepo;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IProjectOpenSaveViewModel
	{
		//public event PropertyChangedEventHandler PropertyChanged;

		bool IsOpenDialog { get; }

		ObservableCollection<IProjectInfo> ProjectInfos { get; }
		IProjectInfo SelectedProject { get; set; }

		string SelectedName { get; set; }
		string SelectedDescription { get; set; }

		bool UserIsSettingTheName { get; set; }
	}
}
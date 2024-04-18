using MSS.Common;
using System;
using System.ComponentModel;
using System.Windows.Data;

namespace MSetExplorer
{
	public interface IProjectOpenSaveViewModel : INotifyPropertyChanged
	{
		DialogType DialogType { get; }

		//ObservableCollection<IProjectInfo> ProjectInfos { get; }
		ListCollectionView ProjectInfosView { get; set; } 
		IProjectInfo? SelectedProject { get; set; }

		string? SelectedName { get; set; }
		string? SelectedDescription { get; set; }

		bool UserIsSettingTheName { get; set; }

		bool IsNameTaken(string? name);

		bool DeleteSelected(out long numberOfMapSectionsDeleted);
		long TrimSelected(bool agressive);

		ViewModelFactory ViewModelFactory { get; }

		string SortByFieldName { get; set; }
		bool SortDescending { get; set; }

		DateTime LastAccessedAfterDate { get; set; }
	}
}
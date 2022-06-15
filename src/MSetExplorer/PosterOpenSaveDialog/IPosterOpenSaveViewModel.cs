using MSS.Types;
using MSS.Types.MSet;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public interface IPosterOpenSaveViewModel
	{
		DialogType DialogType { get; }

		ObservableCollection<Poster> Posters { get; }
		Poster? SelectedPoster { get; set; }

		string? SelectedName { get; set; }
		string? SelectedDescription { get; set; }

		bool UserIsSettingTheName { get; set; }

		bool IsNameTaken(string? name);
		void DeleteSelected();
	}
}
using MSS.Types;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public interface IColorBandSetOpenSaveViewModel
	{
		DialogType DialogType { get; }

		ObservableCollection<ColorBandSetInfo> ColorBandSetInfos { get; }
		ColorBandSetInfo? SelectedColorBandSetInfo { get; set; }

		string? SelectedName { get; set; }
		string? SelectedDescription { get; set; }
		int? SelectedVersionNumber { get; set; }

		bool UserIsSettingTheName { get; set; }

		bool IsNameTaken(string? name);
	}


}
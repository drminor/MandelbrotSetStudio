using MSS.Common;
using MSS.Types;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public interface IPosterOpenSaveViewModel
	{
		DialogType DialogType { get; }

		ObservableCollection<IPosterInfo> PosterInfos { get; }
		IPosterInfo? SelectedPoster { get; set; }

		string? SelectedName { get; set; }
		string? SelectedDescription { get; set; }

		bool UserIsSettingTheName { get; set; }

		bool IsNameTaken(string? name);
		bool DeleteSelected(out long numberOfMapSectionsDeleted);

		byte[]? GetPreviewImageData(SizeInt imageSize);
	}
}
using MSS.Types;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IColorBandSetOpenSaveViewModel : INotifyPropertyChanged
	{
		DialogType DialogType { get; }
		string ResolutionStrategy { get; set; }

		int TargetIterations { get; }

		ObservableCollection<ColorBandSetInfo> ColorBandSetInfos { get; }
		ColorBandSetInfo? SelectedColorBandSetInfo { get; set; }

		string? SelectedName { get; set; }
		string? SelectedDescription { get; set; }
		bool UserIsSettingTheName { get; set; }

		//bool SaveColorBandSet(ColorBandSet colorBandSet);

		//bool TryOpenColorBandSet(ObjectId colorBandSetId, [MaybeNullWhen(false)] out ColorBandSet colorBandSet);

		bool IsNameTaken(string name);
	}

}
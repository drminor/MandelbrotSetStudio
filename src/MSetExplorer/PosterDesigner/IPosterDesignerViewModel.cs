using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IPosterDesignerViewModel : INotifyPropertyChanged
	{
		IPosterViewModel PosterViewModel { get; }

		IMapScrollViewModel MapScrollViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }

		IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);

		CreateImageProgressViewModel CreateACreateImageProgressViewModel(string imageFilePath);

		IProjectAdapter ProjectAdapter { get; init; }
	}
}

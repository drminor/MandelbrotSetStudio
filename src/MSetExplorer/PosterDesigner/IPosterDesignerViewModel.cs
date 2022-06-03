using MSS.Types;
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

		IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);

		void PrintPoster(Poster poster);

		IProjectAdapter ProjectAdapter { get; init; }
	}
}

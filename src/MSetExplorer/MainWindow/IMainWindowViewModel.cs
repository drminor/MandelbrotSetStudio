using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler? PropertyChanged;

		IMapProjectViewModel MapProjectViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }
		MSetInfoViewModel MSetInfoViewModel { get; }

		IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateAColorBandSetOpenSaveViewModel(string? initalName, DialogType dialogType);

		//void BumpDispWidth(bool increase);
		//void BumpDispHeight(bool increase);
	}
}
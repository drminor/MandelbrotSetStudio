using MSS.Types.MSet;
using System;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IExplorerViewModel : INotifyPropertyChanged, IDisposable
	{
		IMapProjectViewModel MapProjectViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }

		IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);

		IProjectAdapter ProjectAdapter { get; }
	}
}
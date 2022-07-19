using MSS.Types;
using System;
using System.ComponentModel;

namespace MSetExplorer
{
	internal interface IExplorerViewModel : INotifyPropertyChanged, IDisposable
	{
		IMapProjectViewModel MapProjectViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }
		ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }
		
		IJobTreeViewModel JobTreeViewModel { get; }

		IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);
		IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType);
		CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeInt canvasSize, bool allowEdits);

		JobProgressViewModel CreateAJobProgressViewModel();

	}
}
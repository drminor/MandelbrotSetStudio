using MSS.Types;
using System;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IExplorerViewModel : INotifyPropertyChanged, IDisposable
	{
		IProjectViewModel ProjectViewModel { get; }
		IJobTreeViewModel JobTreeViewModel { get; }

		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }
		ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }

		IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);
		IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType);
		CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeInt canvasSize, bool allowEdits);

		JobProgressViewModel CreateAJobProgressViewModel();

	}
}
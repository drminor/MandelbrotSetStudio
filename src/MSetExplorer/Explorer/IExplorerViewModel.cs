using ImageBuilder;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IExplorerViewModel : INotifyPropertyChanged, IDisposable
	{
		IProjectViewModel ProjectViewModel { get; }
		IJobTreeViewModel JobTreeViewModel { get; }

		IMapDisplayViewModel2 MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }
		ColorBandSetHistogramViewModel ColorBandSetHistogramViewModel { get; }

		//IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType);
		//IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);
		//IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType);
		//CoordsEditorViewModel CreateACoordsEditorViewModel(MapAreaInfo2 mapAreaInfo2, SizeInt canvasSize, bool allowEdits);

		ViewModelFactory ViewModelFactory { get; }

		CreateImageProgressViewModel CreateACreateImageProgressViewModel(/*string imageFilePath, bool useEscapeVelocities*/);
		JobProgressViewModel CreateAJobProgressViewModel();
	}
}
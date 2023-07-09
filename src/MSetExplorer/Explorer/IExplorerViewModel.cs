using MSS.Types.MSet;
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

		ICbsHistogramViewModel CbshDisplayViewModel { get; }

		ViewModelFactory ViewModelFactory { get; }

		bool MapCoordsIsVisible { get; set; }

		//CreateImageProgressViewModel CreateACreateImageProgressViewModel(/*string imageFilePath, bool useEscapeVelocities*/);
		//JobProgressViewModel CreateAJobProgressViewModel();

		//MapAreaInfo GetMapAreaWithSizeFat(MapAreaInfo2 mapAreaInfo2, SizeDbl imageSize);

	}
}
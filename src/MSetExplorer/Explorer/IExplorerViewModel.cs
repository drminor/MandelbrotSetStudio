using System;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IExplorerViewModel : INotifyPropertyChanged, IDisposable
	{
		IProjectViewModel ProjectViewModel { get; }
		IJobTreeViewModel JobTreeViewModel { get; }

		IMapDisplayViewModel MapDisplayViewModel { get; }
		JobProgressViewModel JobProgressViewModel { get; }
		
		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		//ColorBandSetViewModel ColorBandSetViewModel { get; }

		ICbsHistogramViewModel CbsHistogramViewModel { get; }

		ViewModelFactory ViewModelFactory { get; }

		bool MapCoordsIsVisible { get; set; }

		//CreateImageProgressViewModel CreateACreateImageProgressViewModel(/*string imageFilePath, bool useEscapeVelocities*/);
		//JobProgressViewModel CreateAJobProgressViewModel();

		//MapAreaInfo GetMapAreaWithSizeFat(MapCenterAndDelta mapCenterAndDelta, SizeDbl imageSize);

	}
}
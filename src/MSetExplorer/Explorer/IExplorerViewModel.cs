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

		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }

		ICbshDisplayViewModel CbshDisplayViewModel { get; }

		ViewModelFactory ViewModelFactory { get; }

		CreateImageProgressViewModel CreateACreateImageProgressViewModel(/*string imageFilePath, bool useEscapeVelocities*/);
		JobProgressViewModel CreateAJobProgressViewModel();
	}
}
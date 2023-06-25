using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IPosterDesignerViewModel : INotifyPropertyChanged
	{
		IPosterViewModel PosterViewModel { get; }
		IJobTreeViewModel JobTreeViewModel { get; }

		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }

		ICbshDisplayViewModel CbshDisplayViewModel { get; }

		ViewModelFactory ViewModelFactory { get; }

		CreateImageProgressViewModel CreateACreateImageProgressViewModel(/*string imageFilePath, bool useEscapeVelocities*/);
		LazyMapPreviewImageProvider GetPreviewImageProvider(MapAreaInfo2 mapAreaInfo, SizeDbl previewImagesize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, Color fallbackColor);

		MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea, out double diagReciprocal);

		void RunCurrentJob();
	}
}

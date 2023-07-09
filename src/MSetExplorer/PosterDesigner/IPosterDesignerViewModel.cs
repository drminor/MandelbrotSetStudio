using System.ComponentModel;

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

		ICbsHistogramViewModel CbshDisplayViewModel { get; }

		ViewModelFactory ViewModelFactory { get; }

		//CreateImageProgressViewModel CreateACreateImageProgressViewModel(/*string imageFilePath, bool useEscapeVelocities*/);
		//LazyMapPreviewImageProvider GetPreviewImageProvider(ObjectId jobId, MapAreaInfo2 mapAreaInfo, SizeDbl previewImagesize, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, Color fallbackColor);

		//MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea, out double diagReciprocal);

		//void SubmitMapDisplayJob();

		//MapAreaInfo GetMapAreaWithSizeFat(MapAreaInfo2 mapAreaInfo2, SizeDbl imageSize);
	}
}

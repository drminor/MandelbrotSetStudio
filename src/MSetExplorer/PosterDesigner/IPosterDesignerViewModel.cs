using MSetExplorer.MapDisplay.ScrollAndZoom;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IPosterDesignerViewModel : INotifyPropertyChanged
	{
		//SizeDbl MapDisplaySize { get; }
		IPosterViewModel PosterViewModel { get; }

		//IMapScrollViewModel MapScrollViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }

		IJobTreeViewModel JobTreeViewModel { get; }

		//IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType);
		//IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);
		//CoordsEditorViewModel CreateACoordsEditorViewModel(MapAreaInfo2 mapAreaInfo, SizeInt canvasSize, bool allowEdits);

		ViewModelFactory ViewModelFactory { get; }

		CreateImageProgressViewModel CreateACreateImageProgressViewModel(/*string imageFilePath, bool useEscapeVelocities*/);

		LazyMapPreviewImageProvider GetPreviewImageProvider(MapAreaInfo2 mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, SizeInt previewImagesize, Color fallbackColor);

		//MapAreaInfo2? GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize);

		MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeInt currentPosterSize, RectangleDbl screenArea);
	}
}

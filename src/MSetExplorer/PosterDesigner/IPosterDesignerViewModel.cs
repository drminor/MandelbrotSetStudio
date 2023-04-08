using MSetExplorer.MapDisplay.ScrollAndZoom;
using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IPosterDesignerViewModel : INotifyPropertyChanged
	{
		SizeDbl MapDisplaySize { get; }
		IPosterViewModel PosterViewModel { get; }

		IMapScrollViewModel MapScrollViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }

		IJobTreeViewModel JobTreeViewModel { get; }

		IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, bool useEscapeVelocities, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);
		CreateImageProgressViewModel CreateACreateImageProgressViewModel(string imageFilePath, bool useEscapeVelocities);
		CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeInt canvasSize, bool allowEdits);

		LazyMapPreviewImageProvider GetPreviewImageProvider(MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, SizeInt previewImagesize, Color fallbackColor);

		MapAreaInfo GetUpdatedMapAreaInfo(MapAreaInfo mapAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize);
	}
}

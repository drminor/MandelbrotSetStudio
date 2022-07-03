using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IPosterDesignerViewModel : INotifyPropertyChanged
	{
		IPosterViewModel PosterViewModel { get; }

		IMapScrollViewModel MapScrollViewModel { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		MapCoordsViewModel MapCoordsViewModel { get; }
		MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		ColorBandSetViewModel ColorBandSetViewModel { get; }

		IPosterOpenSaveViewModel CreateAPosterOpenSaveViewModel(string? initalName, DialogType dialogType);
		IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType);
		CreateImageProgressViewModel CreateACreateImageProgressViewModel(string imageFilePath);
		CoordsEditorViewModel CreateACoordsEditorViewModel(RRectangle coords, SizeInt canvasSize, bool allowEdits);

		//ImageSource GetPreviewImage(JobAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, SizeInt previewImagesize, CancellationToken ct, bool useGenericImage = true);
		LazyMapPreviewImageProvider GetPreviewImageProvider(MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, SizeInt previewImagesize, Color fallbackColor);

		MapAreaInfo GetUpdatedJobAreaInfo(MapAreaInfo jobAreaInfo, RectangleDbl screenArea, SizeDbl newMapSize);

	}
}

using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;
using System.Threading;
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

		//PosterSizeEditorViewModel CreateAPosterSizeEditorViewModel(Poster poster, SizeInt previewImageSize, SizeDbl? displaySize);

		ImageSource GetPreviewImage(Poster poster, SizeInt previewImagesize, CancellationToken ct, bool useGenericImage = true);

		JobAreaInfo GetUpdatedJobAreaInfo(JobAreaInfo jobAreaInfo, RectangleDbl screenArea);

		IProjectAdapter ProjectAdapter { get; init; }
	}
}

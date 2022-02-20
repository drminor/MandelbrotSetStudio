using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	internal interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler PropertyChanged;

		SizeInt CanvasSize { get; set; }

		Project Project { get; }

		void SaveProject();
		void LoadProject();

		void SetMapInfo(MSetInfo mSetInfo);
		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ScreenPannedEventArgs e);

		IMapLoaderJobStack MapLoaderJobStack { get;}

		IMapDisplayViewModel MapDisplayViewModel { get; }
	}
}
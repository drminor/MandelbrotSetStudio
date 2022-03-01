using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler PropertyChanged;

		IMapLoaderJobStack MapLoaderJobStack { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		Project Project { get; }
		//SizeInt CanvasSize { get; set; }

		int Iterations { get; set; }
		int Steps { get; set; }

		void SaveProject();
		void LoadProject();

		void SetMapInfo(MSetInfo mSetInfo);
		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ScreenPannedEventArgs e);

		void Test();

	}
}
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler PropertyChanged;

		// This will be removed.
		Project CurrentProject { get; set; }

		SizeInt BlockSize { get; }
		ImageSource ImageSource { get; }
		ObservableCollection<MapSection> MapSections { get; }

		// These may need to be dependency properties
		SizeDbl ContainerSize { get; set; }

		SizeInt CanvasSize { get; set; }
		VectorInt CanvasControlOffset { get; set; }

		//void SetCanvasSize(SizeInt canvasSize);

		// These will become ICommands
		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

	}
}
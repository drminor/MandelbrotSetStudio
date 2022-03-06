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

		Project CurrentProject { get; set; }

		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; }
		ImageSource ImageSource { get; }
		ObservableCollection<MapSection> MapSections { get; }

		// These may need to be dependency properties
		VectorInt CanvasControlOffset { get; set; }
		void SetCanvasSize(SizeInt canvasSize);

		// These will become ICommands
		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

	}
}
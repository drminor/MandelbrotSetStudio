using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;

		SizeInt BlockSize { get; }
		ImageSource ImageSource { get; }
		ObservableCollection<MapSection> MapSections { get; }

		JobAreaAndCalcSettings? CurrentJobAreaAndCalcSettings { get; set; }

		ColorBandSet ColorBandSet { get; }

		void SetColorBandSet(ColorBandSet value, bool updateDisplay);

		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

		// These may need to be dependency properties
		SizeDbl ContainerSize { get; set; }
		SizeInt CanvasSize { get; set; }
		VectorInt CanvasControlOffset { get; set; }

		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

		void TearDown();
	}
}
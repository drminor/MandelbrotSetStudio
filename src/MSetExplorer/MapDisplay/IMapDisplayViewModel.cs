using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		event EventHandler<int>? DisplayJobCompleted;

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
		SizeInt CanvasSize { get; }
		VectorInt CanvasControlOffset { get; set; }

		SizeInt LogicalDisplaySize { get; }
		double DisplayZoom { get; set; }
		
		// Just for diagnostics
		//RectangleDbl ClipRegion { get; }

		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

		// New Methods to replace the Update... methods above.
		void SubmitJob(JobAreaAndCalcSettings job);
		void CancelJob();
		void RestartLastJob();
		void ClearDisplay();

		VectorInt ScreenCollectionIndex { get; }
	}
}
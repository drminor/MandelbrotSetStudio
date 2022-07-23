using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		ImageSource ImageSource { get; }

		bool HandleContainerSizeUpdates { get; set; }
		SizeDbl ContainerSize { get; set; }
		
		SizeDbl CanvasSize { get; }
		VectorInt CanvasControlOffset { get; set; }

		SizeDbl LogicalDisplaySize { get; }
		double DisplayZoom { get; set; }

		bool InDesignMode { get; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		event EventHandler<int>? DisplayJobCompleted;

		SizeInt BlockSize { get; }

		ObservableCollection<MapSection> MapSections { get; }

		JobAreaAndCalcSettings? CurrentJobAreaAndCalcSettings { get; set; }

		ColorBandSet ColorBandSet { get; }

		void SetColorBandSet(ColorBandSet value, bool updateDisplay);

		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

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
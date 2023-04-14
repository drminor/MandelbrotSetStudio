using MSS.Types;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		SizeInt BlockSize { get; }

		//Image Image { get; }
		WriteableBitmap Bitmap { get; }

		//ObservableCollection<MapSection> MapSections { get; }
		MapSectionCollection MapSections { get; }

		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; }

		ColorBandSet ColorBandSet { get; set; }

		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

		//bool HandleContainerSizeUpdates { get; set; }
		//SizeDbl ContainerSize { get; set; }

		SizeDbl CanvasSize { get; set; }
		SizeInt CanvasSizeInBlocks { get; set; }
		SizeDbl LogicalDisplaySize { get; }

		VectorDbl ImageOffset { get; set; }

		double DisplayZoom { get; set; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		event EventHandler<int>? DisplayJobCompleted;

		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

		// New Methods to replace the Update... methods above.
		int? SubmitJob(AreaColorAndCalcSettings newValue);
		void CancelJob();
		void RestartLastJob();
		void ClearDisplay();

	}
}
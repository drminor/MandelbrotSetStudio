using MSS.Types;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		SizeInt BlockSize { get; }

		//Image Image { get; }
		//WriteableBitmap Bitmap { get; }

		IBitmapGrid? BitmapGrid { get; set; }

		Action<MapSection> DisposeMapSection { get; }

		ObservableCollection<MapSection> MapSections { get; }

		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; }

		ColorBandSet ColorBandSet { get; set; }

		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

		//bool HandleContainerSizeUpdates { get; set; }
		//SizeDbl ContainerSize { get; set; }

		SizeDbl CanvasSize { get; set; }
		//SizeInt CanvasSizeInBlocks { get; set; }
		SizeDbl LogicalDisplaySize { get; }

		//BigVector MapBlockOffset { get; set; }
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
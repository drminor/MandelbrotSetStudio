using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		event EventHandler<MapViewUpdateCompletedEventArgs>? MapViewUpdateCompleted;

		event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;

		ObservableCollection<MapSection> MapSections { get; }
		
		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; }
		MapPositionSizeAndDelta? LastMapAreaInfo { get; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; }
		int CurrentColorBandIndex { get; set; }


		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

		ImageSource ImageSource { get; }
		VectorInt ImageOffset { get; }

		void RaiseMapViewZoomUpdate(AreaSelectedEventArgs e);
		void RaiseMapViewPanUpdate(ImageDraggedEventArgs e);

		MsrJob? SubmitJob(AreaColorAndCalcSettings newValue);
		void SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom);

		void CancelJob();
		void PauseJob();
		MsrJob? RestartJob();
		void ClearDisplay();

		SizeDbl UnscaledExtent { get; }
		SizeDbl ViewportSize { get; set; }
		SizeDbl LogicalViewportSize { get; }
		SizeDbl? ContentViewportSize { get; }

		VectorDbl DisplayPosition { get; }

		double DisplayZoom { get; set; }
		double MinimumDisplayZoom { get; set; }
		double MaximumDisplayZoom { get; set; }

		MsrJob? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset);
		MsrJob? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		MsrJob? MoveTo(VectorDbl contentOffset);

	}
}
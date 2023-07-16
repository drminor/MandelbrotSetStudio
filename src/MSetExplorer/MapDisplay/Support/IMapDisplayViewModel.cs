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
		event EventHandler<int>? DisplayJobCompleted;
		
		event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;

		SizeInt BlockSize { get; }
		ObservableCollection<MapSection> MapSections { get; }
		
		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; }
		MapAreaInfo? LastMapAreaInfo { get; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; set; }
		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }

		ImageSource ImageSource { get; }
		VectorDbl ImageOffset { get; }

		void RaiseMapViewZoomUpdate(AreaSelectedEventArgs e);
		void RaiseMapViewPanUpdate(ImageDraggedEventArgs e);

		int? SubmitJob(AreaColorAndCalcSettings newValue);
		void SubmitJob(AreaColorAndCalcSettings newValue, SizeDbl posterSize, VectorDbl displayPosition, double displayZoom);

		void CancelJob();
		void PauseJob();
		int? RestartJob();
		void ClearDisplay();

		SizeDbl UnscaledExtent { get; }
		SizeDbl ViewportSize { get; set; }

		//ValueTuple<VectorDbl, double>? ScaledDisplayPositionYInverted { get; set; }
		//VectorDbl GetCurrentDisplayPosition();
		//double DisplayPositionX { get; set; }
		//double DisplayPositionY { get; set; }

		VectorDbl DisplayPosition { get; set; }

		double DisplayZoom { get; set; }
		double MinimumDisplayZoom { get; set; }
		double MaximumDisplayZoom { get; set; }

		int? UpdateViewportSize(SizeDbl viewportSize);
		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		int? MoveTo(VectorDbl contentOffset, SizeDbl contentViewportSize);

		//void ReceiveAdjustedContentScale(double contentScaleFromPanAndZoomControl, double contentScaleFromBitmapGridControl);

	}
}
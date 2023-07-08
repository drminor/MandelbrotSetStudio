using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
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
		
		//event EventHandler? JobSubmitted;
		event EventHandler<InitialDisplaySettingsEventArgs>? InitializeDisplaySettings;

		SizeInt BlockSize { get; }
		ObservableCollection<MapSection> MapSections { get; }
		//ObservableCollection<MapSection> MapSectionsPendingGeneration { get; }
		
		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; }

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
		SizeDbl ViewportSize { get; }
		VectorDbl DisplayPosition { get; }

		double DisplayZoom { get; }
		double MinimumDisplayZoom { get; }

		MapAreaInfo? LastMapAreaInfo { get; }
		Func<IContentScaleInfo, ZoomSlider>? ZoomSliderFactory { get; set; }

		int? UpdateViewportSize(SizeDbl viewportSize);
		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		int? MoveTo(VectorDbl contentOffset);

		void ReceiveAdjustedContentScale(double contentScaleFromPanAndZoomControl, double contentScaleFromBitmapGridControl);

	}
}
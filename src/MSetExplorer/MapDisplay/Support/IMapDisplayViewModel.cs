using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		event EventHandler<int>? DisplayJobCompleted;

		SizeInt BlockSize { get; }
		ObservableCollection<MapSection> MapSections { get; }
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
		int? SubmitJob(AreaColorAndCalcSettings newValue, SizeInt posterSize);

		void CancelJob();
		int? RestartLastJob();
		void ClearDisplay();

		SizeDbl UnscaledExtent { get; }
		SizeDbl ViewportSize { get; }

		VectorDbl DisplayPosition { get; }

		double DisplayZoom { get; }
		double MinimumDisplayZoom { get; }

		MapAreaInfo? LastMapAreaInfo { get; }
		Func<IContentScaleInfo, ZoomSlider>? ZoomSliderFactory { get; set; }

		int? UpdateViewportSize(SizeDbl viewportSize);
		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale, double baseScale);

		int? MoveTo(VectorDbl displayPosition);

	}
}
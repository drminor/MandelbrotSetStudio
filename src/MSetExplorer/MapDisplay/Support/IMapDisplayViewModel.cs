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

		ImageSource ImageSource { get; set; }
		VectorDbl ImageOffset { get; set; }

		void RaiseMapViewZoomUpdate(AreaSelectedEventArgs e);
		void RaiseMapViewPanUpdate(ImageDraggedEventArgs e);

		int? SubmitJob(AreaColorAndCalcSettings newValue);
		int? SubmitJob(AreaColorAndCalcSettings newValue, SizeInt posterSize);

		void CancelJob();
		int? RestartLastJob();
		void ClearDisplay();


		SizeDbl UnscaledExtent { get; set; }
		SizeDbl ViewportSize { get; set; }

		double HorizontalPosition { get; set; }

		double VerticalPosition { get; set; }
		double InvertedVerticalPosition { get; }

		double DisplayZoom { get; set; }
		double MinimumDisplayZoom { get; }

		MapAreaInfo? LastMapAreaInfo { get; }
		Func<IContentScaleInfo, ZoomSlider>? ZoomSliderFactory { get; set; }

		void UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl positionRelativeToPosterMapBlockOffset);

	}
}
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel2 : INotifyPropertyChanged, IDisposable
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

		Size? PosterSize { get; set; }
		SizeDbl ViewPortSize { get; set; }

		VectorDbl ImageOffset { get; set; }

		MapAreaInfo? LastMapAreaInfo { get; }

		double HorizontalPosition { get; set; }

		double VerticalPosition { get; set; }
		double InvertedVerticalPosition { get; set; }

		double DisplayZoom { get; set; }
		double MaximumDisplayZoom { get; }

		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

		int? SubmitJob(AreaColorAndCalcSettings newValue);
		int? SubmitJob(AreaColorAndCalcSettings newValue, SizeInt posterSize);

		void CancelJob();
		int? RestartLastJob();
		void ClearDisplay();
	}
}
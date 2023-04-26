using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel : INotifyPropertyChanged, IDisposable
	{
		bool InDesignMode { get; }

		SizeInt BlockSize { get; }

		ObservableCollection<MapSection> MapSections { get; }

		AreaColorAndCalcSettings? CurrentAreaColorAndCalcSettings { get; }

		IBitmapGrid? BitmapGrid { get; set; }
		Action<MapSection> DisposeMapSection { get; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; set; }
		bool UseEscapeVelocities { get; set; }
		bool HighlightSelectedColorBand { get; set; }


		SizeDbl ViewPortSize { get; set; }

		VectorDbl ImageOffset { get; set; }


		event EventHandler<MapViewUpdateRequestedEventArgs>? MapViewUpdateRequested;
		event EventHandler<int>? DisplayJobCompleted;

		void UpdateMapViewZoom(AreaSelectedEventArgs e);
		void UpdateMapViewPan(ImageDraggedEventArgs e);

		int? SubmitJob(AreaColorAndCalcSettings newValue);
		int? SubmitJob(AreaColorAndCalcSettings newValue, SizeInt posterSize);

		void CancelJob();
		int? RestartLastJob();
		void ClearDisplay();

		//MapAreaInfo? GetMapAreaInfo();

		MapAreaInfo? LastMapAreaInfo { get; }


		SizeInt? PosterSize { get; set; }

		//SizeDbl LogicalDisplaySize { get; }
		double DisplayZoom { get; set; }
		//double MaximumDisplayZoom { get; }

		double HorizontalPosition { get; set; }

		double VerticalPosition { get; set; }
		double InvertedVerticalPosition { get; set; }
	}
}
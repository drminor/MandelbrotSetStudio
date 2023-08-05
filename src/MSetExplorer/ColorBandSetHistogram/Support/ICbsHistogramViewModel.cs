using MSetExplorer.ColorBandSetHistogram.Support;
using MSS.Types;
using System;
using System.Windows.Data;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface ICbsHistogramViewModel
	{
		bool InDesignMode { get; }

		event EventHandler<DisplaySettingsInitializedEventArgs>? DisplaySettingsInitialized;

		ListCollectionView ColorBandsView { get; set; }
		ColorBand? CurrentColorBand { get; set; }
		ColorBandSet ColorBandSet { get; set; }

		int EndPtr { get; set; }
		int StartPtr { get; set; }

		HPlotSeriesData SeriesData { get; }
		ImageSource ImageSource { get; }
		VectorDbl ImageOffset { get; }

		SizeDbl ContainerSize { get; set; }
		SizeInt CanvasSize { get; set; }		

		SizeDbl UnscaledExtent { get; }         // PosterSize
		SizeDbl ViewportSize { get; set; }		// ContainerSize
		VectorDbl DisplayPosition { get; }

		double DisplayZoom { get; set; }
		double MinimumDisplayZoom { get; set; }
		double MaximumDisplayZoom { get; set; }

		void RefreshHistogramDisplay();

		//KeyValuePair<int, int>[] GetKeyValuePairsForBand(int previousCutoff, int cutoff, bool includeCatchAll);
		//IEnumerable<KeyValuePair<int, int>> GetKeyValuePairsForBand(int previousCutoff, int cutoff);
		//int? UpdateViewportSize(SizeDbl viewportSize);

		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset);
		int? UpdateViewportSizePosAndScale(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		int? MoveTo(VectorDbl displayPosition);

	}
}
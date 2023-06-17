using MSS.Types;
using System.Windows.Data;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface ICbshDisplayViewModel
	{
		bool InDesignMode { get; }

		ListCollectionView ColorBandsView { get; set; }
		ColorBand? CurrentColorBand { get; set; }
		ColorBandSet ColorBandSet { get; set; }

		int EndPtr { get; set; }
		int StartPtr { get; set; }

		ImageSource ImageSource { get; }
		VectorDbl ImageOffset { get; }

		SizeDbl ContainerSize { get; set; }
		SizeInt CanvasSize { get; set; }		

		SizeDbl UnscaledExtent { get; }         // PosterSize
		SizeDbl ViewportSize { get; }			// ContainerSize
		VectorDbl DisplayPosition { get; }

		double DisplayZoom { get; }
		double MinimumDisplayZoom { get; }

		void RefreshHistogramDisplay();

		int? UpdateViewportSize(SizeDbl viewportSize);
		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale);

		int? MoveTo(VectorDbl displayPosition);

	}
}
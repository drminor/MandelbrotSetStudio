using MSS.Types;
using System.Windows.Data;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface ICbshDisplayViewModel
	{
		ImageSource ImageSource { get; init; }
		SizeDbl ContainerSize { get; set; }
		SizeInt CanvasSize { get; set; }

		SizeDbl UnscaledExtent { get; set; }
		double DisplayZoom { get; set; }

		bool InDesignMode { get; }
		ListCollectionView ColorBandsView { get; set; }
		ColorBand? CurrentColorBand { get; set; }
		ColorBandSet ColorBandSet { get; set; }

		int EndPtr { get; set; }
		int StartPtr { get; set; }

		void RefreshHistogramDisplay();

		int? UpdateViewportSize(SizeDbl viewportSize);
		int? UpdateViewportSizeAndPos(SizeDbl contentViewportSize, VectorDbl contentOffset, double baseScale);

		int? MoveTo(VectorDbl displayPosition);

	}
}
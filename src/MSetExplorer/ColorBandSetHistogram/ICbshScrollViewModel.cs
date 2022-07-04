using MSS.Types;

namespace MSetExplorer
{
	public interface ICbshScrollViewModel
	{
		CbshDisplayViewModel CbshDisplayViewModel { get; init; }

		SizeInt CanvasSize { get; set; }
		SizeInt? HistogramSize { get; set; }

		double DisplayZoom { get; set; }
		double MaximumDisplayZoom { get; }

		double HorizontalPosition { get; set; }
		double VerticalPosition { get; set; }
		double InvertedVerticalPosition { get; set; }
	}
}
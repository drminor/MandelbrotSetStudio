using MSS.Types;
using System.ComponentModel;

namespace MSetExplorer
{
	internal interface IMapScrollViewModel : INotifyPropertyChanged
	{
		IMapDisplayViewModel MapDisplayViewModel { get; init; }

		SizeDbl CanvasSize { get; set; }
		SizeInt? PosterSize { get; set; }

		double DisplayZoom { get; set; }
		double MaximumDisplayZoom { get; }

		double HorizontalPosition { get; set; }
		double VerticalPosition { get; set; }
		double InvertedVerticalPosition { get; set; }
	}
}
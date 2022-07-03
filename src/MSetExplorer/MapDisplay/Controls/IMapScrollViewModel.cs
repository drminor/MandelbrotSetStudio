using MSS.Types;
using System.ComponentModel;

namespace MSetExplorer
{
	internal interface IMapScrollViewModel : INotifyPropertyChanged
	{
		IMapDisplayViewModel MapDisplayViewModel { get; init; }

		SizeInt CanvasSize { get; set; }
		SizeInt? PosterSize { get; set; }

		double DisplayZoom { get; set; }
		double MaximumDisplayZoom { get; }

		double InvertedVerticalPosition { get; set; }
		double VerticalPosition { get; set; }
		double HorizontalPosition { get; set; }
	}
}
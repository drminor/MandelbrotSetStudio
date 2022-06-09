using MSS.Types;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapScrollViewModel : INotifyPropertyChanged
	{
		IMapDisplayViewModel MapDisplayViewModel { get; init; }

		SizeInt? PosterSize { get; set; }

		double InvertedVerticalPosition { get; }
		double VerticalPosition { get; set; }
		double HorizontalPosition { get; set; }

		//double VMax { get; set; }
		//double HMax { get; set; }
	}
}
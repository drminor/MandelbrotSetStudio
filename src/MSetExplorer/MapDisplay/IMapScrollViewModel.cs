using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapScrollViewModel : INotifyPropertyChanged
	{
		IMapDisplayViewModel MapDisplayViewModel { get; init; }

		JobAreaAndCalcSettings? CurrentJobAreaAndCalcSettings { get; set; }

		double VerticalPosition { get; set; }

		double HorizontalPosition { get; set; }

		double GetVMax();
		double GetHMax();

		double GetVerticalViewPortSize();
		double GetHorizontalViewPortSize();
	}
}
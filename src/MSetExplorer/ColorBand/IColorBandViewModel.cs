using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IColorBandViewModel
	{
		bool InDesignMode { get; }

		event PropertyChangedEventHandler PropertyChanged;

		double RowHeight { get; set; }
		double ItemWidth { get; set; }

		Project CurrentProject { get; set; }

		ObservableCollection<ColorBandW> ColorBands { get; }
		ColorBandW SelectedColorBand { get; set; }
		int? HighCutOff { get; set; }
		ColorBandSetW ColorBandSet { get; }

		void Test1();
		void Test2();
		void Test3();
		void Test4();
	}
}
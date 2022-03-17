using MSS.Types;
using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IColorBandViewModel
	{
		bool InDesignMode { get; }
		event PropertyChangedEventHandler PropertyChanged;

		Project CurrentProject { get; set; }
		ColorBandSet ColorBandSet { get; }
		int? HighCutOff { get; set; }

		ObservableCollection<ColorBand> ColorBands { get; }
		ColorBand SelectedColorBand { get; set; }

		void Test1();
		void Test2();
		void Test3();
		void Test4();
	}
}
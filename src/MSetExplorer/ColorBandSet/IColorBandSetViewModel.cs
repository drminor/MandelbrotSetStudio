using MSS.Types;
using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IColorBandSetViewModel
	{
		//bool InDesignMode { get; }

		event PropertyChangedEventHandler PropertyChanged;

		double RowHeight { get; set; }
		double ItemWidth { get; set; }

		Project CurrentProject { get; set; }

		//ObservableCollection<ColorBand> ColorBands { get; }
		ColorBand SelectedColorBand { get; set; }
		int? HighCutoff { get; set; }
		ColorBandSet ColorBandSet { get; }

		void DeleteSelectedItem();
		void InsertItem();

		void ApplyChanges();

		//void Test1();
		//void Test2();
		//void Test3();
		//void Test4();
	}
}
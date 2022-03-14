using MSS.Types;
using MSS.Types.MSet;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public interface IColorBandViewModel
	{
		Job CurrentJob { get; set; }
		ObservableCollection<ColorBand> ColorBands { get; }
		ColorBand SelectedColorBand { get; set; }
	}
}
using MSS.Types;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler PropertyChanged;

		IMapProject JobStack { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		int TargetIterations { get; set; }
		int Steps { get; set; }

		ColorMapEntry[] ColorMapEntries { get; set; }

	}
}
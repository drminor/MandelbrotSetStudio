using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler PropertyChanged;

		IJobStack JobStack { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		int Iterations { get; set; }
		int Steps { get; set; }

	}
}
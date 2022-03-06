using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler PropertyChanged;

		IJobStack JobStack { get; }
		IMapDisplayViewModel MapDisplayViewModel { get; }

		Project CurrentProject { get; }

		int Iterations { get; set; }
		int Steps { get; set; }

		void SetMapInfo(MSetInfo mSetInfo);

		void SaveProject();
		void LoadProject();
	}
}
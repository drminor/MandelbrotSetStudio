using MSS.Types.MSet;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMainWindowViewModel
	{
		event PropertyChangedEventHandler PropertyChanged;

		IMapDisplayViewModel MapDisplayViewModel { get; }

		Project CurrentProject { get; }

		int Iterations { get; set; }
		int Steps { get; set; }

		void SaveProject();
		void LoadProject();

		public bool CanGoBack { get; }
		public bool CanGoForward { get; }

		public void GoBack();
		public void GoForward();

		void SetMapInfo(MSetInfo mSetInfo);

		void Test();
	}
}
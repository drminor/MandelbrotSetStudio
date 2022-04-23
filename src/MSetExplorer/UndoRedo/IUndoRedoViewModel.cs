using System.ComponentModel;

namespace MSetExplorer
{
	public interface IUndoRedoViewModel : INotifyPropertyChanged
	{
		int CurrentIndex { get; set; }

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		bool GoBack();
		bool GoForward();

	}
}
using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IJobTreeViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		Project? CurrentProject { get; set; }

		public ObservableCollection<JobTreeItem> JobItems { get; }
		Job CurrentJob { get; }
	}
}
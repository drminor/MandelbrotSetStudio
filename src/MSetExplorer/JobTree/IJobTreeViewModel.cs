using MongoDB.Bson;
using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IJobTreeViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		//event EventHandler<NavigateToJobRequestedEventArgs>? NavigateToJobRequested;

		Project? CurrentProject { get; set; }

		ObservableCollection<JobTreeItem>? JobItems { get; }

		//Job? CurrentJob { get; }

		void RaiseNavigateToJobRequested(ObjectId jobId);

		//void AddJob(Job job);
		//void ShowOriginalVersion();
		//void RollupPans();
		//void RollupSingles();

	}
}
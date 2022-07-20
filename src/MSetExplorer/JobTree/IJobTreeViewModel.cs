using MongoDB.Bson;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IJobTreeViewModel : INotifyPropertyChanged
	{
		bool InDesignMode { get; }

		public event EventHandler<NavigateToJobRequestedEventArgs>? NavigateToJobRequested;

		Project? CurrentProject { get; set; }

		public ObservableCollection<JobTreeItem> JobItems { get; }

		Job? CurrentJob { get; }

		void RaiseNavigateToJobRequested(ObjectId jobId);

		public void ShowOriginalVersion();
		public void RollupPans();
		public void RollupSingles();

	}
}
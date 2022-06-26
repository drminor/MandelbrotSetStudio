using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public class JobProgressViewModel : ViewModelBase
	{
		#region Constructor

		private JobProgressRecord? _selectedJobProgressRecord;

		public JobProgressViewModel()
		{
			JobProgressRecords = new ObservableCollection<JobProgressRecord>();
		}

		#endregion

		#region Public Properties

		public ObservableCollection<JobProgressRecord> JobProgressRecords { get; }

		public JobProgressRecord? SelectedJobProgressRecord
		{
			get => _selectedJobProgressRecord; 
			set
			{
				if (value !=_selectedJobProgressRecord)
				{
					_selectedJobProgressRecord = value;
					OnPropertyChanged();
				}
			}
		}

		public string SelectedJobProgressLabel => SelectedJobProgressRecord?.Label ?? "None";

		#endregion

		#region Public Methods

		public void AddJobProgressRecord(JobProgressRecord jobProgressRecord)
		{
			JobProgressRecords.Add(jobProgressRecord);
		}

		#endregion
	}
}

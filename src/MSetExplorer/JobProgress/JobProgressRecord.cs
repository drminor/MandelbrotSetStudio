using System;

namespace MSetExplorer
{
	public class JobProgressRecord : ViewModelBase
	{
		private int _percentComplete;
		private int _fetchedCount;
		private int _generatedCount;

		public JobProgressRecord(string label, DateTime dateCreated, int totalSections)
		{
			Label = label;
			DateCreated = dateCreated;
			TotalSections = totalSections;
		}

		public string Label { get; init; }

		public DateTime DateCreated { get; init; }

		public int TotalSections { get; init; }

		public int PercentComplete
		{
			get => _percentComplete;
			set
			{
				if (value != _percentComplete)
				{
					_percentComplete = value;
					OnPropertyChanged();
				}
			}
		}

		public int FetchedCount
		{
			get => _fetchedCount;
			set
			{
				if (value != _fetchedCount)
				{
					_fetchedCount = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(PercentComplete));
				}
			}
		}

		public int GeneratedCount
		{
			get => _generatedCount;
			set
			{
				if (value != _generatedCount)
				{
					_generatedCount = value;
					OnPropertyChanged();
					OnPropertyChanged(nameof(PercentComplete));
				}
			}
		}


	}
}

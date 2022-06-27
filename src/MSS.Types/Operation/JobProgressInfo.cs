using System;
using System.Diagnostics;

namespace MSS.Types
{
	public class JobProgressInfo 
	{
		private double _percentComplete;
		private int _fetchedCount;
		private int _generatedCount;

		public JobProgressInfo(int jobNumber, string label, DateTime dateCreated, int totalSections)
		{
			JobNumber = jobNumber;
			Label = label;
			DateCreated = dateCreated;
			TotalSections = totalSections;
		}

		public int JobNumber { get; init; }

		public string Label { get; init; }

		public DateTime DateCreated { get; init; }

		public int TotalSections { get; init; }

		public double PercentComplete
		{
			get => _percentComplete;
			set
			{
				if (value != _percentComplete)
				{
					_percentComplete = value;
					//OnPropertyChanged();
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
					//OnPropertyChanged();
					if (TotalSections > 0)
					{
						PercentComplete = 100 * (_generatedCount + _fetchedCount) / TotalSections;
						Debug.WriteLine($"G: {_generatedCount}, F: {_fetchedCount}, PC: {_percentComplete}, TS: {TotalSections}.");
						//OnPropertyChanged(nameof(PercentComplete));
					}
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
					//OnPropertyChanged();

					if (TotalSections > 0)
					{
						PercentComplete = 100 * (_generatedCount + _fetchedCount) / TotalSections;
						//OnPropertyChanged(nameof(PercentComplete));
					}
				}
			}
		}

		public double PercentageFetched
		{
			get
			{
				if (_generatedCount == 0)
				{
					return 100;
				}
				else
				{
					var result = 100 * _fetchedCount / (double)_generatedCount;
					return result;
				}
			}
		}


	}
}

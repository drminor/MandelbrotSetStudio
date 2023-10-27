using System;

namespace MSS.Types
{
	public class JobProgressInfo 
	{
		private int _cancelledCount;
		private int _fetchedCount;
		private int _generatedCount;

		public JobProgressInfo(int jobNumber, string label, DateTime dateCreatedUtc, int totalSections, int numberOfSectionsFetched)
		{
			JobNumber = jobNumber;
			Label = label;
			DateCreatedUtc = dateCreatedUtc;

			TotalSections = totalSections;
			_cancelledCount = 0;
			_fetchedCount = numberOfSectionsFetched;
			
			_generatedCount = -1;
			GeneratedCount = 0;
		}

		public int JobNumber { get; init; }

		public string Label { get; init; }

		public DateTime DateCreatedUtc { get; set; }

		private bool _isComplete;
		public bool IsComplete
		{
			get => _isComplete;
			set => _isComplete = value;
		}

		public int TotalSections { get; init; }
		public int SectionsReceived => _cancelledCount + _fetchedCount + _generatedCount;

		public TimeSpan RunTime => DateTime.UtcNow - DateCreatedUtc;

		public TimeSpan EstimatedTimeRemaining
		{
			get
			{
				TimeSpan result;
				if (RunTime.TotalSeconds > 5 && PercentComplete > 1)
				{
					result = TimeSpan.FromSeconds((RunTime.TotalSeconds * 100 / PercentComplete) - RunTime.TotalSeconds);
				}
				else
				{
					result = TimeSpan.Zero;
				}

				return result;
			}
		}

		public double PercentComplete { get; set; }

		public int CancelledCount
		{
			get => _cancelledCount;
			set
			{
				if (value != _cancelledCount)
				{
					_cancelledCount = value;
					if (TotalSections > 0)
					{
						PercentComplete = 100 * SectionsReceived / (double)TotalSections;
						//Debug.WriteLine($"G: {GeneratedCount}, F: {FetchedCount}, PC: {PercentComplete}, TS: {TotalSections}.");
					}
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
					if (TotalSections > 0)
					{
						PercentComplete = 100 * SectionsReceived / (double)TotalSections;
						//Debug.WriteLine($"G: {GeneratedCount}, F: {FetchedCount}, PC: {PercentComplete}, TS: {TotalSections}.");
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

					if (TotalSections > 0)
					{
						PercentComplete = 100 * SectionsReceived / (double)TotalSections;
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

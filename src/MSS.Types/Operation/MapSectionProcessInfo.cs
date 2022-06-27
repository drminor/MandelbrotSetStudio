using System;

namespace MSS.Types
{
	public class MapSectionProcessInfo
	{
		public MapSectionProcessInfo(int jobNumber, int requestsCompleted, TimeSpan requestDuration, bool foundInRepo)
		{
			JobNumber = jobNumber;
			RequestsCompleted = requestsCompleted;
			RequestDuration = requestDuration;
			FoundInRepo = foundInRepo;
		}

		public int JobNumber { get; init; }
		public int RequestsCompleted { get; init; }
		public TimeSpan RequestDuration { get; init; }
		public bool FoundInRepo { get; init; }

		public string JobAndReqNum => $"{JobNumber}-{RequestsCompleted}";

		public double RequestDurationSeconds => RequestDuration.TotalSeconds;

		public string Generated => FoundInRepo ? "N" : "Y";
	}
}

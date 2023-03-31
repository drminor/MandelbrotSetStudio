using System;

namespace MSS.Types
{
	public class MapSectionProcessInfo
	{
		public MapSectionProcessInfo(int jobNumber, bool foundInRepo, int numberOfRequestsCompleted, bool isLastSection, 
			TimeSpan? requestDuration, 	TimeSpan? processingDuration, TimeSpan? generationDuration)
		{
			JobNumber = jobNumber;
			FoundInRepo = foundInRepo;
			NumberOfRequestsCompleted = numberOfRequestsCompleted;
			IsLastSection = isLastSection;

			RequestDuration = requestDuration;
			ProcessingDuration = processingDuration;
			GenerationDuration = generationDuration;
		}

		public int JobNumber { get; init; }
		public bool FoundInRepo { get; init; }
		public int NumberOfRequestsCompleted { get; init; }
		public bool IsLastSection { get; init; }

		public TimeSpan? RequestDuration { get; init; }
		public TimeSpan? ProcessingDuration { get; init; }
		public TimeSpan? GenerationDuration { get; init; }

		public string JobAndReqNum => $"{JobNumber}-{NumberOfRequestsCompleted}";

		public double RequestDurationSeconds => RequestDuration?.TotalSeconds ?? double.NaN;
		public double ProcessingDurationSeconds => ProcessingDuration?.TotalSeconds ?? double.NaN;

		public string Generated => FoundInRepo ? "N" : "Y";
	}
}

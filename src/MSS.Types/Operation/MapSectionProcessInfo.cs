using System;

namespace MSS.Types
{
	public class MapSectionProcessInfo
	{
		public MapSectionProcessInfo(int jobNumber, int requestNumber, bool foundInRepo, bool requestWasCompleted, bool requestWasCancelled, 
			TimeSpan? requestDuration, 	TimeSpan? processingDuration, TimeSpan? generationDuration)
		{
			JobNumber = jobNumber;
			FoundInRepo = foundInRepo;
			//NumberOfRequestsCompleted = numberOfRequestsCompleted;
			//IsLastSection = isLastSection;
			RequestWasCompleted = requestWasCompleted;
			RequestWasCancelled = requestWasCancelled;

			RequestDuration = requestDuration;
			ProcessingDuration = processingDuration;
			GenerationDuration = generationDuration;
		}

		public int JobNumber { get; init; }
		public int RequestNumber { get; init; }

		public bool FoundInRepo { get; init; }
		public bool RequestWasCompleted { get; set; }
		public bool RequestWasCancelled { get; set; }

		//public int NumberOfRequestsCompleted { get; init; }
		//public bool IsLastSection { get; init; }

		public TimeSpan? RequestDuration { get; init; }
		public TimeSpan? ProcessingDuration { get; init; }
		public TimeSpan? GenerationDuration { get; init; }

		public string JobAndReqNum => $"{JobNumber}/{RequestNumber}";

		public double RequestDurationSeconds => RequestDuration?.TotalSeconds ?? double.NaN;
		public double ProcessingDurationSeconds => ProcessingDuration?.TotalSeconds ?? double.NaN;

		public string Generated => FoundInRepo ? "N" : "Y";
	}
}

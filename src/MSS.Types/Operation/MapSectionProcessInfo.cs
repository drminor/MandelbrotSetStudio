using System;

namespace MSS.Types
{
	public class MapSectionProcessInfo
	{
		public MapSectionProcessInfo(int jobNumber, int requestNumber, bool foundInRepo, bool requestWasCompleted, bool requestWasCancelled, 
			TimeSpan? requestDuration, 	TimeSpan? processingDuration, TimeSpan? generationDuration, MathOpCounts? mathOpCounts)
		{
			JobNumber = jobNumber;
			RequestNumber = requestNumber;
			RequestId = jobNumber + "/" + requestNumber;

			FoundInRepo = foundInRepo;
			RequestWasCompleted = requestWasCompleted;
			RequestWasCancelled = requestWasCancelled;

			RequestDuration = requestDuration;
			ProcessingDuration = processingDuration;
			GenerationDuration = generationDuration;
			MathOpCounts = mathOpCounts;
		}

		public int JobNumber { get; init; }
		public int RequestNumber { get; init; }
		public string RequestId { get; init; }

		public bool FoundInRepo { get; init; }
		public bool RequestWasCompleted { get; set; }
		public bool RequestWasCancelled { get; set; }

		public TimeSpan? RequestDuration { get; init; }
		public TimeSpan? ProcessingDuration { get; init; }
		public TimeSpan? GenerationDuration { get; init; }

		public MathOpCounts? MathOpCounts { get; init; }

		public double RequestDurationSeconds => RequestDuration?.TotalSeconds ?? double.NaN;
		public double ProcessingDurationSeconds => ProcessingDuration?.TotalSeconds ?? double.NaN;

		public string Generated => FoundInRepo ? "N" : "Y";
	}
}

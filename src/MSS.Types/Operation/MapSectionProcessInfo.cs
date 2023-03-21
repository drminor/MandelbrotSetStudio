using System;

namespace MSS.Types
{
	public class MapSectionProcessInfo
	{
		public MapSectionProcessInfo(int jobNumber, int requestsCompleted, TimeSpan? requestDuration, TimeSpan? processingDuration, TimeSpan? generationDuration, bool foundInRepo,
			bool isLastSection, MathOpCounts? mathOpCounts = null)
		{
			JobNumber = jobNumber;
			RequestsCompleted = requestsCompleted;
			RequestDuration = requestDuration;
			ProcessingDuration = processingDuration;
			GenerationDuration = generationDuration;
			FoundInRepo = foundInRepo;
			IsLastSection = isLastSection;
			MathOpCounts = mathOpCounts;
		}

		public int JobNumber { get; init; }
		public int RequestsCompleted { get; init; }
		public TimeSpan? RequestDuration { get; init; }
		public TimeSpan? ProcessingDuration { get; init; }
		public TimeSpan? GenerationDuration { get; init; }
		public bool FoundInRepo { get; init; }
		public MathOpCounts? MathOpCounts { get; init; }
		public bool IsLastSection { get; init; }

		public string JobAndReqNum => $"{JobNumber}-{RequestsCompleted}";

		public double RequestDurationSeconds => RequestDuration?.TotalSeconds ?? double.NaN;
		public double ProcessingDurationSeconds => ProcessingDuration?.TotalSeconds ?? double.NaN;

		public string Generated => FoundInRepo ? "N" : "Y";
	}
}

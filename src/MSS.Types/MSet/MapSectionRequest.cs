using System;

namespace MSS.Types.MSet
{
	public class MapSectionRequest
	{
		public MapSectionRequest(string ownerId, JobOwnerType jobOwnerType, string subdivisionId, 
			PointInt screenPosition, BigVector mapBlockOffset, BigVector blockPosition, bool isInverted, RPoint mapPosition, int precision, 
			SizeInt blockSize, RSize samplePointDelta, MapCalcSettings mapCalcSettings)
		{
			MapSectionId = null;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			ScreenPosition = screenPosition;
			MapBlockOffset = mapBlockOffset;
			BlockPosition = blockPosition;
			IsInverted = isInverted;
			MapPosition = mapPosition;
			Precision = precision;
			BlockSize = blockSize;
			SamplePointDelta = samplePointDelta;
			MapCalcSettings = mapCalcSettings;
			ProcessingStartTime = DateTime.UtcNow;
		}

		public string? MapSectionId { get; set; }
		public string OwnerId { get; init; }
		public JobOwnerType JobOwnerType { get; init; }
		public string SubdivisionId { get; init; }
		public PointInt ScreenPosition { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public BigVector BlockPosition { get; init; }
		public bool IsInverted { get; init; }
		public RPoint MapPosition { get; init; }
		public SizeInt BlockSize { get; init; }
		public RSize SamplePointDelta { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }

		public int Precision { get; set; }
		public MapSectionVectors? MapSectionVectors { get; set; }
		public MapSectionZVectors? MapSectionZVectors { get; set; }

		public string? ClientEndPointAddress { get; set; }
		public bool IncreasingIterations { get; set; }

		public bool Pending { get; set; }
		public bool Sent { get; set; }
		public bool FoundInRepo { get; set; }
		public bool Completed { get; set; }
		public bool Saved { get; set; }
		public bool Handled { get; set; }

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan? TimeToCompleteGenRequest { get; set; }
		public TimeSpan? ProcessingDuration => ProcessingEndTime.HasValue ? ProcessingEndTime - ProcessingStartTime : null;
		public TimeSpan? GenerationDuration { get; set; }

		public MathOpCounts? MathOpCounts { get; set; }

		public override string ToString()
		{
			return $"Id: {MapSectionId}, S:{SubdivisionId}, ScrPos:{ScreenPosition}.";
		}
	}
}

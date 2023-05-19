using System;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MapSectionRequest
	{
		public MapSectionRequest(string ownerId, JobOwnerType jobOwnerType, string subdivisionId,
			PointInt screenPosition, BigVector mapBlockOffset, BigVector blockPosition, RPoint mapPosition, bool isInverted,
			int precision, int limbCount, SizeInt blockSize, RSize samplePointDelta, MapCalcSettings mapCalcSettings, int requestNumber)
		{
			MapSectionId = null;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			ScreenPosition = screenPosition;
			MapBlockOffset = mapBlockOffset;
			BlockPosition = blockPosition;
			MapPosition = mapPosition;
			IsInverted = isInverted;
			Precision = precision;
			LimbCount = limbCount;
			BlockSize = blockSize;
			SamplePointDelta = samplePointDelta;
			MapCalcSettings = mapCalcSettings;
			RequestNumber = requestNumber;
			CancellationTokenSource = new CancellationTokenSource();

			ProcessingStartTime = DateTime.UtcNow;
		}

		public string? MapSectionId { get; set; }
		public string OwnerId { get; init; }
		public JobOwnerType JobOwnerType { get; init; }
		public string SubdivisionId { get; init; }

		/// <summary>
		/// X,Y coords on screen in Block-Size units
		/// </summary>
		public PointInt ScreenPosition { get; init; }
		
		/// <summary>
		/// X,Y coords for the Job, relative to Subdivision Base in Block-Size units
		/// </summary>
		public BigVector MapBlockOffset { get; init; }


		// TODO: Confirm that the ScreenPosition and BlockPostion values are identical, always

		/// <summary>
		/// X,Y coords for this MapSection, relative to the MapBlockOffset in Block-Size units.
		/// </summary>
		public BigVector BlockPosition { get; init; }
		
		/// <summary>
		/// X,Y coords for this MapSection in absolute map coordinates. Equal to the BlockPosition x BlockSize x SamplePointDelta 
		/// </summary>
		public RPoint MapPosition { get; init; }

		/// <summary>
		/// True, if this MapSection has a negative Y coordinate. 
		/// </summary>
		public bool IsInverted { get; init; }

		public SizeInt BlockSize { get; init; }
		
		public RSize SamplePointDelta { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public int RequestNumber { get; init; }

		public CancellationTokenSource CancellationTokenSource { get; init; }

		public int Precision { get; set; }
		public int LimbCount { get; set; }
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

		public override string ToString()
		{
			return $"Id: {MapSectionId}, S:{SubdivisionId}, ScrPos:{ScreenPosition}.";
		}

		public (MapSectionVectors? mapSectionVectors, MapSectionZVectors? mapSectionZVectors) TransferMapVectorsOut()
		{
			var msv = MapSectionVectors;
			var mszv = MapSectionZVectors;

			MapSectionVectors = null;
			MapSectionZVectors = null;

			return (msv, mszv);

		}
	}
}

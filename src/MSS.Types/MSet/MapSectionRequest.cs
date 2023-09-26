using MongoDB.Bson;
using System;
using System.Diagnostics;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MapSectionRequest
	{
		public MapSectionRequest(JobType jobType, string jobId, OwnerType ownerType, string subdivisionId, string originalSourceSubdivisionId,
			PointInt screenPosition, VectorInt screenPositionRelativeToCenter, BigVector jobBlockOffset, MapBlockOffset sectionBlockOffset, RPoint mapPosition, bool isInverted,
			int precision, int limbCount, SizeInt blockSize, RSize samplePointDelta, MapCalcSettings mapCalcSettings, int mapLoaderJobNumber, int requestNumber)
		{
			ObjectId test = new ObjectId(originalSourceSubdivisionId);

			if (test == ObjectId.Empty)
			{
				Debug.WriteLine($"The originalSourceSubdivisionId is blank during MapSectionRequest construction.");
			}

			JobType = jobType;
			MapSectionId = null;
			JobId = jobId;
			OwnerType = ownerType;
			SubdivisionId = subdivisionId;
			OriginalSourceSubdivisionId = originalSourceSubdivisionId;
			ScreenPosition = screenPosition;
			ScreenPositionReleativeToCenter = screenPositionRelativeToCenter;
			JobBlockOffset = jobBlockOffset;
			SectionBlockOffset = sectionBlockOffset;
			MapPosition = mapPosition;
			IsInverted = isInverted;
			Precision = precision;
			LimbCount = limbCount;
			BlockSize = blockSize;
			SamplePointDelta = samplePointDelta;
			MapCalcSettings = mapCalcSettings;
			MapLoaderJobNumber = mapLoaderJobNumber;
			RequestNumber = requestNumber;
			CancellationTokenSource = new CancellationTokenSource();

			ProcessingStartTime = DateTime.UtcNow;
		}

		public JobType JobType { get; init; }
		public string? MapSectionId { get; set; }
		public string JobId { get; init; }
		public OwnerType OwnerType { get; init; }
		public string SubdivisionId { get; init; }
		public string OriginalSourceSubdivisionId { get; init; }

		/// <summary>
		/// X,Y coords for this MapSection, relative to the Subdivision BaseMapPosition in Block-Size units.
		/// </summary>
		//public BigVector RepoBlockPosition { get; init; }
		public MapBlockOffset SectionBlockOffset { get; init; }

		/// <summary>
		/// True, if this MapSection has a negative Y coordinate. 
		/// </summary>
		public bool IsInverted { get; init; }

		/// <summary>
		/// X,Y coords for the MapSection located at the lower, left for this Job, relative to the Subdivision BaseMapPosition in Block-Size units
		/// </summary>
		public BigVector JobBlockOffset { get; init; }

		// TODO: Confirm that the ScreenPosition and the BlockPosition - MapBlockOffset are always identical.

		/// <summary>
		/// X,Y coords on screen in Block-Size units
		/// </summary>
		public PointInt ScreenPosition { get; init; }

		public VectorInt ScreenPositionReleativeToCenter { get; init; }

		/// <summary>
		/// X,Y coords for this MapSection in absolute map coordinates. Equal to the (BlockPosition + Subdivision.BaseMapPosition) x BlockSize x SamplePointDelta 
		/// </summary>
		public RPoint MapPosition { get; init; }

		public SizeInt BlockSize { get; init; }
		
		public RSize SamplePointDelta { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public int MapLoaderJobNumber { get; set; }
		public int RequestNumber { get; init; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public int Precision { get; set; }
		public int LimbCount { get; set; }

		public MapSectionVectors2? MapSectionVectors2 { get; set; }
		public MapSectionZVectors? MapSectionZVectors { get; set; }

		public string? ClientEndPointAddress { get; set; }
		public bool IncreasingIterations { get; set; }

		public bool Pending { get; set; }
		public bool Sent { get; set; }
		public bool FoundInRepo { get; set; }
		public bool Completed { get; set; }
		public bool Saved { get; set; }
		public bool Handled { get; set; }
		public bool Cancelled { get; set; }

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan? TimeToCompleteGenRequest { get; set; }
		public TimeSpan? ProcessingDuration => ProcessingEndTime.HasValue ? ProcessingEndTime - ProcessingStartTime : null;
		public TimeSpan? GenerationDuration { get; set; }

		public override string ToString()
		{
			return $"Id: {MapSectionId}, S:{SubdivisionId}, ScrPos:{ScreenPosition}.";
		}

		public (MapSectionVectors2? mapSectionVectors, MapSectionZVectors? mapSectionZVectors) TransferMapVectorsOut2()
		{
			var msv = MapSectionVectors2;
			var mszv = MapSectionZVectors;

			MapSectionVectors2 = null;
			MapSectionZVectors = null;

			return (msv, mszv);
		}

	}
}

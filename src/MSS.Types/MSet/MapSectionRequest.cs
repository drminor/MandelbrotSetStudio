using System;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MapSectionRequest
	{
		public MapSectionRequest(MsrJob msrJob) : this(msrJob, requestNumber: 0, mapPosition: new RPoint(), screenPosition: new PointInt(),
			screenPositionRelativeToCenter: new VectorInt(), sectionBlockOffset: new VectorLong(), isInverted: false)
		{ }

		public MapSectionRequest(MsrJob msrJob, int requestNumber, RPoint mapPosition, PointInt screenPosition, VectorInt screenPositionRelativeToCenter, VectorLong sectionBlockOffset, bool isInverted)
		{
			MsrJob = msrJob ?? throw new ArgumentNullException(nameof(MsrJob), "All MapSectionRequest must reference a MsrJob.");

			RequestNumber = requestNumber;
			RequestId = MsrJob.MapLoaderJobNumber + "/" + RequestNumber;

			Mirror = null;
			MapSectionId = null;

			ScreenPosition = screenPosition;
			ScreenPositionReleativeToCenter = screenPositionRelativeToCenter;

			SectionBlockOffset = sectionBlockOffset;
			MapPosition = mapPosition;
			IsInverted = isInverted;

			CancellationTokenSource = new CancellationTokenSource();
			ProcessingStartTime = DateTime.UtcNow;
		}

		public MsrJob MsrJob { get; init; }

		public int MapLoaderJobNumber => MsrJob.MapLoaderJobNumber;
		public int RequestNumber { get; init; }

		public string RequestId { get; init; }

		public string? MapSectionId { get; set; }

		public MapSectionRequest? Mirror { get; set; }

		public bool RequestOrMirrorIsInPlay => !IsCancelled || (Mirror != null && !Mirror.IsCancelled);
		public bool NeitherRequestNorMirrorIsInPlay => !RequestOrMirrorIsInPlay;

		public JobType JobType => MsrJob.JobType;
		public string JobId => MsrJob.JobId;
		public OwnerType OwnerType => MsrJob.OwnerType;

		public Subdivision Subdivision => MsrJob.Subdivision;

		//public string SubdivisionId => MsrJob.Subdivision.Id.ToString();
		public string OriginalSourceSubdivisionId => MsrJob.OriginalSourceSubdivisionId;

		/// <summary>
		/// X,Y coords on screen in Block-Size units
		/// </summary>
		public PointInt ScreenPosition { get; init; }

		public VectorInt ScreenPositionReleativeToCenter { get; init; }

		/// <summary>
		/// X,Y coords for the MapSection located at the lower, left for this Job, relative to the Subdivision BaseMapPosition in Block-Size units
		/// </summary>
		public VectorLong JobBlockOffset => MsrJob.JobBlockOffset;

		/// <summary>
		/// X,Y coords for this MapSection, relative to the Subdivision BaseMapPosition in Block-Size units.
		/// </summary>
		//public BigVector RepoBlockPosition { get; init; }
		public VectorLong SectionBlockOffset { get; init; }

		/// <summary>
		/// X,Y coords for this MapSection in absolute map coordinates. Equal to the (BlockPosition + Subdivision.BaseMapPosition) x BlockSize x SamplePointDelta 
		/// </summary>
		public RPoint MapPosition { get; init; }

		/// <summary>
		/// True, if this MapSection has a negative Y coordinate. 
		/// </summary>
		public bool IsInverted { get; init; }

		public int Precision => MsrJob.Precision;
		public int LimbCount => MsrJob.LimbCount;
		public SizeInt BlockSize => MsrJob.BlockSize;

		public RSize SamplePointDelta => MsrJob.SamplePointDelta;
		public MapCalcSettings MapCalcSettings => MsrJob.MapCalcSettings;

		public CancellationTokenSource CancellationTokenSource { get; set; }

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
		public bool IsCancelled { get; set; }

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan? TimeToCompleteGenRequest { get; set; }
		public TimeSpan? ProcessingDuration => ProcessingEndTime.HasValue ? ProcessingEndTime - ProcessingStartTime : null;
		public TimeSpan? GenerationDuration { get; set; }

		public override string ToString()
		{
			return $"Id: {MapSectionId}, S:{Subdivision.Id}, ScrPos:{ScreenPosition}.";
		}

		public (MapSectionVectors2? mapSectionVectors, MapSectionZVectors? mapSectionZVectors) TransferMapVectorsOut2()
		{
			var msv = MapSectionVectors2;
			var mszv = MapSectionZVectors;

			MapSectionVectors2 = null;
			MapSectionZVectors = null;

			return (msv, mszv);
		}

		public bool Cancel()
		{
			if (IsCancelled)
				return false;

			IsCancelled = true;
			CancellationTokenSource.Cancel();

			if (MsrJob.TotalNumberOfSectionsRequested > 0)
			{
				MsrJob.SectionsCancelled++;
			}

			return true;
		}

	}
}

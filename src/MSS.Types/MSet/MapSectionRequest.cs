using MSS.Common;
using System;
using System.Diagnostics;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MapSectionRequest
	{
		public MapSectionRequest() : this(new MsrJob(), mapPosition: new RPoint(), new MsrPosition())
		{ }

		//public MapSectionRequest(MsrJob msrJob, int requestNumber, RPoint mapPosition, PointInt screenPosition, VectorInt screenPositionRelativeToCenter, VectorLong sectionBlockOffset, bool isInverted)
		//{
		//	MsrJob = msrJob ?? throw new ArgumentNullException(nameof(MsrJob), "All MapSectionRequest must reference a MsrJob.");

		//	var msrPos = new MsrPosition(requestNumber, screenPosition, screenPositionRelativeToCenter, sectionBlockOffset, isInverted);

		//	if (isInverted)
		//	{
		//		InvertedPosition = msrPos;
		//	}
		//	else
		//	{
		//		RegularPosition = msrPos;
		//	}

		//	RequestNumber = requestNumber;
		//	RequestId = MsrJob.MapLoaderJobNumber + "/" + RequestNumber;

		//	//Mirror = null;
		//	MapSectionId = null;

		//	//ScreenPosition = screenPosition;
		//	//ScreenPositionReleativeToCenter = screenPositionRelativeToCenter;
		//	//IsInverted = isInverted;

		//	SectionBlockOffset = sectionBlockOffset;
		//	MapPosition = mapPosition;

		//	CancellationTokenSource = new CancellationTokenSource();
		//	ProcessingStartTime = DateTime.UtcNow;
		//}

		public MapSectionRequest(MsrJob msrJob, RPoint mapPosition, MsrPosition msrPosition)
		{
			MsrJob = msrJob ?? throw new ArgumentNullException(nameof(MsrJob), "All MapSectionRequest must reference a MsrJob.");

			if (msrPosition.IsInverted)
			{
				InvertedPosition = msrPosition;
			}
			else
			{
				RegularPosition = msrPosition;
			}

			RequestNumber = msrPosition.RequestNumber;
			RequestId = MsrJob.MapLoaderJobNumber + "/" + RequestNumber;

			MapSectionId = null;

			ScreenPosition = msrPosition.ScreenPosition;
			ScreenPositionReleativeToCenter = msrPosition.ScreenPositionReleativeToCenter;
			SectionBlockOffset = msrPosition.SectionBlockOffset;
			IsInverted = msrPosition.IsInverted;

			MapPosition = mapPosition;

			CancellationTokenSource = new CancellationTokenSource();
			ProcessingStartTime = DateTime.UtcNow;
		}

		public MapSectionRequest(MsrJob msrJob, RPoint mapPosition, MsrPosition regularPosition, MsrPosition invertedPosition)
		{
			MsrJob = msrJob ?? throw new ArgumentNullException(nameof(MsrJob), "All MapSectionRequest must reference a MsrJob.");

			Debug.Assert(!regularPosition.IsInverted & invertedPosition.IsInverted, "Upon MapSectionRequest contruction, the RegularPosition is inverted or the InvertedPosition is not inverted.");

			RegularPosition = regularPosition;
			InvertedPosition = invertedPosition;
			Debug.Assert(regularPosition.SectionBlockOffset == invertedPosition.SectionBlockOffset, "The RegularPosition's SectionBlockOffset is not the same as the InvertedPosition's SectionBlockOffset.");

			RequestNumber = regularPosition.RequestNumber;
			RequestId = MsrJob.MapLoaderJobNumber + "/" + RequestNumber;

			MapSectionId = null;

			ScreenPosition = regularPosition.ScreenPosition;
			ScreenPositionReleativeToCenter = regularPosition.ScreenPositionReleativeToCenter;
			SectionBlockOffset = regularPosition.SectionBlockOffset;
			IsInverted = false;

			MapPosition = mapPosition;

			CancellationTokenSource = new CancellationTokenSource();
			ProcessingStartTime = DateTime.UtcNow;
		}

		public MsrJob MsrJob { get; init; }

		public int MapLoaderJobNumber => MsrJob.MapLoaderJobNumber;
		public int RequestNumber { get; init; }

		public string RequestId { get; init; }

		public string? MapSectionId { get; set; }

		public MsrPosition?	RegularPosition { get; init; }
		public MsrPosition? InvertedPosition { get; init; }

		public CancellationTokenSource CancellationTokenSource { get; init; }

		public bool HasRegular => RegularPosition != null;
		public bool HasInverted => InvertedPosition != null;
		public bool IsPaired => HasRegular & HasInverted;

		public bool RegularOrInvertedRequestIsInPlay => RegularPosition != null && !RegularPosition.IsCancelled || (InvertedPosition != null && !InvertedPosition.IsCancelled);
		public bool NeitherRegularOrInvertedRequestIsInPlay => !RegularOrInvertedRequestIsInPlay;

		public JobType JobType => MsrJob.JobType;
		public string JobId => MsrJob.JobId.ToString();
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
		/// True, if this MapSection has a negative Y coordinate. 
		/// </summary>
		public bool IsInverted { get; init; }

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

		public int Precision => MsrJob.Precision;
		public int LimbCount => MsrJob.LimbCount;
		public SizeInt BlockSize => MsrJob.BlockSize;

		public RSize SamplePointDelta => MsrJob.SamplePointDelta;
		public MapCalcSettings MapCalcSettings => MsrJob.MapCalcSettings;

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

		public bool IsCancelled
		{
			get
			{
				Debug.Assert(RegularPosition != null | InvertedPosition != null, "No MapSectionRequest should ever have both the Regular and Inverted Positions be null.");

				bool result;


				if (IsPaired)
				{
					result = RegularPosition!.IsCancelled & InvertedPosition!.IsCancelled;
				}
				else
				{
					if (RegularPosition != null)
					{
						result = RegularPosition.IsCancelled;
					}
					else
					{
						result = InvertedPosition!.IsCancelled;
					}
				}

				CheckIsCancelledResult(result, NeitherRegularOrInvertedRequestIsInPlay);

				return result;
			}
		}

		[Conditional("DEBUG2")]
		private void CheckIsCancelledResult(bool isCancelled, bool neitherRegularOrInvertedRequestIsInPlay)
		{
			Debug.Assert(isCancelled == neitherRegularOrInvertedRequestIsInPlay, "MapSectionRequest: IsCancelled has a value different from the NeitherRegularOrInvertedRequestIsInPlay property.");
		}

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan? TimeToCompleteGenRequest { get; set; }
		public TimeSpan? ProcessingDuration => ProcessingEndTime.HasValue ? ProcessingEndTime - ProcessingStartTime : null;
		public TimeSpan? GenerationDuration { get; set; }

		public bool AllRowsHaveEscaped { get; set; }
		public bool RequestWasCompleted { get; set; }

		public MathOpCounts? MathOpCounts { get; set; }

		public override string ToString()
		{
			if (IsPaired)
			{
				return $"Id: {RequestId}, S:{Subdivision.Id}, Regular Pos: {RegularPosition!.ScreenPosition}; Inverted Pos: {InvertedPosition!.ScreenPosition}. Cancelled: {RegularPosition.IsCancelled}/{InvertedPosition.IsCancelled}.";
			}
			else if (RegularPosition != null)
			{
				return $"Id: {RequestId}, S:{Subdivision.Id}, Regular Pos: {RegularPosition.ScreenPosition}. Cancelled: {RegularPosition.IsCancelled}";
			}
			else
			{
				if (InvertedPosition == null) throw new NullReferenceException("This MapSection has for the Regular and Inverted Postion the NULL value.");
				return $"Id: {RequestId}, S:{Subdivision.Id}, Inverted Pos: {InvertedPosition.ScreenPosition}. Cancelled: {InvertedPosition.IsCancelled}";
			}
		}

		public (MapSectionVectors2? mapSectionVectors, MapSectionZVectors? mapSectionZVectors) TransferMapVectorsOut()
		{
			var msv = MapSectionVectors2;
			var mszv = MapSectionZVectors;

			MapSectionVectors2 = null;
			MapSectionZVectors = null;

			return (msv, mszv);
		}

		public void Cancel()
		{
			if (RegularPosition != null)
			{
				if (RegularPosition.Cancel())
				{
					MsrJob.IncrementSectionsCancelled();
				}
			}

			if (InvertedPosition != null)
			{
				if (InvertedPosition.Cancel())
				{
					MsrJob.IncrementSectionsCancelled();
				}
			}
		}

		public bool Cancel(bool isInverted)
		{
			if (isInverted)
			{
				if (InvertedPosition == null) throw new NullReferenceException("InvertedPosition was null on call to Cancel(isInverted = true).");

				if (InvertedPosition.Cancel())
				{
					MsrJob.IncrementSectionsCancelled();
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				if (RegularPosition == null) throw new NullReferenceException("RegularPosition was null on call to Cancel(isInverted = false).");

				if (RegularPosition.Cancel())
				{
					MsrJob.IncrementSectionsCancelled();
					return true;
				}
				else
				{
					return false;
				}
			}
		}

	}
}

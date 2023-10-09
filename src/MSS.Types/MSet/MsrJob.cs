using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MsrJob : IMsrJob
	{
		public MsrJob() : this(mapLoaderJobNumber: 0, jobType: JobType.FullScale, jobId: "", ownerType: OwnerType.Project, subdivision: new Subdivision(), originalSourceSubdivisionId: "",
			jobBlockOffset: new VectorLong(), precision: 0, limbCount: 0, mapCalcSettings: new MapCalcSettings(), crossesXZero: false)
		{ }

		public MsrJob(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType ownerType, Subdivision subdivision, string originalSourceSubdivisionId, 
			VectorLong jobBlockOffset, int precision, int limbCount, MapCalcSettings mapCalcSettings, bool crossesXZero)
		{
			ObjectId test = new ObjectId(originalSourceSubdivisionId);

			if (test == ObjectId.Empty)
			{
				Debug.WriteLine($"The originalSourceSubdivisionId is blank during MapSectionRequest construction.");
			}

			MapLoaderJobNumber = mapLoaderJobNumber;
			
			JobType = jobType;
			JobId = jobId;
			OwnerType = ownerType;
			Subdivision = subdivision;
			OriginalSourceSubdivisionId = originalSourceSubdivisionId;
			JobBlockOffset = jobBlockOffset;
			Precision = precision;
			LimbCount = limbCount;
			MapCalcSettings = mapCalcSettings;
			CrossesYZero = crossesXZero;

			MapSectionRequests = null;

			CancellationTokenSource = new CancellationTokenSource();
		}

		#region Events

		public event EventHandler<MapSectionProcessInfo>? MapSectionLoaded;
		public event EventHandler? JobHasCompleted;

		#endregion

		#region Public Properties

		public int MapLoaderJobNumber { get; set; }

		public int JobNumber => MapLoaderJobNumber;

		public JobType JobType { get; init; }
		public string JobId { get; init; }
		public OwnerType OwnerType { get; init; }
		public Subdivision Subdivision { get; init; }
		public string OriginalSourceSubdivisionId { get; init; }

		/// <summary>
		/// X,Y coords for the MapSection located at the lower, left for this Job, relative to the Subdivision BaseMapPosition in Block-Size units
		/// </summary>
		public VectorLong JobBlockOffset { get; init; }

		public int Precision { get; set; }
		public int LimbCount { get; set; }

		public SizeInt BlockSize => Subdivision.BlockSize;
		public RSize SamplePointDelta => Subdivision.SamplePointDelta;
		public MapCalcSettings MapCalcSettings { get; init; }
		public bool CrossesYZero { get; init; }

		public bool Cancelled { get; set; }
		public CancellationTokenSource CancellationTokenSource { get; set; }

		public bool IncreasingIterations { get; set; }


		#endregion

		#region Pubic Properties - Optional

		public List<MapSectionRequest>? MapSectionRequests { get; set; }


		public string? ClientEndPointAddress { get; set; }

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan ElaspedTime { get; set; }
		public TimeSpan TotalExecutionTime { get; set; }

		public MathOpCounts? MathOpCounts { get; }

		public int NumSectionsRequested { get; }
		public int NumSectionsSubmitted { get; }
		public int NumSectionsCompleted { get; }

		#endregion

		#region Public Methods

		public int GetNumberOfRequestsPendingSubmittal()
		{
			return 0;
		}

		public int GetNumberOfRequestsPendingGeneration()
		{
			return 0;
		}

		public void Cancel()
		{

		}

		public void MarkJobAsComplete()
		{

		}

		#endregion

		public override string ToString()
		{
			return $"Id: {JobId}, JobNumber: {MapLoaderJobNumber}.";
		}
	}
}

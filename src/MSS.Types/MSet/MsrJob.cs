using MongoDB.Bson;
using System;
using System.Diagnostics;
using System.Threading;

namespace MSS.Types.MSet
{
	public class MsrJob
	{
		public MsrJob() : this(mapLoaderJobNumber: 0, jobType: JobType.FullScale, jobId: "", ownerType: OwnerType.Project, subdivision: new Subdivision(), originalSourceSubdivisionId: "",
			jobBlockOffset: new BigVector(), precision: 0, limbCount: 0, mapCalcSettings: new MapCalcSettings())
		{ }

		public MsrJob(int mapLoaderJobNumber, JobType jobType, string jobId, OwnerType ownerType, Subdivision subdivision, string originalSourceSubdivisionId, 
			BigVector jobBlockOffset, int precision, int limbCount, MapCalcSettings mapCalcSettings)
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
			CrossesYZero = false;

			CancellationTokenSource = new CancellationTokenSource();
		}

		public int MapLoaderJobNumber { get; set; }

		public JobType JobType { get; init; }
		public string JobId { get; init; }
		public OwnerType OwnerType { get; init; }
		public Subdivision Subdivision { get; init; }
		public string OriginalSourceSubdivisionId { get; init; }

		/// <summary>
		/// X,Y coords for the MapSection located at the lower, left for this Job, relative to the Subdivision BaseMapPosition in Block-Size units
		/// </summary>
		public BigVector JobBlockOffset { get; init; }

		public int Precision { get; set; }
		public int LimbCount { get; set; }

		public SizeInt BlockSize => Subdivision.BlockSize;
		public RSize SamplePointDelta => Subdivision.SamplePointDelta;
		public MapCalcSettings MapCalcSettings { get; init; }
		public bool CrossesYZero { get; init; }

		public CancellationTokenSource CancellationTokenSource { get; set; }

		public string? ClientEndPointAddress { get; set; }
		public bool IncreasingIterations { get; set; }

		public bool Cancelled { get; set; }

		public DateTime? ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }


		public override string ToString()
		{
			return $"Id: {JobId}, JobNumber: {MapLoaderJobNumber}.";
		}

	}
}

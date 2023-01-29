using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class MapSectionResponse
	{
		public MapSectionResponse()
			: this(
				  mapSectionId: string.Empty,
				  ownerId: string.Empty, 
				  jobOwnerType: JobOwnerType.Undetermined, 
				  subdivisionId: string.Empty,
				  blockPosition: new BigVector(),
				  //blockSize: new SizeInt(),
				  //limbCount: 0
				  mapCalcSettings: null
				  )
		{
		}

		public MapSectionResponse(MapSectionRequest mapSectionRequest)
			: this(
				  mapSectionRequest.MapSectionId, 
				  mapSectionRequest.OwnerId,
				  mapSectionRequest.JobOwnerType,
				  mapSectionRequest.SubdivisionId,
				  mapSectionRequest.BlockPosition,
				  //mapSectionRequest.BlockSize,
				  //mapSectionRequest.LimbCount,
				  mapSectionRequest.MapCalcSettings
				  )
		{
		}

		public MapSectionResponse(string? mapSectionId, string ownerId, JobOwnerType jobOwnerType, string subdivisionId, 
			BigVector blockPosition/*, SizeInt blockSize, int limbCount*/, MapCalcSettings? mapCalcSettings)
		{
			MapSectionId = mapSectionId;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			//BlockSize = blockSize;
			//LimbCount = limbCount;
			MapCalcSettings = mapCalcSettings;

			RequestCancelled = false;
			MathOpCounts = null;
		}

		//private MapSectionVectors? _mapSectionVectors;

		//private MapSectionValues? _mapSectionValues;

		//public MapSectionVectors? MapSectionVectors
		//{
		//	get => _mapSectionVectors;
		//	set
		//	{
		//		if (_mapSectionVectors != null)
		//		{
		//			Debug.WriteLine($"WARNING: Setting the MapSectionVectors value, when it already has a value. This will not be returned to the pool.");
		//		}

		//		if (value == null)
		//		{
		//			Debug.WriteLine($"WARNING: Setting the MapSectionVectors value to null.");
		//		}
		//		else
		//		{
		//			_mapSectionValues = null;
		//		}
		//		_mapSectionVectors = value;
		//	}
		//}

		//public MapSectionValues? MapSectionValues
		//{
		//	get => _mapSectionValues;
		//	set
		//	{
		//		if (_mapSectionValues != null)
		//		{
		//			Debug.WriteLine($"WARNING: Setting the MapSectionVectors value, when it already has a value. This will not be returned to the pool.");
		//		}

		//		if (value == null)
		//		{
		//			Debug.WriteLine($"WARNING: Setting the MapSectionVectors value to null.");
		//		}
		//		else
		//		{
		//			_mapSectionVectors = null;
		//		}
		//		_mapSectionValues = value;
		//	}
		//}


		//public bool IsEmpty => string.IsNullOrEmpty(SubdivisionId);

		public MapSectionVectors? MapSectionVectors { get; set; }
		public MapSectionValues? MapSectionValues { get; set; }

		public MapSectionZVectors? MapSectionZVectors { get; set; }

		public string? MapSectionId { get; set; }
		public string OwnerId { get; set; }
		public JobOwnerType JobOwnerType { get; set; }
		public string SubdivisionId { get; init; }
		public BigVector BlockPosition { get; init; }

		//public SizeInt BlockSize { get; init; }
		//public int LimbCount { get; init; }

		public MapCalcSettings? MapCalcSettings { get; set; }

		public MathOpCounts? MathOpCounts { get; set; }

		public bool RecordOnFile => !string.IsNullOrEmpty(MapSectionId);
		public bool RequestCancelled { get; set; }
	}
}

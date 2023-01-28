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
				  mapSectionRequest.MapCalcSettings
				  )
		{
		}

		public MapSectionResponse(string? mapSectionId, string ownerId, JobOwnerType jobOwnerType, string subdivisionId, 
			BigVector blockPosition, MapCalcSettings? mapCalcSettings)
		{
			MapSectionId = mapSectionId;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;

			RequestCancelled = false;
			MathOpCounts = null;
		}


		public MapSectionVectors? MapSectionVectors { get; set; }
		public MapSectionValues? MapSectionValues { get; set; }

		//public bool IsEmpty => string.IsNullOrEmpty(SubdivisionId);

		public string? MapSectionId { get; set; }
		public string OwnerId { get; set; }
		public JobOwnerType JobOwnerType { get; set; }
		public string SubdivisionId { get; init; }
		public BigVector BlockPosition { get; init; }
		public MapCalcSettings? MapCalcSettings { get; set; }

		public MathOpCounts? MathOpCounts { get; set; }

		public bool RequestCancelled { get; set; }
	}
}

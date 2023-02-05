using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class MapSectionResponse
	{
		//public MapSectionResponse()
		//	: this(
		//		  mapSectionId: string.Empty,
		//		  ownerId: string.Empty, 
		//		  jobOwnerType: JobOwnerType.Undetermined, 
		//		  subdivisionId: string.Empty,
		//		  blockPosition: new BigVector(),
		//		  mapCalcSettings: null,
		//		  allRowsHaveEscaped: false
		//		  )
		//{ }

		public MapSectionResponse(MapSectionRequest mapSectionRequest, bool isCancelled = false)
			: this(
				  mapSectionRequest.MapSectionId, 
				  mapSectionRequest.OwnerId,
				  mapSectionRequest.JobOwnerType,
				  mapSectionRequest.SubdivisionId,
				  mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings,
				  allRowsHaveEscaped: false
				  )
		{
			RequestCancelled = isCancelled;
		}

		public MapSectionResponse(MapSectionRequest mapSectionRequest,
			bool allRowsHaveEscaped, MapSectionVectors? mapSectionVectors = null, MapSectionZVectors? mapSectionZVectors = null, bool isCancelled = false)
			: this(

				  mapSectionRequest.MapSectionId, 
				  mapSectionRequest.OwnerId,
				  mapSectionRequest.JobOwnerType,
				  mapSectionRequest.SubdivisionId,
				  mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings,
				  allRowsHaveEscaped, mapSectionVectors, mapSectionZVectors
				  )
		{
			RequestCancelled = isCancelled;
		}

		public MapSectionResponse(
			string? mapSectionId, 
			string ownerId, 
			JobOwnerType jobOwnerType, 
			string subdivisionId, 
			BigVector blockPosition,
			MapCalcSettings? mapCalcSettings,
			bool allRowsHaveEscaped, MapSectionVectors? mapSectionVectors = null, MapSectionZVectors? mapSectionZVectors = null)
		{
			MapSectionId = mapSectionId;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;
			AllRowsHaveEscaped = allRowsHaveEscaped;
			
			MapSectionVectors = mapSectionVectors;
			MapSectionZVectors = mapSectionZVectors;

			RequestCancelled = false;
			//MathOpCounts = null;
		}

		public MapSectionVectors? MapSectionVectors { get; set; }
		public MapSectionZVectors? MapSectionZVectors { get; set; }
		public bool AllRowsHaveEscaped { get; set; }


		public string? MapSectionId { get; set; }
		public string OwnerId { get; set; }
		public JobOwnerType JobOwnerType { get; set; }
		public string SubdivisionId { get; init; }
		public BigVector BlockPosition { get; init; }

		public MapCalcSettings? MapCalcSettings { get; set; }

		//public MathOpCounts? MathOpCounts { get; set; }

		public bool RecordOnFile => !string.IsNullOrEmpty(MapSectionId);
		public bool RequestCancelled { get; set; }
	}
}

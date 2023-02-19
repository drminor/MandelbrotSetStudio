using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class MapSectionResponse
	{
		public MapSectionResponse(MapSectionRequest mapSectionRequest, 
			bool isCancelled = false)
			: this(
				  mapSectionRequest.MapSectionId, 
				  mapSectionRequest.OwnerId,
				  mapSectionRequest.JobOwnerType,
				  mapSectionRequest.SubdivisionId,
				  mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings,
				  requestCompleted: false,
				  allRowsHaveEscaped: false,
				  mapSectionVectors: null,
				  mapSectionZVectors: null,
				  isCancelled
				  )
		{ }

		public MapSectionResponse(MapSectionRequest mapSectionRequest, 
			bool requestCompleted,
			bool allRowsHaveEscaped, 
			MapSectionVectors? mapSectionVectors = null, MapSectionZVectors? mapSectionZVectors = null, bool isCancelled = false)
			: this(
				  mapSectionRequest.MapSectionId, 
				  mapSectionRequest.OwnerId,
				  mapSectionRequest.JobOwnerType,
				  mapSectionRequest.SubdivisionId,
				  mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings,
				  requestCompleted,
				  allRowsHaveEscaped, 
				  mapSectionVectors, 
				  mapSectionZVectors,
				  isCancelled
				  )
		{ }

		public MapSectionResponse(
			string? mapSectionId, 
			string ownerId, 
			JobOwnerType jobOwnerType, 
			string subdivisionId, 
			BigVector blockPosition,
			MapCalcSettings mapCalcSettings,
			bool requestCompleted,
			bool allRowsHaveEscaped, 
			MapSectionVectors? mapSectionVectors = null, MapSectionZVectors? mapSectionZVectors = null, bool requestCancelled = false)
		{
			MapSectionId = mapSectionId;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;
			RequestCompleted = requestCompleted;
			AllRowsHaveEscaped = allRowsHaveEscaped;
			RequestCancelled = requestCancelled;
			
			MapSectionVectors = mapSectionVectors;
			MapSectionZVectors = mapSectionZVectors;
		}

		public string? MapSectionId { get; set; }
		public string OwnerId { get; set; }
		public JobOwnerType JobOwnerType { get; set; }

		public string SubdivisionId { get; init; }
		public BigVector BlockPosition { get; init; }

		public MapCalcSettings MapCalcSettings { get; init; }
		public bool RequestCompleted { get; set; }
		public bool AllRowsHaveEscaped { get; set; }
		public bool RequestCancelled { get; set; }

		public MapSectionVectors? MapSectionVectors { get; set; }
		public MapSectionZVectors? MapSectionZVectors { get; set; }

		public bool RecordOnFile => !string.IsNullOrEmpty(MapSectionId);
	}
}

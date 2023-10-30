namespace MSS.Types.MSet
{
	public class MapSectionResponse
	{
		public MapSectionResponse(MapSectionRequest mapSectionRequest, 
			bool isCancelled = false)
			: this(
				  mapSectionRequest.MapSectionId, 
				  mapSectionRequest.Subdivision.Id.ToString(),
				  mapSectionRequest.SectionBlockOffset,
				  mapSectionRequest.MapCalcSettings,
				  requestCompleted: false,
				  allRowsHaveEscaped: false,
				  //mapSectionVectors: null,
				  mapSectionVectors2: null,
				  mapSectionZVectors: null,
				  requestCancelled: isCancelled
				  )
		{ }

		public MapSectionResponse(MapSectionRequest mapSectionRequest,
			bool requestCompleted,
			bool allRowsHaveEscaped,
			MapSectionVectors2? mapSectionVectors2 = null, 
			MapSectionZVectors? mapSectionZVectors = null, 
			bool requestCancelled = false)
			: this(
				  mapSectionRequest.MapSectionId,
				  mapSectionRequest.Subdivision.Id.ToString(),
				  blockPosition: mapSectionRequest.SectionBlockOffset,
				  mapSectionRequest.MapCalcSettings,
				  requestCompleted,
				  allRowsHaveEscaped,
				  //mapSectionVectors: null,
				  mapSectionVectors2: mapSectionVectors2,
				  mapSectionZVectors,
				  requestCancelled
				  )
		{ }

		public MapSectionResponse(
			string? mapSectionId,
			string subdivisionId,
			VectorLong blockPosition,
			MapCalcSettings mapCalcSettings,
			bool requestCompleted,
			bool allRowsHaveEscaped,
			//MapSectionVectors? mapSectionVectors = null,
			MapSectionVectors2? mapSectionVectors2 = null,
			MapSectionZVectors? mapSectionZVectors = null, 
			bool requestCancelled = false)
		{
			MapSectionId = mapSectionId;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;
			RequestCompleted = requestCompleted;
			AllRowsHaveEscaped = allRowsHaveEscaped;
			RequestCancelled = requestCancelled;

			//MapSectionVectors = mapSectionVectors;
			MapSectionVectors2 = mapSectionVectors2;
			MapSectionZVectors = mapSectionZVectors;
		}

		public string? MapSectionId { get; set; }
		public string SubdivisionId { get; init; }

		public VectorLong BlockPosition { get; init; }

		public MapCalcSettings MapCalcSettings { get; init; }
		public bool RequestCompleted { get; set; }
		public bool AllRowsHaveEscaped { get; set; }
		public bool RequestCancelled { get; set; }
		
		//public MathOpCounts? MathOpCounts { get; set; }

		//public MapSectionVectors? MapSectionVectors { get; set; }
		public MapSectionVectors2? MapSectionVectors2 { get; set; }
		public MapSectionZVectors? MapSectionZVectors { get; set; }

		public bool RecordOnFile => !string.IsNullOrEmpty(MapSectionId);

		public bool AllVectorPropertiesAreNull => !(/*MapSectionVectors != null || */MapSectionVectors2 != null || MapSectionZVectors != null);

		public static MapSectionResponse CreateCancelledResponseWithVectors(MapSectionRequest request)
		{
			var mapSectionResponse = new MapSectionResponse(request, isCancelled: true);
			
			var (mapSectionVectors2, mapSectionZVectors) = request.TransferMapVectorsOut();

			mapSectionResponse.MapSectionVectors2 = mapSectionVectors2;
			mapSectionResponse.MapSectionZVectors = mapSectionZVectors;

			return mapSectionResponse;
		}

	}
}

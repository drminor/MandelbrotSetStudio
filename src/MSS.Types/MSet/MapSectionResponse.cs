namespace MSS.Types.MSet
{
	public class MapSectionResponse
	{
		public MapSectionResponse(MapSectionRequest mapSectionRequest, 
			bool isCancelled = false)
			: this(
				  mapSectionRequest.MapSectionId, 
				  mapSectionRequest.SubdivisionId,
				  mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings,
				  requestCompleted: false,
				  allRowsHaveEscaped: false,
				  mapSectionVectors2: null,
				  mapSectionZVectors: null,
				  requestCancelled: isCancelled
				  )
		{ }

		//public MapSectionResponse(MapSectionRequest mapSectionRequest, 
		//	bool requestCompleted,
		//	bool allRowsHaveEscaped, 
		//	MapSectionVectors? mapSectionVectors = null, MapSectionZVectors? mapSectionZVectors = null, bool requestCancelled = false)
		//	: this(
		//		  mapSectionRequest.MapSectionId, 
		//		  mapSectionRequest.SubdivisionId,
		//		  blockPosition: mapSectionRequest.BlockPosition,
		//		  mapSectionRequest.MapCalcSettings,
		//		  requestCompleted,
		//		  allRowsHaveEscaped, 
		//		  mapSectionVectors, 
		//		  mapSectionZVectors,
		//		  requestCancelled
		//		  )
		//{ }

		public MapSectionResponse(MapSectionRequest mapSectionRequest,
			bool requestCompleted,
			bool allRowsHaveEscaped,
			MapSectionVectors2? mapSectionVectors = null, MapSectionZVectors? mapSectionZVectors = null, bool requestCancelled = false)
			: this(
				  mapSectionRequest.MapSectionId,
				  mapSectionRequest.SubdivisionId,
				  blockPosition: mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings,
				  requestCompleted,
				  allRowsHaveEscaped,
				  mapSectionVectors,
				  mapSectionZVectors,
				  requestCancelled
				  )
		{ }


		public MapSectionResponse(
			string? mapSectionId,
			string subdivisionId,
			BigVector blockPosition,
			MapCalcSettings mapCalcSettings,
			bool requestCompleted,
			bool allRowsHaveEscaped,
			MapSectionVectors? mapSectionVectors = null, MapSectionZVectors? mapSectionZVectors = null, bool requestCancelled = false)
		{
			MapSectionId = mapSectionId;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;
			RequestCompleted = requestCompleted;
			AllRowsHaveEscaped = allRowsHaveEscaped;
			RequestCancelled = requestCancelled;

			MapSectionVectors = mapSectionVectors;
			MapSectionVectors2 = null;
			MapSectionZVectors = mapSectionZVectors;
		}

		public MapSectionResponse(
			string? mapSectionId,
			string subdivisionId,
			BigVector blockPosition,
			MapCalcSettings mapCalcSettings,
			bool requestCompleted,
			bool allRowsHaveEscaped,
			MapSectionVectors2? mapSectionVectors2 = null, MapSectionZVectors? mapSectionZVectors = null, bool requestCancelled = false)
		{
			MapSectionId = mapSectionId;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;
			RequestCompleted = requestCompleted;
			AllRowsHaveEscaped = allRowsHaveEscaped;
			RequestCancelled = requestCancelled;

			MapSectionVectors = null;
			MapSectionVectors2 = mapSectionVectors2;
			MapSectionZVectors = mapSectionZVectors;
		}

		public string? MapSectionId { get; set; }
		public string SubdivisionId { get; init; }

		public BigVector BlockPosition { get; init; }

		public MapCalcSettings MapCalcSettings { get; init; }
		public bool RequestCompleted { get; set; }
		public bool AllRowsHaveEscaped { get; set; }
		public bool RequestCancelled { get; set; }
		
		public MathOpCounts? MathOpCounts { get; set; }

		public MapSectionVectors? MapSectionVectors { get; set; }
		public MapSectionVectors2? MapSectionVectors2 { get; set; }
		public MapSectionZVectors? MapSectionZVectors { get; set; }

		public bool RecordOnFile => !string.IsNullOrEmpty(MapSectionId);

		public MapSectionResponse CreateCopySansVectors()
		{
			var result = new MapSectionResponse(MapSectionId, SubdivisionId, BlockPosition, MapCalcSettings, 
				RequestCompleted, AllRowsHaveEscaped, mapSectionVectors2: null, mapSectionZVectors: null, requestCancelled: RequestCancelled);
			return result;
		}

	}
}

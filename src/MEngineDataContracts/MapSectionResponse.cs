using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionResponse
	{
		public MapSectionResponse()
		{
			SubdivisionId = string.Empty;
		}

		public MapSectionResponse(MapSectionRequest mapSectionRequest)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings, null, null)
		{ }

		public MapSectionResponse(MapSectionRequest mapSectionRequest, MapSectionVectors mapSectionVectors, double[] zValues)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings, mapSectionVectors, zValues)
		{ }

		public MapSectionResponse(string mapSectionId, string ownerId, JobOwnerType jobOwnerType, string subdivisionId, BigVectorDto blockPosition,
			MapCalcSettings mapCalcSettings, MapSectionVectors mapSectionVectors, double[] zValues)
		{
			MapSectionId = mapSectionId;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;
			MapSectionVectors = mapSectionVectors;
			ZValues = zValues;
			IncludeZValues = zValues != null;
			RequestCancelled = false;
		}

		[DataMember(Order = 1)]
		public string MapSectionId { get; set; }

		[DataMember(Order = 2)]
		public string OwnerId { get; set; }

		[DataMember(Order = 3)]
		public JobOwnerType JobOwnerType { get; set; }

		[DataMember(Order = 4)]
		public string SubdivisionId { get; init; }

		[DataMember(Order = 5)]
		public BigVectorDto BlockPosition { get; init; }

		[DataMember(Order = 6)]
		public MapCalcSettings MapCalcSettings { get; init; }

		public MapSectionVectors MapSectionVectors { get; set; }

		public double[] ZValuesForLocalStorage { get; init; }

		[DataMember(Order = 10)]
		public double[] ZValues
		{
			get => IncludeZValues ? ZValuesForLocalStorage : null;
			init
			{
				ZValuesForLocalStorage = value;
			}
		}

		public bool IncludeZValues { get; set; }

		public bool RequestCancelled { get; set; }

		public bool IsEmpty => string.IsNullOrEmpty(SubdivisionId);

		public MathOpCounts MathOpCounts { get; set; }

	}
}

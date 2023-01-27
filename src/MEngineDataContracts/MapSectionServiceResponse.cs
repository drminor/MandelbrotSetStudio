using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionServiceResponse
	{
		public MapSectionServiceResponse()
		{
			SubdivisionId = string.Empty;
		}

		public MapSectionServiceResponse(MapSectionServiceRequest mapSectionRequest)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings, null, null)
		{ }

		public MapSectionServiceResponse(MapSectionServiceRequest mapSectionRequest, MapSectionVectors mapSectionVectors, double[] zValues)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings, mapSectionVectors, zValues)
		{ }

		public MapSectionServiceResponse(string mapSectionId, string ownerId, JobOwnerType jobOwnerType, string subdivisionId, BigVectorDto blockPosition,
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

		// On Request -- Not on Response
		//[DataMember(Order = 5)]
		//public PointInt ScreenPosition { get; set; }

		[DataMember(Order = 5)]										// 6
		public BigVectorDto BlockPosition { get; init; }

		/* On Request -- Not on Response
		[DataMember(Order = 7)]
		public RPointDto Position { get; set; }

		[DataMember(Order = 8)]
		public int Precision { get; set; }

		[DataMember(Order = 9)]
		public SizeInt BlockSize { get; set; }

		[DataMember(Order = 10)]
		public RSizeDto SamplePointDelta { get; set; }
		*/

		[DataMember(Order = 6)]										// 11
		public MapCalcSettings MapCalcSettings { get; init; }

		public MapSectionVectors MapSectionVectors { get; set; }    // 12

		public bool IncludeZValues { get; set; }
		public double[] ZValuesForLocalStorage { get; init; }		// 13

		[DataMember(Order = 7)]
		public double[] ZValues
		{
			get => IncludeZValues ? ZValuesForLocalStorage : null;
			init
			{
				ZValuesForLocalStorage = value;
			}
		}

		// On Request -- Not on Response
		//public bool IsInverted { get; init; }

		public bool RequestCancelled { get; set; }

		public bool IsEmpty => string.IsNullOrEmpty(SubdivisionId);

		public MathOpCounts MathOpCounts { get; set; }

	}
}

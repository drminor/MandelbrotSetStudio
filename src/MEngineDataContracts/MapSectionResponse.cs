using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionResponse //: ICloneable
	{
		public MapSectionResponse()
		{
			SubdivisionId = string.Empty;
		}

		public MapSectionResponse(MapSectionRequest mapSectionRequest)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition,
				  mapSectionRequest.MapCalcSettings, null, null)
		{ }

		//public MapSectionResponse(MapSectionRequest mapSectionRequest, bool[] hasEscapedFlags, ushort[] counts, ushort[] escapeVelocities, double[] zValues)
		//	: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition,
		//		  mapSectionRequest.MapCalcSettings, hasEscapedFlags, counts, escapeVelocities, zValues)
		//{ }

		//public MapSectionResponse(string mapSectionId, string ownerId, JobOwnerType jobOwnerType, string subdivisionId, BigVectorDto blockPosition,
		//	MapCalcSettings mapCalcSettings, bool[] hasEscapedFlags, ushort[] counts, ushort[] escapeVelocities, double[] zValues)
		//{
		//	MapSectionId = mapSectionId;
		//	OwnerId = ownerId;
		//	JobOwnerType = jobOwnerType;
		//	SubdivisionId = subdivisionId;
		//	BlockPosition = blockPosition;
		//	MapCalcSettings = mapCalcSettings;
		//	Counts = counts;
		//	EscapeVelocities = escapeVelocities;
		//	HasEscapedFlags = hasEscapedFlags;
		//	ZValues = zValues;
		//	IncludeZValues = zValues != null;
		//	RequestCancelled = false;
		//}


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

		//[DataMember(Order = 7)]
		//public bool[] HasEscapedFlags { get; init; }

		//[DataMember(Order = 8)]
		//public ushort[] Counts { get; init; }

		//[DataMember(Order = 9)]
		//public ushort[] EscapeVelocities { get; init; }

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

		//object ICloneable.Clone()
		//{
		//	return Clone();
		//}

		//public MapSectionResponse Clone()
		//{
		//	var result = new MapSectionResponse(MapSectionId, OwnerId, JobOwnerType, SubdivisionId, BlockPosition, MapCalcSettings, 
		//		Counts, EscapeVelocities, DoneFlags, ZValuesForLocalStorage);

		//	result.IncludeZValues = IncludeZValues;
		//	result.RequestCancelled = RequestCancelled;

		//	return result;
		//}
	}
}

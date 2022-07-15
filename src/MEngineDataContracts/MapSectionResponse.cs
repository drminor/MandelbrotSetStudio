using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionResponse : ICloneable
	{
		public MapSectionResponse()
		{
			Debug.WriteLine("The MapSectionResponse's paremeterless contructor is being called.");
		}

		public MapSectionResponse(MapSectionRequest mapSectionRequest)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition, 
				  mapSectionRequest.MapCalcSettings, null, null, null, null)
		{ }

		public MapSectionResponse(MapSectionRequest mapSectionRequest, ushort[] counts, ushort[] escapeVelocities, bool[] doneFlags, double[] zValues)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.OwnerId, mapSectionRequest.JobOwnerType, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition, 
				  mapSectionRequest.MapCalcSettings, counts, escapeVelocities, doneFlags, zValues)
		{ }

		public MapSectionResponse(string mapSectionId, string ownerId, int jobOwnerType, string subdivisionId, BigVectorDto blockPosition, 
			MapCalcSettings mapCalcSettings, ushort[] counts, ushort[] escapeVelocities, bool[] doneFlags, double[] zValues)
		{
			MapSectionId = mapSectionId;
			OwnerId = ownerId;
			JobOwnerType = jobOwnerType;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;
			MapCalcSettings = mapCalcSettings;
			Counts = counts;
			EscapeVelocities = escapeVelocities;
			DoneFlags = doneFlags;
			ZValues = zValues;
			IncludeZValues = zValues != null;
			RequestCancelled = false;
		}

		[DataMember(Order = 1)]
		public string MapSectionId { get; set; }

		[DataMember(Order = 2)]
		public string OwnerId { get; init; }

		[DataMember(Order = 3)]
		public int JobOwnerType { get; init; }

		[DataMember(Order = 4)]
		public string SubdivisionId { get; init; }

		[DataMember(Order = 5)]
		public BigVectorDto BlockPosition { get; init; }

		[DataMember(Order = 6)]
		public MapCalcSettings MapCalcSettings { get; init; }

		[DataMember(Order = 7)]
		public ushort[] Counts { get; init; }

		[DataMember(Order = 8)]
		public ushort[] EscapeVelocities { get; init; }

		[DataMember(Order = 9)]
		public bool[] DoneFlags { get; init; }

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

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapSectionResponse Clone()
		{
			var result = new MapSectionResponse(MapSectionId, OwnerId, JobOwnerType, SubdivisionId, BlockPosition, MapCalcSettings, 
				Counts, EscapeVelocities, DoneFlags, ZValuesForLocalStorage);

			result.IncludeZValues = IncludeZValues;
			result.RequestCancelled = RequestCancelled;

			return result;
		}
	}
}

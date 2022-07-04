using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionResponse : ICloneable
	{
		[DataMember(Order = 1)]
		public string MapSectionId { get; set; }

		[DataMember(Order = 2)]
		public string SubdivisionId { get; set; }

		[DataMember(Order = 3)]
		public BigVectorDto BlockPosition { get; set; }

		[DataMember(Order = 4)]
		public MapCalcSettings MapCalcSettings { get; set; }

		[DataMember(Order = 5)]
		public ushort[] Counts { get; set; }

		[DataMember(Order = 6)]
		public ushort[] EscapeVelocities { get; set; }

		[DataMember(Order = 7)]
		public bool[] DoneFlags { get; set; }

		public double[] ZValuesForLocalStorage { get; set; }

		[DataMember(Order = 8)]
		public double[] ZValues
		{ 
			get => IncludeZValues ? ZValuesForLocalStorage : null;
			set
			{
				ZValuesForLocalStorage = value;
			}
		}

		public bool IncludeZValues { get; set; }

		public bool RequestCancelled { get; set; }

		public bool JustNowUpdated { get; set; }

		object ICloneable.Clone()
		{
			return Clone();
		}

		public MapSectionResponse Clone()
		{
			var result = new MapSectionResponse()
			{
				MapSectionId = MapSectionId,
				SubdivisionId = SubdivisionId,
				BlockPosition = BlockPosition,
				MapCalcSettings = MapCalcSettings,
				Counts = Counts,
				EscapeVelocities = EscapeVelocities,
				DoneFlags = DoneFlags,
				ZValuesForLocalStorage = ZValuesForLocalStorage,
				IncludeZValues = IncludeZValues,
				RequestCancelled = RequestCancelled,
				JustNowUpdated = JustNowUpdated
			};

			return result;
		}
	}
}

using MSS.Types.DataTransferObjects;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionResponse
	{
		[DataMember(Order = 1)]
		public string MapSectionId { get; set; }

		[DataMember(Order = 2)]
		public string SubdivisionId { get; set; }

		[DataMember(Order = 3)]
		public BigVectorDto BlockPosition { get; set; }

		[DataMember(Order = 4)]
		public int[] Counts { get; set; }

		//[DataMember(Order = 5)]
		//public bool[] DoneFlags { get; set; }

		//[DataMember(Order = 6)]
		//public double[] ZValues { get; set; }
	}
}

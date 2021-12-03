using MSS.Types;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionResponse
	{
		[DataMember(Order = 1)]
		public string SubdivisionId { get; set; }

		[DataMember(Order = 2)]
		public PointInt BlockPosition { get; set; }

		[DataMember(Order = 3)]
		public int Status { get; set; }

		[DataMember(Order = 4)]
		public int[] Counts { get; set; }

		//[DataMember(Order = 5)]
		//public bool[] DoneFlags { get; set; }


	}
}

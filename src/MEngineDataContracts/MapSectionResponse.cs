using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionResponse
	{
		[DataMember(Order = 1)]
		public int Status { get; set; }

		[DataMember(Order = 2)]
		public int QueuePosition { get; set; }
	}
}

using MSS.Types;
using MSS.Types.MSet;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class CancelRequest
	{
		[DataMember(Order = 1)]
		public string MapSectionId { get; set; }

		[DataMember(Order = 2)]
		public string JobId { get; set; }

		[DataMember(Order = 3)]
		public OwnerType OwnerType { get; set; }

		[DataMember(Order = 4)]
		public string SubdivisionId { get; set; }

		[DataMember(Order = 5)]
		public PointInt ScreenPosition { get; set; }

		[DataMember(Order = 6)]
		public int MapLoaderJobNumber { get; set; }

		[DataMember(Order = 7)]
		public int RequestNumber { get; set; }

		public override string ToString()
		{
			return $"Job: {MapLoaderJobNumber}, Req: {RequestNumber}";
		}
	}

}

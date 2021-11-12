﻿using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	//[DataContract]
	//public class HelloReply1
	//{
	//    [DataMember(Order = 1)]
	//    public string Message { get; set; }
	//}

	//[DataContract]
	//public class HelloRequest1
	//{
	//    [DataMember(Order = 1)]
	//    public string Name { get; set; }
	//}

	[DataContract]
	public class MapSectionRequest
	{
		//public MapSectionRequest()
		//{
		//	SubdivisionId = string.Empty;
		//	BlockSize = new SizeInt();
		//}

		[DataMember(Order = 1)]
		public string SubdivisionId { get; set; }

		[DataMember(Order = 2)]
		public PointInt BlockPosition { get; set; }

		[DataMember(Order = 3)]
		public RPointDto Position { get; set; }

		[DataMember(Order = 4)]
		public SizeInt BlockSize { get; set; }

		[DataMember(Order = 5)]
		public RSizeDto SamplePointsDelta { get; set; }

		[DataMember(Order = 6)]
		public MapCalcSettings MapCalcSettings { get; set; }
	}

}

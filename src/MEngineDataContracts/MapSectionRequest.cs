using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionRequest
	{
		[DataMember(Order = 1)]
		public string MapSectionId { get; set; }

		[DataMember(Order = 2)]
		public string SubdivisionId { get; set; }

		[DataMember(Order = 3)]
		public BigVectorDto BlockPosition { get; set; }

		[DataMember(Order = 4)]
		public RPointDto Position { get; set; }

		[DataMember(Order = 5)]
		public SizeInt BlockSize { get; set; }

		[DataMember(Order = 6)]
		public RSizeDto SamplePointsDelta { get; set; }

		[DataMember(Order = 7)]
		public MapCalcSettings MapCalcSettings { get; set; }



		public bool IsInverted { get; init; }

		public bool Pending { get; set; }

		public bool Sent { get; set; }
		public bool FoundInRepo { get; set; }
		public bool Completed { get; set; }

		public bool Saved { get; set; }

		public bool Handled { get; set; } // I.e., Drawn

		public override string ToString()
		{
			var bVals = BigIntegerHelper.FromLongs(BlockPosition.GetValues());
			var bp = new BigVector(bVals);
			return $"S:{SubdivisionId}, BPos:{bp}.";
		}
	}

}

using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionServiceRequest
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
		public BigVectorDto BlockPosition { get; set; }

		[DataMember(Order = 7)]
		public RPointDto MapPosition { get; set; }

		[DataMember(Order = 8)]
		public SizeInt BlockSize { get; set; }

		[DataMember(Order = 9)]
		public RSizeDto SamplePointDelta { get; set; }

		[DataMember(Order = 10)]
		public MapCalcSettings MapCalcSettings { get; set; }

		[DataMember(Order = 11)]
		public int Precision { get; set; }

		[DataMember(Order = 12)]
		public int LimbCount { get; set; }

		[DataMember(Order = 13)]
		public bool IncreasingIterations { get; set; }

		//[DataMember(Order = 14)]
		//public ushort[] Counts { get; set; }

		//[DataMember(Order = 15)]
		//public ushort[] EscapeVelocities { get; set; }

		[DataMember(Order = 14)]
		public byte[] Counts { get; set; }

		[DataMember(Order = 15)]
		public byte[] EscapeVelocities { get; set; }

		[DataMember(Order = 16)]
		public byte[] Zrs { get; set; }

		[DataMember(Order = 17)]
		public byte[] Zis { get; set; }

		[DataMember(Order = 18)]
		public byte[] HasEscapedFlags { get; set; }

		[DataMember(Order = 19)]
		public byte[] RowHasEscaped { get; set; }

		public override string ToString()
		{
			var bVals = BigIntegerHelper.FromLongsDeprecated(BlockPosition.GetValues());
			var bp = new BigVector(bVals);
			return $"Subdivision Id:{SubdivisionId}, Block Position:{bp}, Screen Position:{ScreenPosition}.";
		}

	}

}

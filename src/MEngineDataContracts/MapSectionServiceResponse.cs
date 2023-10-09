using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionServiceResponse
	{
		public MapSectionServiceResponse()
		{
			SubdivisionId = string.Empty;
		}

		public MapSectionServiceResponse(MapSectionServiceRequest mapSectionRequest)
			: this(mapSectionRequest.MapSectionId, mapSectionRequest.SubdivisionId, mapSectionRequest.BlockPosition)
		{ }

		public MapSectionServiceResponse(string mapSectionId, string subdivisionId, VectorLong blockPosition)
		{
			MapSectionId = mapSectionId;
			SubdivisionId = subdivisionId;
			BlockPosition = blockPosition;

			RequestCompleted = false;
			AllRowsHaveEscaped = false;
			RequestCancelled = false;

			MathOpCounts = new MathOpCounts();
		}

		public bool IsEmpty => string.IsNullOrEmpty(SubdivisionId);

		[DataMember(Order = 1)]
		public string MapSectionId { get; init; }

		[DataMember(Order = 2)]
		public string SubdivisionId { get; init; }

		[DataMember(Order = 3)]
		public VectorLong BlockPosition { get; init; }

		[DataMember(Order = 4)]
		public bool RequestCompleted { get; init; }

		[DataMember(Order = 5)]
		public bool AllRowsHaveEscaped { get; init; }

		[DataMember(Order = 6)]
		public bool RequestCancelled { get; set; }

		[DataMember(Order = 7)]
		public double TimeToGenerateMs { get; init; }

		[DataMember(Order = 8)]
		public MathOpCounts MathOpCounts { get; init; }

		[DataMember(Order = 9)]
		public byte[] Counts { get; set; }

		[DataMember(Order = 10)]
		public byte[] EscapeVelocities { get; set; }

		[DataMember(Order = 11)]
		public byte[] Zrs { get; set; }

		[DataMember(Order = 12)]
		public byte[] Zis { get; set; }

		[DataMember(Order = 13)]
		public byte[] HasEscapedFlags { get; set; }

		[DataMember(Order = 14)]
		public byte[] RowHasEscaped { get; set; }

	}
}

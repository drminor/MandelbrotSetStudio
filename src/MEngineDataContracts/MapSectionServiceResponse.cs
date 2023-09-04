using MSS.Types;
using MSS.Types.DataTransferObjects;
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

		public MapSectionServiceResponse(string mapSectionId, string subdivisionId, BigVectorDto blockPosition)
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
		public BigVectorDto BlockPosition { get; init; }

		//public bool IncludeZValues { get; set; }
		//public double[] ZValuesForLocalStorage { get; init; }

		//[DataMember(Order = 7)]
		//public double[] ZValues
		//{
		//	get => IncludeZValues ? ZValuesForLocalStorage : null;
		//	init
		//	{
		//		ZValuesForLocalStorage = value;
		//	}
		//}

		[DataMember(Order = 4)]
		public bool RequestCompleted { get; init; }

		[DataMember(Order = 5)]
		public bool AllRowsHaveEscaped { get; init; }

		[DataMember(Order = 6)]
		public bool RequestCancelled { get; init; }

		[DataMember(Order = 7)]
		public double TimeToGenerate { get; init; }

		[DataMember(Order = 8)]
		public MathOpCounts MathOpCounts { get; init; }

		[DataMember(Order = 9)]
		public ushort[] Counts { get; init; }

		[DataMember(Order = 10)]
		public ushort[] EscapeVelocities { get; init; }

	}
}

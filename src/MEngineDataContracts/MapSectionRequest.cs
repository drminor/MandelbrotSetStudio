using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Linq;
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

		[DataMember(Order = 8)]
		public ushort[] Counts { get; set; }

		[DataMember(Order = 9)]
		public ushort[] EscapeVelocities { get; set; }

		[DataMember(Order = 10)]
		public bool[] DoneFlags { get; set; }

		[DataMember(Order = 11)]
		public double[] ZValues { get; set; }

		public bool IsInverted { get; init; }

		public bool Pending { get; set; }
		public bool Sent { get; set; }
		public bool FoundInRepo { get; set; }
		public bool Completed { get; set; }
		public bool Saved { get; set; }
		public bool Handled { get; set; }

		public bool IncreasingIterations { get; set; }
		
		public string ClientEndPointAddress { get; set; }
		public TimeSpan? TimeToCompleteGenRequest { get; set; }

		public bool GetIsDone()
		{
			if (DoneFlags == null)
			{
				return false;
			}

			var result = !DoneFlags.Any(x => !x);
			return result;
		}

		public override string ToString()
		{
			var bVals = BigIntegerHelper.FromLongs(BlockPosition.GetValues());
			var bp = new BigVector(bVals);
			return $"S:{SubdivisionId}, BPos:{bp}.";
		}

		private const int VALUE_FACTOR = 10000;
		public static int[] CombineCountsAndEscapeVelocities(ushort[] counts, ushort[] escapeVelocities)
		{
			var result = new int[counts.Length];

			for(var i = 0; i < counts.Length; i++)
			{
				result[i] = (counts[i] * VALUE_FACTOR) + escapeVelocities[i];
			}

			return result;
		}

		public static ushort[] SplitCountsAndEscapeVelocities(int[] rawCounts, out ushort[] escapeVelocities)
		{
			var result = new ushort[rawCounts.Length];
			escapeVelocities = new ushort[rawCounts.Length];

			for (var i = 0; i < rawCounts.Length; i++)
			{
				result[i] = (ushort)Math.DivRem(rawCounts[i], VALUE_FACTOR, out var ev);
				escapeVelocities[i] = (ushort)ev;
			}

			return result;
		}

		public static void SplitCountsAndEscapeVelocities(int[] rawCounts, Span<ushort> counts, Span<ushort> escapeVelocities)
		{
			for (var i = 0; i < rawCounts.Length; i++)
			{
				counts[i] = (ushort)Math.DivRem(rawCounts[i], VALUE_FACTOR, out var ev);
				escapeVelocities[i] = (ushort)ev;
			}
		}

	}

}

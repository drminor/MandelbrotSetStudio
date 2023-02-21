﻿using MSS.Types;
using MSS.Types.DataTransferObjects;
using MSS.Types.MSet;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace MEngineDataContracts
{
	[DataContract]
	public class MapSectionServiceRequest
	{
		[DataMember(Order = 1)]
		public string MapSectionId { get; set; }

		[DataMember(Order = 2)]
		public string OwnerId { get; set; }

		[DataMember(Order = 3)]
		public JobOwnerType JobOwnerType { get; set; }

		[DataMember(Order = 4)]
		public string SubdivisionId { get; set; }

		[DataMember(Order = 5)]
		public PointInt ScreenPosition { get; set; }

		[DataMember(Order = 6)]
		public BigVectorDto BlockPosition { get; set; }

		[DataMember(Order = 7)]
		public RPointDto Position { get; set; }

		[DataMember(Order = 8)]
		public int Precision { get; set; }

		[DataMember(Order = 9)]
		public SizeInt BlockSize { get; set; }

		[DataMember(Order = 10)]
		public RSizeDto SamplePointDelta { get; set; }

		[DataMember(Order = 11)]
		public MapCalcSettings MapCalcSettings { get; set; }

		//[DataMember(Order = 12)]
		//public MapSectionVectors MapSectionVectors { get; set; }

		[DataMember(Order = 13)]
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

		public DateTime ProcessingStartTime { get; set; }
		public DateTime? ProcessingEndTime { get; set; }

		public TimeSpan? ProcessingDuration => ProcessingEndTime.HasValue ? ProcessingEndTime - ProcessingStartTime : null;

		public bool GetIsDone()
		{
			//if (MapSectionVectors == null)
			//{
			//	return false;
			//}

			// TODO: Implement GetIsDone on the MapSectionRequest class.
			//var result = !HasEscapedFlags.Any(x => !x);
			//return result;

			return false;
		}

		public override string ToString()
		{
			var bVals = BigIntegerHelper.FromLongsDeprecated(BlockPosition.GetValues());
			var bp = new BigVector(bVals);
			return $"S:{SubdivisionId}, BPos:{bp}.";
		}

		//private const int VALUE_FACTOR = 10000;
		//public static int[] CombineCountsAndEscapeVelocities(ushort[] counts, ushort[] escapeVelocities)
		//{
		//	var result = new int[counts.Length];

		//	for(var i = 0; i < counts.Length; i++)
		//	{
		//		result[i] = (counts[i] * VALUE_FACTOR) + escapeVelocities[i];
		//	}

		//	return result;
		//}

		//public static ushort[] SplitCountsAndEscapeVelocities(int[] rawCounts, out ushort[] escapeVelocities)
		//{
		//	var result = new ushort[rawCounts.Length];
		//	escapeVelocities = new ushort[rawCounts.Length];

		//	for (var i = 0; i < rawCounts.Length; i++)
		//	{
		//		result[i] = (ushort)Math.DivRem(rawCounts[i], VALUE_FACTOR, out var ev);
		//		escapeVelocities[i] = (ushort)ev;
		//	}

		//	return result;
		//}

		//public static void SplitCountsAndEscapeVelocities(int[] rawCounts, Span<ushort> counts, Span<ushort> escapeVelocities)
		//{
		//	for (var i = 0; i < rawCounts.Length; i++)
		//	{
		//		counts[i] = (ushort)Math.DivRem(rawCounts[i], VALUE_FACTOR, out var ev);
		//		escapeVelocities[i] = (ushort)ev;
		//	}
		//}

	}

}
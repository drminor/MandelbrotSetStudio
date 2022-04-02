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
		public int[] Counts { get; set; }

		[DataMember(Order = 9)]
		public bool[] DoneFlags { get; set; }

		[DataMember(Order = 10)]
		public double[] ZValues { get; set; }

		public bool IsInverted { get; init; }

		public bool Pending { get; set; }
		public bool Sent { get; set; }
		public bool FoundInRepo { get; set; }
		public bool Completed { get; set; }
		public bool Saved { get; set; }
		public bool Handled { get; set; }

		public bool GetIsDone()
		{
			if (DoneFlags == null)
			{
				return false;
			}

			var result = !DoneFlags.Any(x => !x);
			return result;
		}

		public void UpdateDoneFlags()
		{
			if (Counts == null)
			{
				throw new InvalidOperationException("Cannot UpdateDoneFlags if the Counts is null.");
			}

			if (Counts.Length != BlockSize.NumberOfCells)
			{
				throw new InvalidOperationException("Error while UpdateDoneFlags. The size of the Counts array does not match the BlockSize.");
			}

			int target = MapCalcSettings.TargetIterations;

			DoneFlags = Counts.Select(x => x >= target).ToArray();
		}

		public override string ToString()
		{
			var bVals = BigIntegerHelper.FromLongs(BlockPosition.GetValues());
			var bp = new BigVector(bVals);
			return $"S:{SubdivisionId}, BPos:{bp}.";
		}
	}

}

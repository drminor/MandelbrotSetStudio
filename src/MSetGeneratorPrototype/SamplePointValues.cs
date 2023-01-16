using MSS.Common.APValues;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public ref struct SamplePointValues
	{
		#region Constructor

		public SamplePointValues(FP31Deck crs, FP31Deck cis, FP31Deck zrs, FP31Deck zis, int targetIterations,
			Span<bool> hasEscapedFlags, Memory<int> counts, Span<ushort> escapeVelocities)
		{
			Crs = crs ?? throw new ArgumentNullException(nameof(crs));
			Cis = cis ?? throw new ArgumentNullException(nameof(cis));
			Zrs = zrs ?? throw new ArgumentNullException(nameof(zrs));
			Zis = zis ?? throw new ArgumentNullException(nameof(zis));

			TargetIterations = targetIterations;
			TargetIterationsVector = Vector256.Create(targetIterations);


			// Initially, all vectors are 'In Play.'
			InPlayList = Enumerable.Range(0, crs.VectorCount).ToArray();

			//HasEscapedFlags = hasEscapedFlags;
			Counts = counts;
			//EscapeVelocities = escapeVelocities;

			//InPlayList = BuildTheInplayList(hasEscapedFlags, counts, targetIterations, out bool[] doneFlags);
			//DoneFlags = doneFlags;

			UnusedCalcs = new uint[crs.ValueCount];

			HasEscapedFlagsV = Enumerable.Repeat(Vector256<int>.Zero, crs.VectorCount).ToArray();


			//CountsV = Enumerable.Repeat(Vector256<ushort>.Zero, crs.VectorCount).ToArray();

			CountsV = MemoryMarshal.Cast<int, Vector256<int>>(counts.Span);


			EscapeVelocitiesV = Enumerable.Repeat(Vector256<uint>.Zero, crs.VectorCount).ToArray();

			DoneFlagsV = Enumerable.Repeat(Vector256<int>.Zero, crs.VectorCount).ToArray();
			UnusedCalcsV = Enumerable.Repeat(Vector256<uint>.Zero, crs.VectorCount).ToArray();

			//ZValuesAreZero = Zrs.IsZero || Zis.IsZero;

			//if (ZValuesAreZero)
			//{
			//	Debug.Assert(zrs.IsZero && zis.IsZero, "One of zRs or zIs is zero, but both zRs and zIs are not zero.");
			//}

		}


		//public Span<Vector256<uint>> GetLimbVectorsUW(int limbIndex)
		//{
		//	var x = MantissaMemories[limbIndex];
		//	Span<Vector256<uint>> result = MemoryMarshal.Cast<uint, Vector256<uint>>(x.Span);

		//	return result;
		//}

		#endregion

		#region Public Properties

		public int ValueCount => Crs.ValueCount;
		public int VectorCount => Crs.VectorCount;

		public int[] InPlayList { get; private set; }

		public int TargetIterations { get; init; }

		public FP31Deck Crs { get; init; }
		public FP31Deck Cis { get; init; }
		public FP31Deck Zrs { get; init; }
		public FP31Deck Zis { get; init; }

		//public bool ZValuesAreZero { get; set; }

		//public Span<bool> HasEscapedFlags { get; init; }
		public Memory<int> Counts { get; init; }
		//public Span<ushort> EscapeVelocities { get; init; }

		//public bool[] DoneFlags { get; init; }
		public uint[] UnusedCalcs { get; init; }

		public Vector256<int> TargetIterationsVector { get; init; }

		public Span<Vector256<int>> HasEscapedFlagsV { get; init; }
		public Span<Vector256<int>> CountsV { get; init; }
		public Span<Vector256<uint>> EscapeVelocitiesV { get; init; }

		public Span<Vector256<int>> DoneFlagsV { get; init; }
		public Span<Vector256<uint>> UnusedCalcsV { get; init; }

		#endregion

		#region Public Methods

		public int[] UpdateTheInPlayList(List<int> vectorsNoLongerInPlay)
		{
			var lst = InPlayList.ToList();

			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				lst.Remove(vectorIndex);
			}

			var updatedLst = lst.ToArray();

			InPlayList = updatedLst;

			return updatedLst;
		}

		//public void UpdateTheCounts()
		//{
		//	var lanes = Vector256<uint>.Count;

		//	var vPtr = 0;

		//	for (int i = 0; i < VectorCount; i++)
		//	{
		//		for (int lPtr = 0; lPtr < lanes; lPtr++)
		//		{
		//			Counts[vPtr++] = (ushort) CountsV[i].GetElement(lPtr);
		//		}
		//	}
		//}

		#endregion

		#region Private Methods

		private static int[] BuildTheInplayList(Span<bool> hasEscapedFlags, Span<ushort> counts, int targetIterations, out bool[] doneFlags)
		{
			var lanes = Vector256<uint>.Count;
			var vectorCount = hasEscapedFlags.Length / lanes;

			doneFlags = new bool[hasEscapedFlags.Length];

			for (int i = 0; i < hasEscapedFlags.Length; i++)
			{
				if (hasEscapedFlags[i] | counts[i] >= targetIterations) 
				{
					doneFlags[i] = true;
				}
			}

			var result = Enumerable.Range(0, vectorCount).ToList();

			for (int j = 0; j < vectorCount; j++)
			{
				var arrayPtr = j * lanes;

				var allDone = true;

				for (var lanePtr = 0; lanePtr < lanes; lanePtr++)
				{
					if (!doneFlags[arrayPtr + lanePtr])
					{
						allDone = false;
						break;
					}
				}

				if (allDone)
				{
					result.Remove(j);
				}
			}

			return result.ToArray();
		}

		#endregion
	}
}


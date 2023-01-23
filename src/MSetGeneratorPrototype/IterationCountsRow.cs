using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public ref struct IterationCountsRow
	{
		private readonly MapSectionVectors _mapSectionVectors;

		private List<int> _inPlayBackingList;

		#region Constructor

		public IterationCountsRow(MapSectionVectors mapSectionVectors)
		{
			_mapSectionVectors = mapSectionVectors;
			VectorCount = _mapSectionVectors.VectorsPerRow;

			RowNumber = 0;

			var vector256Ints = Enumerable.Repeat(Vector256<int>.Zero, VectorCount).ToArray();

			HasEscapedFlags = new Span<Vector256<int>>(vector256Ints);
			Counts = new Span<Vector256<int>>(vector256Ints);
			EscapeVelocities = new Span<Vector256<int>>(vector256Ints);

			DoneFlags = new Vector256<int>[VectorCount];
			UnusedCalcs = new Vector256<int>[VectorCount];

			InPlayList = Enumerable.Range(0, VectorCount).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();

			//_unusedCalcsBuffer = new int[Stride];
		}

		//public IterationCountsRow(MapSectionVectors mapSectionVectors, int rowNumber)
		//{
		//	RowNumber = rowNumber;

		//	VectorCount = mapSectionVectors.VectorsPerRow;

		//	HasEscapedFlags = new Span<Vector256<int>>(mapSectionVectors.HasEscapedVectors, rowNumber * VectorCount, VectorCount);
		//	Counts = new Span<Vector256<int>>(mapSectionVectors.CountVectors, rowNumber * VectorCount, VectorCount);
		//	EscapeVelocities = new Span<Vector256<int>>(mapSectionVectors.EscapeVelocityVectors, rowNumber * VectorCount, VectorCount);

		//	DoneFlags = new Vector256<int>[VectorCount];
		//	UnusedCalcs = new Vector256<int>[VectorCount];

		//	InPlayList = Enumerable.Range(0, VectorCount).ToArray();
		//	InPlayListNarrow = BuildNarowInPlayList(InPlayList);

		//	//_unusedCalcsBuffer = new int[Stride];
		//}

		#endregion

		#region Public Properties

		public int RowNumber { get; private set; }

		public int VectorCount { get; init; }

		public Span<Vector256<int>> HasEscapedFlags { get; private set; }
		public Span<Vector256<int>> Counts { get; private set; }
		public Span<Vector256<int>> EscapeVelocities { get; private set; }

		public Vector256<int>[] DoneFlags { get; private set; }
		public Vector256<int>[] UnusedCalcs { get; private set; }

		public int[] InPlayList { get; private set; }
		public int[] InPlayListNarrow { get; private set; }

		#endregion

		#region Public Methods

		public void SetRowNumber(int rowNumber)
		{
			RowNumber = rowNumber;

			HasEscapedFlags = new Span<Vector256<int>>(_mapSectionVectors.HasEscapedVectors, rowNumber * VectorCount, VectorCount);
			Counts = new Span<Vector256<int>>(_mapSectionVectors.CountVectors, rowNumber * VectorCount, VectorCount);
			EscapeVelocities = new Span<Vector256<int>>(_mapSectionVectors.EscapeVelocityVectors, rowNumber * VectorCount, VectorCount);

			Array.Clear(DoneFlags, 0, DoneFlags.Length);
			Array.Clear(UnusedCalcs, 0, UnusedCalcs.Length);

			_inPlayBackingList.Clear();
			for(var i = 0; i < VectorCount; i++)
			{
				_inPlayBackingList.Add(i);
			}

			InPlayList = _inPlayBackingList.ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);
		}

		public long GetTotalUnusedCalcs()
		{
			return 0L;
		}

		#endregion

		#region Private Methods

		public int[] UpdateTheInPlayList(List<int> vectorsNoLongerInPlay)
		{
			foreach (var vectorIndex in vectorsNoLongerInPlay)
			{
				_inPlayBackingList.Remove(vectorIndex);
			}

			InPlayList = _inPlayBackingList.ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			return InPlayList;
		}

		private static int[] BuildNarowInPlayList(int[] inPlayList)
		{
			var result = new int[inPlayList.Length * 2];

			var resultIdxPtr = 0;

			for (var idxPtr = 0; idxPtr < inPlayList.Length; idxPtr++, resultIdxPtr += 2)
			{
				var resultIdx = inPlayList[idxPtr] * 2;

				result[resultIdxPtr] = resultIdx;
				result[resultIdxPtr + 1] = resultIdx + 1;
			}

			return result;
		}

		private int[] BuildTheInplayList(Span<bool> hasEscapedFlags, Span<ushort> counts, int targetIterations, out bool[] doneFlags)
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


using MSS.Common.APValues;
using MSS.Types;
using System.Runtime.Intrinsics;

namespace MSetGeneratorPrototype
{
	public ref struct IterationStateDepthFirst
	{
		private readonly MapSectionVectors _mapSectionVectors;
		private readonly MapSectionZVectors _mapSectionZVectors;

		private List<int> _inPlayBackingList;

		#region Constructor

		public IterationStateDepthFirst(MapSectionVectors mapSectionVectors, MapSectionZVectors mapSectionZVectors)
		{
			_mapSectionVectors = mapSectionVectors;
			_mapSectionZVectors = mapSectionZVectors;

			ValueCount = mapSectionZVectors.ValueCount;
			LimbCount = mapSectionZVectors.LimbCount;
			VectorCount = mapSectionVectors.VectorsPerRow;
			ValuesPerRow = mapSectionVectors.ValuesPerRow;

			RowNumber = -1;

			Crs = new FP31ValArray(LimbCount, ValuesPerRow);
			Cis = new FP31ValArray(LimbCount, ValuesPerRow);
			Zrs = new FP31ValArray(LimbCount, ValuesPerRow);
			Zis = new FP31ValArray(LimbCount, ValuesPerRow);

			ZValuesAreZero = true;


			//HasEscapedFlags = new Vector256<int>[_mapSectionZVectors.ValuesPerRow];
			//_mapSectionZVectors.FillHasEscapedFlagsRow(HasEscapedFlags, 0);

			HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(0);

			Counts = mapSectionVectors.GetCountVectors();
			CountsRow = Counts.Slice(0, VectorCount);

			ZrsRow = _mapSectionZVectors.GetZrsRow(0);
			ZisRow = _mapSectionZVectors.GetZisRow(0);


			DoneFlags = new Vector256<int>[VectorCount];
			UnusedCalcs = new Vector256<int>[VectorCount];

			InPlayList = Enumerable.Range(0, VectorCount).ToArray();
			InPlayListNarrow = BuildNarowInPlayList(InPlayList);

			_inPlayBackingList = InPlayList.ToList();
		}

		#endregion

		#region Public Properties

		public SizeInt BlockSize => _mapSectionVectors.BlockSize;
		public int ValueCount { get; init; }
		public int LimbCount { get; init; }
		public int VectorCount { get; init; }
		public int ValuesPerRow { get; init; }

		public int RowNumber { get; private set; }

		public FP31ValArray Crs { get; set; }
		public FP31ValArray Cis { get; set; }
		public FP31ValArray Zrs { get; set; }
		public FP31ValArray Zis { get; set; }

		public bool ZValuesAreZero { get; set; }

		//public Vector256<int>[] HasEscapedFlags { get; private set; }
		public Span<Vector256<int>> HasEscapedFlagsRow { get; private set; }

		public Span<Vector256<int>> Counts { get; private set; }
		public Span<Vector256<int>> CountsRow { get; private set; }

		public Vector256<int>[] DoneFlags { get; private set; }
		public int[] InPlayList { get; private set; }
		public int[] InPlayListNarrow { get; private set; }

		public Vector256<int>[] UnusedCalcs { get; private set; }

		public Span<Vector256<uint>> ZrsRow { get; private set; }
		public Span<Vector256<uint>> ZisRow { get; private set; }


		#endregion

		#region Public Methods

		public void SetRowNumber(int rowNumber)
		{
			//if (RowNumber != -1)
			//{
			//	UpdateTheHasEscapedFlagsSource(RowNumber);
			//}

			RowNumber = rowNumber;

			//_mapSectionZVectors.FillHasEscapedFlagsRow(HasEscapedFlags, rowNumber);
			HasEscapedFlagsRow = _mapSectionZVectors.GetHasEscapedFlagsRow(rowNumber);

			CountsRow = Counts.Slice(rowNumber * VectorCount, VectorCount);

			ZrsRow = _mapSectionZVectors.GetZrsRow(rowNumber);
			ZisRow = _mapSectionZVectors.GetZisRow(rowNumber);

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


		//public void UpdateTheHasEscapedFlagsSource(int rowNumber)
		//{
		//	_mapSectionZVectors.UpdateHasEscapedFlagsRowFrom(HasEscapedFlags, rowNumber);
		//}

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


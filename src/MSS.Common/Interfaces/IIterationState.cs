using MSS.Types;
using MSS.Types.APValues;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IIterationState
	{
		MapSectionVectors MapSectionVectors { get;  }
		MapSectionZVectors MapSectionZVectors { get; }

		bool IncreasingIterations { get; }
		Vector256<int> TargetIterationsVector { get; }

		int LimbCount { get; init; }
		int ValueCount { get; init; }
		int ValuesPerRow { get; init; }
		int VectorsPerRow { get; init; }
		int VectorsPerZValueRow { get; init; }
		int RowCount { get; init; }

		int? RowNumber { get; }

		bool[] RowHasEscaped { get; init; }
		long[] RowUsedCalcs { get; init; }
		long[] RowUnusedCalcs { get; init; }

		Vector256<int>[] CountsRowV { get; }
		Vector256<int>[] HasEscapedFlagsRowV { get; }
		Vector256<int>[] DoneFlags { get; }

		FP31ValArray CrsRowVArray { get; }
		Vector256<uint>[] CiLimbSet { get; }

		Vector256<uint>[] ZisRowV { get; }
		Vector256<uint>[] ZrsRowV { get; }

		Vector256<int>[] UsedCalcs { get; }
		Vector256<int>[] UnusedCalcs { get; }

		int[] InPlayList { get; }
		int[] InPlayListNarrow { get; }

		int? GetNextRowNumber();
		void SetRowNumber(int rowNumber);
		void FillCiLimbSetForRow(int rowNumber, Vector256<uint>[] limbSet);

		void FillCrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void FillZrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void FillZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet);

		void UpdateZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void UpdateZrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
	}
}
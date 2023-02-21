using MSS.Types.APValues;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IIterationState
	{
		Vector256<uint>[] CiLimbSet { get; }
		Vector256<int>[] CountsRowV { get; }
		FP31ValArray CrsRowVArray { get; }
		Vector256<int>[] DoneFlags { get; }
		Vector256<int>[] HasEscapedFlagsRowV { get; }
		bool IncreasingIterations { get; }
		int[] InPlayList { get; }
		int[] InPlayListNarrow { get; }
		int LimbCount { get; init; }
		int RowCount { get; init; }
		bool[] RowHasEscaped { get; init; }
		int? RowNumber { get; }
		long[] RowUnusedCalcs { get; init; }
		long[] RowUsedCalcs { get; init; }
		Vector256<int> TargetIterationsVector { get; }
		Vector256<int>[] UnusedCalcs { get; }
		Vector256<int>[] UsedCalcs { get; }
		int ValueCount { get; init; }
		int ValuesPerRow { get; init; }
		int VectorsPerRow { get; init; }
		int VectorsPerZValueRow { get; init; }
		Vector256<uint>[] ZisRowV { get; }
		Vector256<uint>[] ZrsRowV { get; }

		void FillCiLimbSetForRow(int rowNumber, Vector256<uint>[] limbSet);
		void FillCrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void FillZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void FillZrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		int? GetNextRowNumber();
		void SetRowNumber(int rowNumber);
		void UpdateZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void UpdateZrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
	}
}
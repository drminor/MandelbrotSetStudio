﻿using MSS.Types;
using MSS.Types.APValues;
using System.Runtime.Intrinsics;

namespace MSS.Common
{
	public interface IIterationState
	{
		//MapSectionVectors MapSectionVectors { get;  }
		//MapSectionZVectors? MapSectionZVectors { get; }

		bool HaveZValues { get; }
		bool IncreasingIterations { get; }
		int TargetIterations { get; }
		Vector256<int> TargetIterationsVector { get; }

		int LimbCount { get; init; }
		int ValueCount { get; init; }
		int ValuesPerRow { get; init; }
		int VectorsPerRow { get; init; }
		int VectorsPerZValueRow { get; init; }
		int RowCount { get; init; }

		int? RowNumber { get; }

		bool[] RowHasEscaped { get; init; }

		Vector256<int>[] CountsRowV { get; }
		public ushort[] EscapeVelocities { get; }

		Vector256<int>[] HasEscapedFlagsRowV { get; }
		Vector256<int>[] DoneFlags { get; }

		FP31ValArray CrsRowVArray { get; }
		Vector256<uint>[] CiLimbSet { get; }

		Vector256<int>[] UsedCalcs { get; }
		Vector256<int>[] UnusedCalcs { get; }

		long[] RowUsedCalcs { get; init; }
		long[] RowUnusedCalcs { get; init; }

		int[] InPlayList { get; }
		int[] InPlayListNarrow { get; }

		int? GetNextRowNumber();
		void SetRowNumber(int rowNumber);

		void FillCrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void FillCiLimbSetForRow(int rowNumber, Vector256<uint>[] limbSet);

		void FillZrLimbSet(int vectorIndex, Vector256<uint>[] limbSet);
		void FillZiLimbSet(int vectorIndex, Vector256<uint>[] limbSet);

		void UpdateZrLimbSet(int rowNumber, int vectorIndex, Vector256<uint>[] limbSet);
		void UpdateZiLimbSet(int rowNumber, int vectorIndex, Vector256<uint>[] limbSet);
	}
}
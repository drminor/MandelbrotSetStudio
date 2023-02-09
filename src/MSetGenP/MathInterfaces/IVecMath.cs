using MSetGenP.Types;
using MSS.Common;
using MSS.Types;


namespace MSetGenP
{
    public interface IVecMath
	{
		bool IsSigned { get; }

		ApFixedPointFormat ApFixedPointFormat { get; init; }
		byte BitsBeforeBP { get; }
		int FractionalBits { get; }
		int LimbCount { get; }
		int TargetExponent { get; }

		int VecCount { get; init; }
		int ValueCount { get; init; }

		int[] InPlayList { get; set; }   // Vector-Level 
		bool[] DoneFlags { get; set; }   // Value-Level	
		BigVector BlockPosition { get; set; }
		int RowNumber { get; set; }

		uint MaxIntegerValue { get; init; }
		uint Threshold { get; init; }
		//double MslWeight { get; init; }
		//Vector256<double> MslWeightVector { get; init; }

		int NumberOfACarries { get; }
		int NumberOfMCarries { get; }
		long NumberOfSplits { get; }
		long NumberOfGetCarries { get; }
		long NumberOfGrtrThanOps { get; }

		void Add(FPValues a, FPValues b, FPValues c);
		void Sub(FPValues a, FPValues b, FPValues c);

		void Square(FPValues a, FPValues result);

		void IsGreaterOrEqThanThreshold(FPValues a, bool[] results);

		Smx GetSmxAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION);

		Smx2C GetSmx2CAtIndex(FPValues fPValues, int index, int precision = RMapConstants.DEFAULT_PRECISION);

	}
}
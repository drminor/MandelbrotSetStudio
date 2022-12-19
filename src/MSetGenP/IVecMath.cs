using System.Runtime.Intrinsics;

namespace MSetGenP
{
	public interface IVecMath
	{
		int[] InPlayList { get; set; }
		bool[] DoneFlags { get; set; }

		int VecCount { get; init; }
		int ValueCount { get; init; }

		ApFixedPointFormat ApFixedPointFormat { get; init; }

		byte BitsBeforeBP { get; }
		int FractionalBits { get; }

		int TargetExponent { get; init; }
		int LimbCount { get; init; }

		uint MaxIntegerValue { get; init; }
		uint Threshold { get; init; }
		double MslWeight { get; init; }
		Vector256<double> MslWeightVector { get; init; }

		int NumberOfACarries { get; }
		int NumberOfMCarries { get; }
		long NumberOfSplits { get; }
		long NumberOfGetCarries { get; }
		long NumberOfGrtrThanOps { get; }

		void Add(FPValues a, FPValues b, FPValues c);
		void Sub(FPValues a, FPValues b, FPValues c);

		void Square(FPValues a, FPValues result);

		void IsGreaterOrEqThanThreshold(FPValues a, bool[] results);

		Smx GetSmxAtIndex(FPValues fPValues, int index, int precision = 53);


	}
}
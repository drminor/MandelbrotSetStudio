using MSS.Common;
using MSS.Types;

namespace MSetGenP
{
	public interface IScalerMath2C
	{
		bool IsSigned { get; }

		ApFixedPointFormat ApFixedPointFormat { get; init; }
		byte BitsBeforeBP { get; }
		int FractionalBits { get; }
		int LimbCount { get; }
		int TargetExponent { get; }

		uint MaxIntegerValue { get; init; }
		uint Threshold { get; init; }
		ulong ThresholdMsl { get; init; }

		int NumberOfACarries { get; }
		int NumberOfMCarries { get; }
		int NumberOfSplits { get; }
		long NumberOfGetCarries { get; }
		long NumberOfGrtrThanOps { get; }

		Smx2C Add(Smx2C a, Smx2C b, string desc);
		Smx2C Sub(Smx2C a, Smx2C b, string desc);

		Smx2C Multiply(Smx2C a, int b);
		Smx2C Multiply(Smx2C a, Smx2C b);
		Smx2C Square(Smx2C a);

		bool IsGreaterOrEqThanThreshold(Smx2C a);

		Smx2C CreateNewMaxIntegerSmx2C(int precision = RMapConstants.DEFAULT_PRECISION);
		Smx2C CreateNewZeroSmx2C(int precision = RMapConstants.DEFAULT_PRECISION);

		//Smx2C CreateSmx2C(RValue rValue);

	}
}
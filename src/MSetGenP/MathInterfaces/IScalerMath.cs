using MSS.Common;
using MSS.Common.APValues;
using MSS.Types;

namespace MSetGenP
{
    public interface IScalerMath
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

		Smx Add(Smx a, Smx b, string desc);
		Smx Sub(Smx a, Smx b, string desc);

		Smx Multiply(Smx a, int b);
		Smx Multiply(Smx a, Smx b);
		Smx Square(Smx a);

		bool IsGreaterOrEqThanThreshold(Smx a);

		Smx CreateNewMaxIntegerSmx(int precision = RMapConstants.DEFAULT_PRECISION);
		Smx CreateNewZeroSmx(int precision = RMapConstants.DEFAULT_PRECISION);
		Smx CreateSmx(RValue rValue);


	}
}
using MSS.Types;

namespace MSetGenP
{
	public interface IScalerMath2C
	{
		ApFixedPointFormat ApFixedPointFormat { get; init; }

		byte BitsBeforeBP { get; }
		int FractionalBits { get; }

		int TargetExponent { get; init; }
		int LimbCount { get; init; }

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

		Smx2C CreateNewMaxIntegerSmx2C(int precision = 53);
		Smx2C CreateNewZeroSmx2C(int precision = 53);
		Smx2C CreateSmx2C(RValue rValue);

		Smx2C Convert(Smx smx, bool overrideFormatChecks = false);
		Smx Convert(Smx2C smx2C);

	}
}
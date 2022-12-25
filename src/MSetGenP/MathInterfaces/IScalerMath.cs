using MSS.Types;

namespace MSetGenP
{
	public interface IScalerMath
	{
		ApFixedPointFormat ApFixedPointFormat { get; init; }

		bool IsSigned { get; }

		byte BitsBeforeBP { get; }
		int FractionalBits { get; }

		int TargetExponent { get; }
		int LimbCount { get; }

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

		Smx CreateNewMaxIntegerSmx(int precision = 53);
		Smx CreateNewZeroSmx(int precision = 53);
		Smx CreateSmx(RValue rValue);

		Smx2C Convert(Smx smx, bool overrideFormatChecks = false);
		Smx Convert(Smx2C smx2C);


	}
}
using System;

namespace MSS.Types
{
	public interface IColorMapEntry : ICloneable
	{
		ColorBandBlendStyle BlendStyle { get; init; }
		int BucketWidth { get; init; }
		byte[]? Cache { get; set; }
		int Cutoff { get; init; }
		ColorBandColor EndColor { get; init; }
		ColorBandColor StartColor { get; init; }
		int StartingCutoff { get; init; }
		double StepAmount { get; init; }
		bool UsingEscapeVelocities { get; init; }

		int BlendAndPlace(double factor, Span<byte> destination);
		string? ReportBlendVals();
	}
}
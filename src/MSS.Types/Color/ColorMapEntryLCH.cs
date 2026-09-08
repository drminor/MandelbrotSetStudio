using System;
using System.Numerics;

namespace MSS.Types
{
	public class ColorMapEntryLCH : IColorMapEntry
	{
		private const int BYTES_PER_PIXEL = 4;

		private readonly BlendValsLCH _blendVals;

		#region Constructor

		public ColorMapEntryLCH(ColorBand cb, bool useEscapeVelocities)
			: this(cb.Cutoff, cb.StartColor, cb.BlendStyle, cb.ActualEndColor, cb.PreviousCutoff, cb.BucketWidth, useEscapeVelocities)
		{ }

		public ColorMapEntryLCH(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor,
			int? previousCutoff, int bucketWidth, bool useEscapeVelocities)
		{
			Cutoff = cutoff;
			StartColor = startColor;
			BlendStyle = blendStyle;
			EndColor = endColor;
			StartingCutoff = (previousCutoff ?? 0) + 1;
			BucketWidth = useEscapeVelocities ? bucketWidth + 1 : bucketWidth;
			UsingEscapeVelocities = useEscapeVelocities;
			StepAmount = 1d / BucketWidth;

			if (blendStyle == ColorBandBlendStyle.None)
			{
				_blendVals = new BlendValsLCH();
			}
			else
			{
				var startingLch = ColorHelper.GetLch(StartColor.ColorComps);
				var endingLch = ColorHelper.GetLch(EndColor.ColorComps);

				_blendVals = new BlendValsLCH(startingLch, endingLch);
			}

			if (!useEscapeVelocities && BucketWidth < 501)
			{
				Cache = new byte[BucketWidth * BYTES_PER_PIXEL];
				Array.Clear(Cache);
			}
			else
			{
				Cache = null;
			}
		}

		#endregion

		#region Public Properties

		public int Cutoff { get; init; }

		public ColorBandColor StartColor { get; init; }
		public ColorBandBlendStyle BlendStyle { get; init; }
		public ColorBandBlendMethod BlendMethod { get; init; }
		public ColorBandColor EndColor { get; init; }

		public int StartingCutoff { get; init; }
		public int BucketWidth { get; init; }
		public double StepAmount { get; init; }
		public bool UsingEscapeVelocities { get; init; }

		public byte[]? Cache { get; set; }

		#endregion

		public int BlendAndPlace(double factor, Span<byte> destination)
		{
			var lch = _blendVals.Blend(factor);

			var errors = ColorHelper.PlaceLch(lch, destination);

			return errors;
		}

		public double[] GetBlendedVals(double factor)
		{
			var lch = _blendVals.Blend(factor);

			return lch.GetComps();
		}

		public string? ReportBlendVals()
		{
			return _blendVals.ToString();
		}

		public override string? ToString()
		{
			return $"Starting Cutoff: {StartingCutoff}, Ending Cutoff: {Cutoff}, Start: {StartColor.GetCssColor()}, Blend: {BlendStyle}, End: {EndColor.GetCssColor()}.";
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public ColorMapEntryLCH Clone()
		{
			return new ColorMapEntryLCH(Cutoff, StartColor, BlendStyle, EndColor, StartingCutoff, BucketWidth, UsingEscapeVelocities);
		}


	}
}

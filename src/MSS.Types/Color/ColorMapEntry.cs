using System;

namespace MSS.Types
{
	internal class ColorMapEntry : IColorMapEntry
	{
		private const int BYTES_PER_PIXEL = 4;

		private readonly BlendVals _blendVals;

		#region Constructor

		public ColorMapEntry(ColorBand cb, bool useEscapeVelocities)
			: this(cb.Cutoff, cb.StartColor, cb.BlendStyle, cb.ActualEndColor, cb.PreviousCutoff, cb.BucketWidth, useEscapeVelocities)
		{ }

		public ColorMapEntry(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor,
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

			if (BlendStyle == ColorBandBlendStyle.None)
			{
				_blendVals = new BlendVals();
			}
			else
			{
				_blendVals = new BlendVals(StartColor.ColorComps, EndColor.ColorComps);
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

			//Cache = null;
		}

		#endregion

		#region Public Properties

		public int Cutoff { get; init; }

		public ColorBandColor StartColor { get; init; }
		public ColorBandBlendStyle BlendStyle { get; init; }
		public ColorBandColor EndColor { get; init; }

		public int StartingCutoff { get; init; }
		public int BucketWidth { get; init; }
		public double StepAmount { get; init; }
		public bool UsingEscapeVelocities { get; init; }

		public byte[]? Cache { get; set; }

		#endregion

		public int BlendAndPlace(double factor, Span<byte> destination)
		{
			return _blendVals.BlendAndPlace(factor, destination);
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

		public ColorMapEntry Clone()
		{
			return new ColorMapEntry(Cutoff, StartColor, BlendStyle, EndColor, StartingCutoff, BucketWidth, UsingEscapeVelocities);
		}
	}

}

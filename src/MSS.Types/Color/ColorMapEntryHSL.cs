using System;
using System.Drawing;

namespace MSS.Types
{
	internal class ColorMapEntryHSL : IColorMapEntry
	{
		private const int BYTES_PER_PIXEL = 4;

		private readonly BlendValsHSL _blendVals;

		#region Constructor

		public ColorMapEntryHSL(ColorBand cb, bool useEscapeVelocities)
			: this(cb.Cutoff, cb.StartColor, cb.BlendStyle, cb.ActualEndColor, cb.PreviousCutoff, cb.BucketWidth, useEscapeVelocities)
		{ }

		public ColorMapEntryHSL(int cutoff, ColorBandColor startColor, ColorBandBlendStyle blendStyle, ColorBandColor endColor,
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
				_blendVals = new BlendValsHSL();
			}
			else if (BlendStyle == ColorBandBlendStyle.End || BlendStyle == ColorBandBlendStyle.Next)
			{
				var startingHsl = GetHSL(StartColor.ColorComps);
				var endingHsl = GetHSL(EndColor.ColorComps);

				_blendVals = new BlendValsHSL(startingHsl, endingHsl, ColorExtensions.Direction.Clockwise);
			}
			else
			{
				var startingHsl = GetHSL(StartColor.ColorComps);
				var endingHsl = GetHSL(EndColor.ColorComps);

				_blendVals = new BlendValsHSL(startingHsl, endingHsl, ColorExtensions.Direction.CounterClockwise);
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
			var hsl = _blendVals.Blend(factor, out var errors);

			PlaceRgb(hsl, destination);

			return errors;
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

		public ColorMapEntryHSL Clone()
		{
			return new ColorMapEntryHSL(Cutoff, StartColor, BlendStyle, EndColor, StartingCutoff, BucketWidth, UsingEscapeVelocities);
		}

		private double[] GetHSL(byte[] cc)
		{
			var color = Color.FromArgb(cc[0], cc[1], cc[2]);

			var hue = color.GetHue();
			var saturation = color.GetSaturation();
			var brightness = color.GetBrightness();

			return new double[] { hue, saturation, brightness };
		}

		private void PlaceRgb(double[] hsl, Span<byte> destination)
		{
			var color = FromAhsb(255, Convert.ToSingle(hsl[0]), Convert.ToSingle(hsl[1]), Convert.ToSingle(hsl[2]));

			destination[0] = color.B;
			destination[1] = color.G;
			destination[2] = color.R;
			destination[3] = color.A;
		}

		private Color FromAhsb(int alpha, float hue, float saturation, float brightness)
		{
			if (0 > alpha
				|| 255 < alpha)
			{
				throw new ArgumentOutOfRangeException(
					"alpha",
					alpha,
					"Value must be within a range of 0 - 255.");
			}

			if (0f > hue
				|| 360f < hue)
			{
				throw new ArgumentOutOfRangeException(
					"hue",
					hue,
					"Value must be within a range of 0 - 360.");
			}

			if (0f > saturation
				|| 1f < saturation)
			{
				throw new ArgumentOutOfRangeException(
					"saturation",
					saturation,
					"Value must be within a range of 0 - 1.");
			}

			if (0f > brightness
				|| 1f < brightness)
			{
				throw new ArgumentOutOfRangeException(
					"brightness",
					brightness,
					"Value must be within a range of 0 - 1.");
			}

			if (0 == saturation)
			{
				return Color.FromArgb(
									alpha,
									Convert.ToInt32(brightness * 255),
									Convert.ToInt32(brightness * 255),
									Convert.ToInt32(brightness * 255));
			}

			float fMax, fMid, fMin;
			int iSextant, iMax, iMid, iMin;

			if (0.5 < brightness)
			{
				fMax = brightness - (brightness * saturation) + saturation;
				fMin = brightness + (brightness * saturation) - saturation;
			}
			else
			{
				fMax = brightness + (brightness * saturation);
				fMin = brightness - (brightness * saturation);
			}

			iSextant = (int)Math.Floor(hue / 60f);
			if (300f <= hue)
			{
				hue -= 360f;
			}

			hue /= 60f;
			hue -= 2f * (float)Math.Floor((iSextant + 1f) % 6f / 2f);
			if (0 == iSextant % 2)
			{
				fMid = (hue * (fMax - fMin)) + fMin;
			}
			else
			{
				fMid = fMin - (hue * (fMax - fMin));
			}

			iMax = Convert.ToInt32(fMax * 255);
			iMid = Convert.ToInt32(fMid * 255);
			iMin = Convert.ToInt32(fMin * 255);

			switch (iSextant)
			{
				case 1:
					return Color.FromArgb(alpha, iMid, iMax, iMin);
				case 2:
					return Color.FromArgb(alpha, iMin, iMax, iMid);
				case 3:
					return Color.FromArgb(alpha, iMin, iMid, iMax);
				case 4:
					return Color.FromArgb(alpha, iMid, iMin, iMax);
				case 5:
					return Color.FromArgb(alpha, iMax, iMin, iMid);
				default:
					return Color.FromArgb(alpha, iMax, iMid, iMin);
			}
		}
	}
}

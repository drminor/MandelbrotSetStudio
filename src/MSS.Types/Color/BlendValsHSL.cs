using System;

namespace MSS.Types
{
	public struct BlendValsHSL 
	{
		//private readonly double[] _startHsl;
		private readonly double[] _endHsl;

		public double SHue { get; init; }
		public double SSaturation { get; init; }
		public double SLuminance { get; init; }

		public double DiffHue { get; init; }
		public double DiffSaturation { get; init; }
		public double DiffLuminance { get; init; }

		public ColorExtensions.Direction Direction { get; init; }

		public BlendValsHSL()
		{
			SHue = 0;
			SSaturation = 0;
			SLuminance = 0;

			DiffHue = 0;
			DiffSaturation = 0;
			DiffLuminance = 0;

			//_startHsl = new double[] { 0, 0, 0 };
			_endHsl = new double[] { 0, 0, 0 };

			Direction = ColorExtensions.Direction.Clockwise;
		}

		public BlendValsHSL(double[] startHsl, double[] endHsl, ColorExtensions.Direction direction = ColorExtensions.Direction.Clockwise)
		{
			SHue = startHsl[0];
			SSaturation = startHsl[1];
			SLuminance = startHsl[2];

			DiffHue = endHsl[0] - startHsl[0];
			DiffSaturation = endHsl[1] - startHsl[1];
			DiffLuminance = endHsl[2] - startHsl[2];

			//_startHsl = startHsl;
			_endHsl = endHsl;

			Direction = direction;

			switch (direction)
			{
				case ColorExtensions.Direction.CounterClockwise:
					if (DiffHue >= 0)
						DiffHue = (360 - DiffHue) * -1;
					break;

				default:
					if (DiffHue <= 0)
						DiffHue = 360 + DiffHue;
					break;
			}
		}


		public double[] Blend(double factor, out int errors)
		{
			errors = 0;

			var h = factor * DiffHue + SHue;
			var s = factor * DiffSaturation + SSaturation;
			var l = factor * DiffLuminance + SLuminance;

			if (h < 0)
			{
				h += 360;
			}
			else
			{
				if (h > 360)
				{
					h -= 360;
				}
			}

			//if (s < 0 || s > 255)
			//{
			//	Debug.WriteLine($"Bad green value. sf: {factor}, st: {SGreen}, en: {EGreen}.");
			//	s = 50;
			//	errors++;
			//}

			//if (l < 0 || l > 255)
			//{
			//	Debug.WriteLine($"Bad blue value. sf: {factor}, st: {SBlue}, en: {EBlue}.");
			//	l = 50;
			//	errors++;
			//}

			if (s < 0)
			{
				s = 0;
			}
			else
			{
				if (s > 1)
				{
					s = 1;
				}
			}

			if (l < 0)
			{
				l = 0;
			}
			else
			{
				if (l > 1)
				{
					l = 1;
				}
			}

			return new double[3] { h, s, l };
		}

		public override string? ToString()
		{
			var result = $"BlendVal (Ending, Starting Diff) Hue: {_endHsl[0]}, {SHue}, {DiffHue}\tSaturation: {_endHsl[1]}, {SSaturation}, {DiffSaturation}\tLuminance: {_endHsl[2]}, {SLuminance}, {DiffLuminance}";

			return result;
		}
	}


}

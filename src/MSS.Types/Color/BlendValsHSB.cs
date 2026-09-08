namespace MSS.Types
{
	public struct BlendValsHSB 
	{
		private readonly double[] _endHsb;

		public double SHue { get; init; }
		public double SSaturation { get; init; }
		public double SBrightness { get; init; }

		public double DiffHue { get; init; }
		public double DiffSaturation { get; init; }
		public double DiffBrightness { get; init; }

		//public HsbBlendDirection Direction { get; init; }

		public BlendValsHSB()
		{
			SHue = 0;
			SSaturation = 0;
			SBrightness = 0;

			DiffHue = 0;
			DiffSaturation = 0;
			DiffBrightness = 0;

			_endHsb = new double[] { 0, 0, 0 };

			//Direction = HsbBlendDirection.Clockwise;
		}

		public BlendValsHSB(double[] startHsb, double[] endHsb/*, HsbBlendDirection direction = HsbBlendDirection.Clockwise*/)
		{
			SHue = startHsb[0];
			SSaturation = startHsb[1];
			SBrightness = startHsb[2];

			DiffHue = endHsb[0] - startHsb[0];
			DiffSaturation = endHsb[1] - startHsb[1];
			DiffBrightness = endHsb[2] - startHsb[2];

			//_startHsb = startHsb;
			_endHsb = endHsb;

			//Direction = direction;

			//switch (direction)
			//{
			//	case HsbBlendDirection.CounterClockwise:
			//		if (DiffHue >= 0)
			//			DiffHue = (360 - DiffHue) * -1;
			//		break;

			//	default:
			//		if (DiffHue <= 0)
			//			DiffHue = 360 + DiffHue;
			//		break;
			//}
		}


		public double[] Blend(double factor, out int errors)
		{
			errors = 0;

			var h = factor * DiffHue + SHue;
			var s = factor * DiffSaturation + SSaturation;
			var b = factor * DiffBrightness + SBrightness;

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

			if (b < 0)
			{
				b = 0;
			}
			else
			{
				if (b > 1)
				{
					b = 1;
				}
			}

			return new double[3] { h, s, b };
		}

		public override string? ToString()
		{
			var result = $"BlendVal (Ending, Starting, Diff) Hue: {_endHsb[0]}, {SHue}, {DiffHue}\tSaturation: {_endHsb[1]}, {SSaturation}, {DiffSaturation}\tBrightness: {_endHsb[2]}, {SBrightness}, {DiffBrightness}";

			return result;
		}
	}


}

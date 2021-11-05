using System;

namespace RussianBoxes
{
	static class RuAreaHelper
	{
		static RuArea[] SubDivide(RuArea ruArea)
		{
			if (ruArea.IsSubdivided)
			{
				throw new InvalidOperationException("The RuArea is already subdivided.");
			}

			RuArea[] result = new RuArea[4];

			double hMidpoint = GetMidpoint(ruArea.Sx, ruArea.Ex);
			double vMidpoint = GetMidpoint(ruArea.Sy, ruArea.Ey);

			// Lower Left
			result[0] = new RuArea(ruArea.Sx, hMidpoint, ruArea.Sy, vMidpoint);

			// Upper Left
			result[1] = new RuArea(ruArea.Sx, hMidpoint, vMidpoint, ruArea.Ey);

			// Lower Right
			result[2] = new RuArea(hMidpoint, ruArea.Ex, ruArea.Sy, vMidpoint);

			// Upper Right
			result[3] = new RuArea(hMidpoint, ruArea.Ex, vMidpoint, ruArea.Ey);

			return result;
		}

		static double GetMidpoint(double x1, double x2)
		{
			double result = (x2 - x1) / 2;

			return result;
		}
	}
}

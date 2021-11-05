using System;

namespace RussianBoxes
{
	class RuArea : ICloneable
	{
		public double Sx { get; init; }
		public double Ex { get; init; }
		public double Sy { get; init; }
		public double Ey { get; init; }

		public RuArea[] SubDivisons { get; init; }

		public bool IsSubdivided => SubDivisons != null; public RuArea(double sx, double ex, double sy, double ey)
		{
			Sx = sx;
			Ex = ex;
			Sy = sy;
			Ey = ey;

			SubDivisons = null;
		}

		public RuArea(double sx, double ex, double sy, double ey, RuArea[] subDivisions)
		{
			Sx = sx;
			Ex = ex;
			Sy = sy;
			Ey = ey;

			SubDivisons = new RuArea[] { subDivisions[0], subDivisions[1], subDivisions[2], subDivisions[3] };
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public RuArea Clone()
		{
			RuArea result;

			if (IsSubdivided)
			{
				result = new RuArea(Sx, Ex, Sy, Ey, new RuArea[] { SubDivisons[0].Clone(), SubDivisons[1].Clone(), SubDivisons[2].Clone(), SubDivisons[3].Clone()});
			}
			else
			{
				result = new RuArea(Sx, Ex, Sy, Ey);
			}

			return result;
		}
	}
}

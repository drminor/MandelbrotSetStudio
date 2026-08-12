
namespace MSS.Types.PColor
{
	public class Lch
	{
		public double L { get; init; }
		public double C { get; init; }
		public double H { get; init; }

		public Lch()
		{
			L = 0;
			C = 0;
			H = 0;
		}

		public Lch(double l, double c, double h)
		{
			L = l;
			C = c;
			H = h;
		}

	}
}

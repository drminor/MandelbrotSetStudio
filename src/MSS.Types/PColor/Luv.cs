namespace MSS.Types.PColor
{
	public class Luv
	{
		public double L { get; init; }
		public double U { get; init; }
		public double V { get; init; }

		public Luv()
		{
			L = 0;
			U = 0;
			V = 0;
		}

		public Luv(double l, double u, double v)
		{
			L = l;
			U = u;
			V = v;
		}

	}
}

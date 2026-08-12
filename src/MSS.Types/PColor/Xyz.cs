
namespace MSS.Types.PColor
{
	public class Xyz
	{
		public double X { get; init; }
		public double Y { get; init; }
		public double Z { get; init; }

		public Xyz()
		{
			X = 0;
			Y = 0;
			Z = 0;
		}

		public Xyz(double x, double y, double z)
		{
			X = x;
			Y = y;
			Z = z;
		}

	}
}



namespace MSS.Types
{
	public class DPoint
	{
		public DPoint() : this(0, 0) { }

		public DPoint(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double X { get; set; }
		public double Y { get; set; }
	}
}

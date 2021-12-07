using MSS.Types.Base;

namespace MSS.Types
{
	public class DPoint : Point<double>
	{
		public DPoint() : this(0, 0)
		{ }

		public DPoint(double[] values) : base(values)
		{ }

		public DPoint(double x, double y) : base(x, y)
		{
		}
	}
}

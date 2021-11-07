
using MSS.Types.Base;
using System;
using System.Linq;

namespace MSS.Types
{
	public class DRectangle : Rectangle<double>
	{
		public DRectangle(double[] values) : base(values)
		{ }

		public DRectangle(long[] values) : base(values.Select(x => Convert.ToDouble(x)).ToArray())
		{ }

		public DRectangle(double x1, double x2, double y1, double y2) : base(x1, x2, y1, y2)
		{ }

		public double Width => X2 - X1;

		public double Height => Y2 - Y1;

		public DSize Size => new DSize(Width, Height);

		// TODO: Catch overflow exceptions
		public DRectangle Scale(double factor)
		{
			return new DRectangle(Values.Select(x => x * factor).ToArray());
		}
	}
}

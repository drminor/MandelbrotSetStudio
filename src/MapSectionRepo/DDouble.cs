namespace MapSectionRepo
{
	public class DDouble
	{
		public double Hi;

		public double Lo;

		private DDouble()
		{
			Hi = 0;
			Lo = 0;
		}

		public DDouble(double hi, double lo)
		{
			Hi = hi;
			Lo = lo;
		}

		public DDouble(DDouble val)
		{
			Hi = val.Hi;
			Lo = val.Lo;
		}

		public void CopyFrom(DDouble val)
		{
			Hi = val.Hi;
			Lo = val.Lo;
		}

		public DDouble Scale(double f)
		{
			Hi *= f;
			Lo *= f;
			return this;
		}

		public DDouble Trans(double f)
		{
			Hi += f;
			Lo += f;
			return this;
		}

		public DDouble Scale(DDouble f)
		{
			double t = Hi * f.Hi - Lo * f.Lo;
			Lo = Hi * f.Lo + Lo * f.Hi;
			Hi = t;
			return this;
		}

		//=> new DPoint(a.X * b.X - a.Y * b.Y, a.X * b.Y + a.Y * b.X);


		public DDouble Trans(DDouble f)
		{
			Hi += f.Hi;
			Lo += f.Lo;
			return this;
		}

		// TODO: Implement a converter from SPoint to DPoint
		//public static bool TryGetFromSPoint(SPoint sPoint, out DPoint dPoint)
		//{
		//	if(double.TryParse(sPoint.X, out double x))
		//	{
		//		if(double.TryParse(sPoint.Y, out double y))
		//		{
		//			dPoint = new DPoint(x, y);
		//			return true;
		//		}
		//		else
		//		{
		//			dPoint = new DPoint();
		//			return false;
		//		}
		//	}
		//	else
		//	{
		//		dPoint = new DPoint();
		//		return false;
		//	}
		//}

		public double SizeSquared
		{
			get
			{
				return Hi * Hi + Lo * Lo;
			}
		}

		//public static DDouble operator -(DDouble a, DDouble b)
		//	=> new(a.X - b.X, a.Y - b.Y);

		//public static DDouble operator +(DDouble a, DDouble b)
		//	=> new(a.X + b.X, a.Y + b.Y);

		//public static DDouble operator *(DDouble a, DDouble b)
		//	=> new((a.X * b.X) - (a.Y * b.Y), (a.X * b.Y) + (a.Y * b.X));

	}
}

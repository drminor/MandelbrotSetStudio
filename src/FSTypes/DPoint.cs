namespace FSTypes
{
	public class DPoint
    {
        public double X;

        public double Y;

        private DPoint()
        {
            X = 0;
            Y = 0;
        }

        public DPoint(double x, double y)
        {
            X = x;
            Y = y;
        }

		public DPoint(DPoint val)
		{
			X = val.X;
			Y = val.Y;
		}

		public void CopyFrom(DPoint val)
		{
			X = val.X;
			Y = val.Y;
		}

		public DPoint Scale(double f)
		{
			X *= f;
			Y *= f;
			return this;
		}

		public DPoint Trans(double f)
		{
			X += f;
			Y += f;
			return this;
		}

		public DPoint Scale(DPoint f)
		{
			double t = X * f.X - Y * f.Y;
			Y = X * f.Y + Y * f.X;
			X = t;
			return this;
		}

		//=> new DPoint(a.X * b.X - a.Y * b.Y, a.X * b.Y + a.Y * b.X);


		public DPoint Trans(DPoint f)
		{
			X += f.X;
			Y += f.Y;
			return this;
		}

		public static bool TryGetFromSPoint(SPoint sPoint, out DPoint dPoint)
		{
			if(double.TryParse(sPoint.X, out double x))
			{
				if(double.TryParse(sPoint.Y, out double y))
				{
					dPoint = new DPoint(x, y);
					return true;
				}
				else
				{
					dPoint = new DPoint();
					return false;
				}
			}
			else
			{
				dPoint = new DPoint();
				return false;
			}
		}

        public double SizeSquared
        {
            get
            {
                return X * X + Y * Y;
            }
        }

		public static DPoint operator -(DPoint a, DPoint b)
			=> new DPoint(a.X - b.X, a.Y - b.Y);

		public static DPoint operator +(DPoint a, DPoint b)
			=> new DPoint(a.X + b.X, a.Y + b.Y);

		public static DPoint operator *(DPoint a, DPoint b)
			=> new DPoint(a.X * b.X - a.Y * b.Y, a.X * b.Y + a.Y * b.X);

	}
}

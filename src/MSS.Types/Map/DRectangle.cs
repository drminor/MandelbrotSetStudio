
namespace MSS.Types
{
	public class DRectangle
	{
		public double Sx { get; init; }
		public double Ex { get; init; }
		public double Sy { get; init; }
		public double Ey { get; init; }

		public DRectangle(double sx, double ex, double sy, double ey)
		{
			Sx = sx;
			Ex = ex;
			Sy = sy;
			Ey = ey;
		}

		public double Width => Ex - Sx;

		public double Height => Ey - Sy;

		public DPoint BotLeft => new DPoint(Sx, Sy);

		public DPoint TopRight => new DPoint(Ex, Ey);

		public DSize Size => new DSize(Width, Height);
	}
}

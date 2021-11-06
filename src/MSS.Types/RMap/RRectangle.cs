
namespace MSS.Types
{
	public class RRectangle
	{
		public long SxN { get; init; }
		public long ExN { get; init; }
		public long SyN { get; init; }
		public long EyN { get; init; }

		public int Exp { get; init; }

		public RRectangle(long sxN, long exN, long syN, long eyN, int exp)
		{
			SxN = sxN;
			ExN = exN;
			SyN = syN;
			EyN = eyN;
			Exp = exp;
		}

		public RPoint LeftBot => new RPoint(SxN, SyN, Exp);

		public RPoint RightTop => new RPoint(ExN, EyN, Exp);

		public RSize Size => new RSize(ExN - SxN, EyN - SyN, Exp);
	}
}

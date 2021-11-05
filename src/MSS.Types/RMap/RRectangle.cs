
namespace MSS.Types
{
	public class RRectangle
	{
		public long SxN { get; init; }
		public long ExN { get; init; }
		public long SyN { get; init; }
		public long EyN { get; init; }

		public uint Exp { get; init; }

		public RRectangle(long sxN, long exN, long syN, long eyN, uint exp)
		{
			SxN = sxN;
			ExN = exN;
			SyN = syN;
			EyN = eyN;
			Exp = exp;
		}

		public RPoint BotLeft => new RPoint(SxN, ExN, Exp);

		public RPoint TopRight => new RPoint(SyN, EyN, Exp);

		public RSize Size => new RSize(ExN - SxN, EyN - SyN, Exp);
	}
}

using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class RSize : Size<BigInteger>
	{
		public int Exp { get; init; }

		public RSize() : this(0, 0, 0) { }

		public RSize(BigInteger width, BigInteger height, int exp) : base(width, height)
		{
			Exp = exp;
		}
	}
}

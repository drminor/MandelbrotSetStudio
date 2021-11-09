using MSS.Types.Base;
using System.Numerics;

namespace MSS.Types
{
	public class BSize : Size<BigInteger>
	{
		public int Exp { get; init; }

		public BSize() : this(0, 0, 0) { }

		public BSize(BigInteger width, BigInteger height, int exp) : base(width, height)
		{
			Exp = exp;
		}
	}
}

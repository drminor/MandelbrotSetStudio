using MSS.Types.Base;

namespace MSS.Types
{
	public class RSize : Size<long>
	{
		public int Exp { get; init; }

		public RSize() : this(0, 0, 0) { }

		public RSize(long width, long height, int exp) : base(width, height)
		{
			Exp = exp;
		}
	}
}

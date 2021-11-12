using MSS.Types;

namespace MSS.Common
{
	public class RMapConstants
	{
		public static readonly RRectangle ENTIRE_SET_RECTANGLE;

		public static readonly SizeInt BLOCK_SIZE;

		static RMapConstants()
		{
			// The set goes from x = -2 to 1 and from y = -1 to 1.
			// Setting the exponent to -1 (i.e, using a factor of 1/2) this is
			// x = -4/2 to 2/2 and y = -2/2 to 2/2

			// Setting the exponent to 0 (i.e, using a factor of 1) this is
			// x = -2/1 to 1/1 and y = -1/1 to 1/1

			ENTIRE_SET_RECTANGLE = new RRectangle(-2, 1, -1, 1, 0);

			BLOCK_SIZE = new SizeInt(128, 128);

		}
	}
}

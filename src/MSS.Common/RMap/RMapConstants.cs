using MSS.Types;

namespace MSS.Common
{
	public class RMapConstants
	{
		public static readonly RRectangle ENTIRE_SET_RECTANGLE;

		static RMapConstants()
		{
			// The set goes from x = -2 to 1 and from y = -1 to 1.
			// Setting the exponent to -1 (i.e, using a factor or 1/2) this is
			// x = -4/2 to 2/2 and y = -2/2 to 2/2

			ENTIRE_SET_RECTANGLE = new RRectangle(-4, 2, -2, 2, -1);

		}
	}
}

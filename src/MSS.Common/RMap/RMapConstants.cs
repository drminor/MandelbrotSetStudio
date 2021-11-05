using MSS.Types;

namespace MSS.Common
{
	public class RMapConstants
	{
		static readonly RRectangle ENTIRE_SET_RECTANGLE;

		static RMapConstants()
		{
			// The set goes from x = -2 to 1 and from y = -1 to 1.
			// At Zooms = 1, this is
			// -4/2, 2/2, -2/2, 2/2

			ENTIRE_SET_RECTANGLE = new RRectangle(-4, 2, -2, 2, 1);

		}
	}
}

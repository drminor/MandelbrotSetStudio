using MSS.Types;
using System;
using System.Collections.Generic;

namespace MSS.Common
{
	public class RMapConstants
	{
		public static readonly SizeInt BLOCK_SIZE;

		public static readonly RRectangle ENTIRE_SET_RECTANGLE;
		public static readonly RRectangle ENTIRE_SET_RECTANGLE_EVEN;


		public static readonly RRectangle TEST_RECTANGLE;
		public static readonly RRectangle TEST_RECTANGLE_HALF;

		public const int DEFAULT_PRECISION = 65;

		static RMapConstants()
		{
			BLOCK_SIZE = new SizeInt(128, 128);


			// The set goes from x = -2 to 1 and from y = -1 to 1.
			// Setting the exponent to -1 (i.e, using a factor of 1/2) this is
			// x = -4/2 to 2/2 and y = -2/2 to 2/2

			// Setting the exponent to 0 (i.e, using a factor of 1) this is
			// x = -2/1 to 1/1 and y = -1/1 to 1/1

			ENTIRE_SET_RECTANGLE = new RRectangle(-4, 2, -3, 3, -1);

			ENTIRE_SET_RECTANGLE_EVEN = new RRectangle(-4, 4, -4, 4, -1);

			TEST_RECTANGLE = new RRectangle(0, 1, 0, 1, 0);

			TEST_RECTANGLE_HALF = new RRectangle(1, 2, 1, 2, -2);
		}

		public static ColorBandSet BuildInitialColorBandSet(int maxIterations)
		{
			var colorBands = new List<ColorBand>
			{
				new ColorBand(1, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(2, "#ff0033", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(3, "#ffffcc", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(5, "#ccccff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(10, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(25, "#ff0033", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(50, "#ffffcc", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(60, "#ccccff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(70, "#ffffff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(120, "#ff0033", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(300, "#ffffcc", ColorBandBlendStyle.Next, "#000000")
				//new ColorBand(500, "#e95ee8", ColorBandBlendStyle.End, "#758cb7")
			};

			var colorBandsSerialNumber = new Guid("{00112233-4455-6677-8899-AABBCCDDEEFF}");

			var highColorCss = "#000000";
			colorBands.Add(new ColorBand(maxIterations, highColorCss, ColorBandBlendStyle.None, highColorCss));

			var result = new ColorBandSet(colorBands, colorBandsSerialNumber);

			return result;
		}


	}
}

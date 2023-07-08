using MSS.Types.MSet;
using System;
using System.Collections.Generic;

namespace MSS.Types
{
	public class RMapConstants
	{
		public const string SERVICE_NAME = "MongoDB";
		
		public const int DEFAULT_MONGO_DB_PORT = 27017;
		public const string DEFAULT_DATA_BASE_NAME = "MandelbrotProjects";

		public static readonly SizeInt BLOCK_SIZE;
		public const int DEFAULT_THRESHOLD = 4;

		public const byte BITS_BEFORE_BP = 8;

		public const int DEFAULT_LIMB_COUNT = 2;
		public const int DEFAULT_TARGET_EXPONENT = -64;

		public const int DEFAULT_PRECISION = 53;

		public const int MAP_SECTION_VALUE_POOL_SIZE = 10;

		public const double DEFAULT_POSTER_DISPLAY_ZOOM = 0.01; // Default to full screen view

		public const double POSTER_DISPLAY_ZOOM_MIN_DIFF = 0.0001; // The minimum amount by which two DisplayZoom values must differ in order to considered a 'real' update.

		public static readonly SizeInt DEFAULT_POSTER_SIZE = new SizeInt(4096);

		public const int MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS = 30;

		public static readonly RRectangle ENTIRE_SET_RECTANGLE;
		public static readonly RRectangle ENTIRE_SET_RECTANGLE_EVEN;

		public static readonly RRectangle TEST_RECTANGLE;
		public static readonly RRectangle TEST_RECTANGLE_HALF;

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

				new ColorBand(49, "#ffffcc", ColorBandBlendStyle.Next, "#000000"),

				//new ColorBand(49, "#ffffcc", ColorBandBlendStyle.Next, "#000000"),
				//new ColorBand(52, "#00ccff", ColorBandBlendStyle.Next, "#000000"),

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


		public static MapAreaInfo2 BuildHomeArea()
		{
			var subdivision = new Subdivision(new RSize(new RValue(1, -8)), new BigVector());
			var mapAreaInfo = new MapAreaInfo2(new RPoint(0, 0, -8), subdivision, DEFAULT_PRECISION, new BigVector(), new VectorInt());

			return mapAreaInfo;
		}

	}
}

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

		public const int DEFAULT_TARGET_ITERATIONS = 400;
		public const int DEFAULT_THRESHOLD = 4;
		public const int DEFAULT_NORMALIZED_THRESHOLD = 100;

		public const bool DEFAULT_CALCULATE_ESCAPE_VELOCITIES = true;
		public const bool DEFAULT_SAVE_THE_ZVALUES = false;

		public const byte BITS_BEFORE_BP = 16;

		public const int DEFAULT_LIMB_COUNT = 2;
		public const int DEFAULT_TARGET_EXPONENT = -64;

		public const int DEFAULT_PRECISION = 53;

		public const int MAP_SECTION_INITIAL_POOL_SIZE = 10;

		public const double DEFAULT_POSTER_DISPLAY_ZOOM = 0.01; // Default to full screen view
		public const double DEFAULT_MINIMUM_DISPLAY_ZOOM = 0.015625; // TODO: Consider setting the Default Minimum Display Zoom to 0.001953125

		public const double POSTER_DISPLAY_ZOOM_MIN_DIFF = 0.0001; // The minimum amount by which two DisplayZoom values must differ in order to considered a 'real' update.

		public static readonly SizeInt DEFAULT_POSTER_SIZE = new SizeInt(4096);

		public const int MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS = 30;

		public static readonly RRectangle ENTIRE_SET_RECTANGLE;
		public static readonly RRectangle ENTIRE_SET_RECTANGLE_EVEN;

		public static readonly RRectangle TEST_RECTANGLE;
		public static readonly RRectangle TEST_RECTANGLE_HALF;

		public const string NAME_FOR_NEW_PROJECTS = "New Project";

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

		public static ColorBandSet BuildInitialColorBandSet(string name, int maxIterations, bool usePercentages)
		{
			var result = usePercentages 
				? BuildInitialColorBandSetWithPercentages(name, maxIterations) 
				: BuildInitialColorBandSetWithCutoffs(name, maxIterations);

			return result;
		}

		private static ColorBandSet BuildInitialColorBandSetWithCutoffs(string name, int maxIterations)
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
				new ColorBand(60, "#ccccff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(70, "#0033ff", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(120, "#ff0033", ColorBandBlendStyle.Next, "#000000"),
				new ColorBand(300, "#ffffcc", ColorBandBlendStyle.Next, "#cce8ff"),
				new ColorBand(400, "#cce8ff", ColorBandBlendStyle.Next, "#000000")

				//new ColorBand(500, "#e95ee8", ColorBandBlendStyle.End, "#758cb7")
			};

			var colorBandsSerialNumber = new Guid("{00112233-4455-6677-8899-AABBCCDDEEFF}");
			var result = new ColorBandSet(name, colorBands, maxIterations, colorBandsSerialNumber);

			return result;
		}

		private static ColorBandSet BuildInitialColorBandSetWithPercentages(string name, int maxIterations)
		{
			var colorBands = new List<ColorBand>
			{
				new ColorBand(1, "#ffffff", ColorBandBlendStyle.Next, "#000000", 0),
				new ColorBand(2, "#ff0033", ColorBandBlendStyle.Next, "#000000", 0.01),
				new ColorBand(3, "#ffffcc", ColorBandBlendStyle.Next, "#000000", 11.72),
				new ColorBand(5, "#ccccff", ColorBandBlendStyle.Next, "#000000", 26.98),
				new ColorBand(10, "#ffffff", ColorBandBlendStyle.Next, "#000000", 39.08),
				new ColorBand(25, "#ff0033", ColorBandBlendStyle.Next, "#000000", 20.15), 
				new ColorBand(49, "#ffffcc", ColorBandBlendStyle.Next, "#000000", 1.48),
				new ColorBand(60, "#ccccff", ColorBandBlendStyle.Next, "#000000", 0.14),
				new ColorBand(70, "#0033ff", ColorBandBlendStyle.Next, "#000000", 0.09),
				new ColorBand(120, "#ff0033", ColorBandBlendStyle.Next, "#000000", 0.19),
				new ColorBand(300, "#ffffcc", ColorBandBlendStyle.Next, "#cce8ff", 0.15),
				new ColorBand(400, "#cce8ff", ColorBandBlendStyle.Next, "#000000", 0.02)
			};
				// > 400 = 9.55%

			var colorBandsSerialNumber = new Guid("{00112233-4455-6677-8899-AABBCCDDEE00}");
			var result = new ColorBandSet(name, colorBands, maxIterations, colorBandsSerialNumber);

			return result;
		}

		/*
1	0	 -1
2	0.01	1-2
3	11.72	2-3
4	26.98	3-5
5	39.08	5-10
6	20.15	10-25
7	1.48	25-49
8	0.14	49-60
9	0.09	60-70
10	0.19	70-120	
11	0.15	120-300
12	0.02	300-400		
		
		*/
		public static MapCenterAndDelta BuildHomeArea()
		{
			var samplePointDelta = new RSize(1, 1, -8);
			var baseMapPosition = new BigVector();
			var subdivision = new Subdivision(samplePointDelta, baseMapPosition);

			var mapCenter = new RPoint(0, 0, -8);

			var mapAreaInfo = new MapCenterAndDelta(mapCenter, subdivision, DEFAULT_PRECISION, new VectorLong(), new VectorInt());

			return mapAreaInfo;
		}

		public static MapCalcSettings BuildMapCalcSettings()
		{
			var result = new MapCalcSettings(
				targetIterations: DEFAULT_TARGET_ITERATIONS, 
				threshold: DEFAULT_THRESHOLD, 
				calculateEscapeVelocities: DEFAULT_CALCULATE_ESCAPE_VELOCITIES, 
				saveTheZValues: DEFAULT_SAVE_THE_ZVALUES
				);
			
			return result;
		}

		public const string UltraZoom5X = "-1.740062382579339905220844167065825638296641720436171866879862418461182919644153056054840718339483225743450008259172138785492983677893366503417299549623738838303346465461290768441055486136870719850559269507357211790243666940134793753068611574745943820712885258222629105433648695946003865";
		public const string UltraZoom5Y = "0.0281753397792110489924115211443195096875390767429906085704013095958801743240920186385400814658560553615695084486774077000669037710191665338060418999324320867147028768983704831316527873719459264592084600433150333362859318102017032958074799966721030307082150171994798478089798638258639934";

	}
}

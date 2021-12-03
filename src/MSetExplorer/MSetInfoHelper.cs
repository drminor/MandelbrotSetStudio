using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Numerics;

namespace MSetExplorer
{
	internal static class MSetInfoHelper
	{
		public static MSetInfo BuildInitialMSetInfo()
		{
			var canvasSize = new SizeInt(768, 768);
			var coords = RMapConstants.ENTIRE_SET_RECTANGLE;
			var mapCalcSettings = new MapCalcSettings(maxIterations: 4000, threshold: 4, iterationsPerStep: 100);

			IList<ColorMapEntry> colorMapEntries = new List<ColorMapEntry>
			{
				new ColorMapEntry(1, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(2, "#ff0033", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(3, "#ffffcc", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(5, "#ccccff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(10, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(25, "#ff0033", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(50, "#ffffcc", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(60, "#ccccff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(70, "#ffffff", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(90, "#e95ee8", ColorMapBlendStyle.End, "#758cb7")
			};

			string highColorCss = "#000000";
			var result = new MSetInfo(canvasSize, coords, mapCalcSettings, colorMapEntries, highColorCss);

			return result;
		}

		public static Subdivision GetSubdivision(MSetInfo mSetInfo)
		{
			var id = ObjectId.GenerateNewId();
			var origin = new RPoint();
			var blockSize = RMapConstants.BLOCK_SIZE;


			// TODO: Calculate the number of blocks to cover the canvas
			//		then figure the difference in map coordinates from the beginning and end of a single block
			var samplePointDelta = new RSize(BigInteger.One, BigInteger.One, -8);

			var result = new Subdivision(id, origin, blockSize, samplePointDelta);

			return result;
		}

	}
}

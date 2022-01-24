using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;

namespace MSetExplorer
{
	internal static class MSetInfoHelper
	{
		public static MSetInfo BuildInitialMSetInfo(int maxIterations)
		{
			var coords = RMapConstants.ENTIRE_SET_RECTANGLE;
			var mapCalcSettings = new MapCalcSettings(maxIterations: maxIterations, threshold: 4, iterationsPerStep: 100);

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
				new ColorMapEntry(120, "#ff0033", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(300, "#ffffcc", ColorMapBlendStyle.Next, "#000000"),
				new ColorMapEntry(500, "#e95ee8", ColorMapBlendStyle.End, "#758cb7")
			};

			//IList<ColorMapEntry> colorMapEntries = new List<ColorMapEntry>
			//{
			//	new ColorMapEntry(1, "#ffffff", ColorMapBlendStyle.None, "#000000"),
			//	new ColorMapEntry(2, "#0000FF", ColorMapBlendStyle.None, "#000000"),
			//	new ColorMapEntry(3, "#00ff00", ColorMapBlendStyle.None, "#000000"),
			//	new ColorMapEntry(5, "#0000ff", ColorMapBlendStyle.None, "#000000"),
			//	new ColorMapEntry(40, "#00ff00", ColorMapBlendStyle.None, "#758cb7")
			//};

			var highColorCss = "#000000";
			var result = new MSetInfo(coords, mapCalcSettings, colorMapEntries, highColorCss);

			return result;
		}

	}
}

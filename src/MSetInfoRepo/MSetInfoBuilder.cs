using FSTypes;
using System.Collections.Generic;

namespace MSetInfoRepo
{
	public static class MSetInfoBuilder
	{
		public static MSetInfo Recreate(string name)
		{
			MSetInfo info = GetMFileInfo(name);
			return info;
		}

		private static MSetInfo GetMFileInfo(string name)
		{
			switch (name)
			{
				case "Circus1":
					{
						return BuildCircus1();
					}
				default:
					throw new KeyNotFoundException($"Cound not find a recreation script with name = {name}.");
			}
		}

		// TODO: Update BuildCircus1 to create a MSetInfo instead
		private static MSetInfo BuildCircus1()
		{
			Coords coords = new Coords(
				startingX: "-7.66830585754868944856241303572093e-01",
				startingY: "1.08316038593833397341534199100796e-01",
				endingX: "-7.66830587074704020221573662634195e-01",
				endingY: "1.08316039471787068157292062147129e-01"
				);

			bool isHighRes = false;
			IList<ColorMapEntry> entries = new List<ColorMapEntry>();

			entries.Add(new ColorMapEntry(375, "#ffffff", ColorMapBlendStyle.Next, "#000000"));
			entries.Add(new ColorMapEntry(399, "#fafdf2", ColorMapBlendStyle.Next, "#000000"));
			entries.Add(new ColorMapEntry(407, "#98e498", ColorMapBlendStyle.Next, "#000000"));
			entries.Add(new ColorMapEntry(428, "#0000ff", ColorMapBlendStyle.Next, "#000000"));
            entries.Add(new ColorMapEntry(446, "#f09ee6", ColorMapBlendStyle.Next, "#000000"));
            entries.Add(new ColorMapEntry(486, "#00ff00", ColorMapBlendStyle.Next, "#000000"));
            entries.Add(new ColorMapEntry(500, "#0000ff", ColorMapBlendStyle.Next, "#000000"));
            entries.Add(new ColorMapEntry(523, "#ffffff", ColorMapBlendStyle.Next, "#000000"));
			entries.Add(new ColorMapEntry(560, "#3ee2e2", ColorMapBlendStyle.Next, "#000000"));
			entries.Add(new ColorMapEntry(1011, "#e95ee8", ColorMapBlendStyle.End, "#758cb7"));

			string highColorCss = "#000000";

			int maxIterations = 4000;
			int threshold = 4;
			int iterationsPerStep = 100;

			var colorMap = new ColorMap(entries, maxIterations, highColorCss);

			MSetInfo result = new MSetInfo("Circus1", coords, isHighRes, maxIterations, threshold, iterationsPerStep, colorMap);

			return result;
		}

	}
}

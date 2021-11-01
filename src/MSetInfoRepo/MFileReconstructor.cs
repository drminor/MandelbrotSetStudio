using FSTypes;
using System.Collections.Generic;

namespace MSetInfoRepo
{
	public static class MFileReconstructor
	{
		public static void Recreate(string name, string path)
		{
			MFileInfo info = GetMFileInfo(name);
			MFileReaderWriter.Write(info, path);
		}

		private static MFileInfo GetMFileInfo(string name)
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
		private static MFileInfo BuildCircus1()
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

			MFileInfo result = new MFileInfo("Circus1", coords, isHighRes, 4000, 4, 100, entries, highColorCss);

			return result;
		}

	}
}

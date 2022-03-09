using System.Collections.Generic;
using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public ColorMapEntry[] ColorMapEntries { get; init; }

		public MSetInfo(RRectangle coords, MapCalcSettings mapCalcSettings, ColorMapEntry[] colorMapEntries)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
			ColorMapEntries = colorMapEntries;
		}

		public static MSetInfo UpdateWithNewCoords(MSetInfo source, RRectangle newCoords)
		{
			return new MSetInfo(newCoords, source.MapCalcSettings, Clone(source.ColorMapEntries));
		}

		public static MSetInfo UpdateWithNewIterations(MSetInfo source, int targetIterations, int iterationsPerRequest)
		{
			var colorMapEntries = Clone(source.ColorMapEntries);
			var lastEntry = colorMapEntries[^1];
			colorMapEntries[^1] = ColorMapEntry.UpdateCutOff(lastEntry, targetIterations);
			
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(targetIterations, 4, iterationsPerRequest), colorMapEntries);
		}

		public static MSetInfo UpdateWithNewColorMapEntries(MSetInfo source, ColorMapEntry[] colorMapEntries)
		{
			var lastEntry = colorMapEntries[^1];
			Debug.Assert(lastEntry.CutOff == source.MapCalcSettings.TargetIterations, "TargetIteration / ColorMapEntries-HighEntry MisMatch.");
			return new MSetInfo(source.Coords.Clone(), source.MapCalcSettings, Clone(colorMapEntries));
		}

		private static ColorMapEntry[] Clone(ColorMapEntry[] colorMapEntries)
		{
			var result = new List<ColorMapEntry>();

			foreach(var cme in colorMapEntries)
			{
				result.Add(cme.Clone());
			}

			return result.ToArray();
		}

	}
}

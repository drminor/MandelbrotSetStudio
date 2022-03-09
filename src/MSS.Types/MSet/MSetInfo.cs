using System.Collections.Generic;
using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public ColorBand[] ColorBands { get; init; }

		public MSetInfo(RRectangle coords, MapCalcSettings mapCalcSettings, ColorBand[] colorBands)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
			ColorBands = colorBands;
		}

		public static MSetInfo UpdateWithNewCoords(MSetInfo source, RRectangle newCoords)
		{
			return new MSetInfo(newCoords, source.MapCalcSettings, Clone(source.ColorBands));
		}

		public static MSetInfo UpdateWithNewIterations(MSetInfo source, int targetIterations, int iterationsPerRequest)
		{
			var colorBands = Clone(source.ColorBands);
			var lastEntry = colorBands[^1];
			colorBands[^1] = ColorBand.UpdateCutOff(lastEntry, targetIterations);
			
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(targetIterations, 4, iterationsPerRequest), colorBands);
		}

		public static MSetInfo UpdateWithNewColorMapEntries(MSetInfo source, ColorBand[] colorBands)
		{
			var lastEntry = colorBands[^1];
			Debug.Assert(lastEntry.CutOff == source.MapCalcSettings.TargetIterations, "TargetIteration / ColorMapEntries-HighEntry MisMatch.");
			return new MSetInfo(source.Coords.Clone(), source.MapCalcSettings, Clone(colorBands));
		}

		private static ColorBand[] Clone(ColorBand[] colorBands)
		{
			var result = new List<ColorBand>();

			foreach(var cme in colorBands)
			{
				result.Add(cme.Clone());
			}

			return result.ToArray();
		}

	}
}

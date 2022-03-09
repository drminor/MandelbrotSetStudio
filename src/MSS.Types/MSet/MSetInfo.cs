using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public ColorBandSet ColorBands { get; init; }

		public MSetInfo(RRectangle coords, MapCalcSettings mapCalcSettings, ColorBandSet colorBands)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
			ColorBands = colorBands;
		}

		public static MSetInfo UpdateWithNewCoords(MSetInfo source, RRectangle newCoords)
		{
			return new MSetInfo(newCoords.Clone(), source.MapCalcSettings, source.ColorBands.Clone());
		}

		public static MSetInfo UpdateWithNewIterations(MSetInfo source, int targetIterations, int iterationsPerRequest)
		{
			var colorBands = source.ColorBands.Clone();
			_ =colorBands.TrySetHighCutOff(targetIterations);
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(targetIterations, 4, iterationsPerRequest), colorBands);
		}

		public static MSetInfo UpdateWithNewColorMapEntries(MSetInfo source, ColorBandSet colorBands)
		{
			var lastEntry = colorBands[^1];
			Debug.Assert(lastEntry.CutOff == source.MapCalcSettings.TargetIterations, "TargetIteration / ColorMapEntries-HighEntry MisMatch.");
			return new MSetInfo(source.Coords.Clone(), source.MapCalcSettings, colorBands.Clone());
		}

	}
}

using System.Diagnostics;

namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		//public ColorBandSet ColorBandSet { get; init; }

		public MSetInfo(RRectangle coords, MapCalcSettings mapCalcSettings/*, ColorBandSet colorBands*/)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
			//ColorBandSet = colorBands;
		}

		public static MSetInfo UpdateWithNewCoords(MSetInfo source, RRectangle newCoords)
		{
			//return new MSetInfo(newCoords.Clone(), source.MapCalcSettings, source.ColorBands.Clone());
			return new MSetInfo(newCoords.Clone(), source.MapCalcSettings/*, source.ColorBandSet*/);
		}

		public static MSetInfo UpdateWithNewIterations(MSetInfo source, int targetIterations, int iterationsPerRequest)
		{
			//var colorBands = source.ColorBandSet.Clone();
			//colorBands.HighCutOff = targetIterations;
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(targetIterations, iterationsPerRequest)/*, colorBands*/);
		}

		//public static MSetInfo UpdateWithNewColorMapEntries(MSetInfo source, ColorBandSet colorBandSet)
		//{
		//	//Debug.Assert(colorBandSet.HighCutOff == source.MapCalcSettings.TargetIterations, "The MapCalcSettings.TargetIteration does not match ColorBandSet.HighCutOff.");
		//	colorBandSet.HighCutOff = source.MapCalcSettings.TargetIterations;

		//	return new MSetInfo(source.Coords.Clone(), source.MapCalcSettings, colorBandSet);
		//}

	}
}

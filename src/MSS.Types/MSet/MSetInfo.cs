using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public ColorMapEntry[] ColorMapEntries { get; init; }

		public MSetInfo(
			RRectangle coords,
			MapCalcSettings mapCalcSettings,
			ColorMapEntry[] colorMapEntries
			)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
			ColorMapEntries = colorMapEntries;
		}

		public static MSetInfo UpdateWithNewCoords(MSetInfo source, RRectangle newCoords)
		{
			return new MSetInfo(newCoords, source.MapCalcSettings, Clone(source.ColorMapEntries));
		}
		//int targetIterations, int iterationsPerRequest
		public static MSetInfo UpdateWithNewIterations(MSetInfo source, int targetIterations, int iterationsPerRequest)
		{
			ColorMapEntry[] cmes = Clone(source.ColorMapEntries);
			var he = cmes[cmes.Length - 1];
			cmes[cmes.Length - 1] = new ColorMapEntry(targetIterations, he.StartColor, he.BlendStyle, he.EndColor);
			
			return new MSetInfo(source.Coords.Clone(), new MapCalcSettings(targetIterations, 4, iterationsPerRequest), cmes);
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

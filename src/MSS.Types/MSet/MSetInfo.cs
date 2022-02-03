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

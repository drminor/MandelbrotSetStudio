using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public IList<ColorMapEntry> ColorMapEntries { get; init; }
		public string HighColorCss { get; init; }

		public MSetInfo(
			RRectangle coords,
			MapCalcSettings mapCalcSettings,
			IList<ColorMapEntry> colorMapEntries,
			string highColorCss
			)
		{
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
			ColorMapEntries = colorMapEntries;
			HighColorCss = highColorCss;
		}

		public static MSetInfo UpdateWithNewCoords(RRectangle newCoords, MSetInfo source)
		{
			return new MSetInfo(newCoords, source.MapCalcSettings, Clone(source.ColorMapEntries), source.HighColorCss);
		}

		//public MSetInfo(MSetInfo currentInfo, RRectangle newCoords) : this(newCoords, currentInfo.MapCalcSettings, Clone(currentInfo.ColorMapEntries), currentInfo.HighColorCss)
		//{ }

		private static IList<ColorMapEntry> Clone(IList<ColorMapEntry> colorMapEntries)
		{
			var result = new List<ColorMapEntry>();

			foreach(var cme in colorMapEntries)
			{
				result.Add(cme.Clone());
			}

			return result;
		}

	}
}

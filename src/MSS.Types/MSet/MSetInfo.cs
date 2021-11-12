using System.Collections.Generic;

namespace MSS.Types.MSet
{
	public class MSetInfo
	{
		public SizeInt CanvasSize { get; init; }
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
		public IList<ColorMapEntry> ColorMapEntries { get; init; }
		public string HighColorCss { get; init; }

		public MSetInfo(
			SizeInt canvasSize,
			RRectangle coords,
			MapCalcSettings mapCalcSettings,
			IList<ColorMapEntry> colorMapEntries,
			string highColorCss
			)
		{
			CanvasSize = canvasSize;
			Coords = coords;
			MapCalcSettings = mapCalcSettings;
			ColorMapEntries = colorMapEntries;
			HighColorCss = highColorCss;
		}
	}
}

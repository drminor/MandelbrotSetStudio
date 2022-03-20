using MSS.Types.MSet;
using MSS.Types.MSetOld;
using System;

namespace MSS.Common
{
	public class MSetInfoOld
    {
		public MSetInfoOld(string name, ApCoords apCoords, bool isHighRes, MapCalcSettings mapCalcSettings, ColorMap colorMap)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			ApCoords = apCoords ?? throw new ArgumentNullException(nameof(apCoords));
			IsHighRes = isHighRes;
			MapCalcSettings = mapCalcSettings;
			ColorMap = colorMap ?? throw new ArgumentNullException(nameof(colorMap));
		}

		public string Name { get; init; }
        public ApCoords ApCoords { get; init; }
		public bool IsHighRes { get; init; }

		public MapCalcSettings MapCalcSettings { get; init; }
        public ColorMap ColorMap { get; init; }

		public string HighColorCss => ColorMap.ColorBandSet?.ColorBands[^1].ActualEndColor.GetCssColor() ?? "#000000";

    }
}

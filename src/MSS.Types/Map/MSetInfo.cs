﻿using MSS.Types.MSet;
using System;

namespace MSS.Types
{
	public class MSetInfo
    {
		public MSetInfo(string name, ApCoords apCoords, bool isHighRes, MapCalcSettings mapCalcSettings, ColorMap colorMap)
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

		public string HighColorCss => ColorMap.HighColorEntry.StartColor.CssColor;

    }
}

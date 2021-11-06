using System;

namespace MSS.Types
{
	public class MSetInfo
    {
		public MSetInfo(string name, ApCoords apCoords, bool isHighRes, int maxIterations, int threshold, int interationsPerStep, ColorMap colorMap)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			ApCoords = apCoords ?? throw new ArgumentNullException(nameof(apCoords));
			IsHighRes = isHighRes;
			MaxIterations = maxIterations;
			Threshold = threshold;
			InterationsPerStep = interationsPerStep;
			ColorMap = colorMap ?? throw new ArgumentNullException(nameof(colorMap));
		}

		public string Name { get; init; }
        public ApCoords ApCoords { get; init; }
		public bool IsHighRes { get; init; }

		public int MaxIterations { get; init; }
		public int Threshold { get; init; }
		public int InterationsPerStep { get; init; }
        public ColorMap ColorMap { get; init; }

		public string HighColorCss => ColorMap.HighColorEntry.StartColor.CssColor;

    }
}

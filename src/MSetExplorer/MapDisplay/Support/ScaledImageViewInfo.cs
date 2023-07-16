using MSS.Types;
using System;

namespace MSetExplorer
{
	// Screen Coordinates with double precision accuracy.
	public class ScaledImageViewInfo
	{
		private static ScaledImageViewInfo _zeroSingleton = new ScaledImageViewInfo(new SizeDbl(), new VectorDbl(), 0.0, null);
		public static ScaledImageViewInfo Zero => _zeroSingleton;

		public SizeDbl ContentViewportSize { get; init; }
		public VectorDbl ContentOffset { get; init; }
		public double ContentScale { get; init; }
		public RectangleDbl? TranslationAndClipSize { get; set; }

		public ScaledImageViewInfo(SizeDbl contentViewportSize, VectorDbl contentOffset, double contentScale, RectangleDbl? translationAndClipSize)
		{
			ContentViewportSize = contentViewportSize;
			ContentOffset = contentOffset;
			ContentScale = contentScale;
			TranslationAndClipSize = translationAndClipSize;	
		}

		public bool IsEmpty(double threshold = 0.1)
		{
			return Math.Abs(ContentScale) < threshold && ContentViewportSize.IsNearZero();
		}

	}

}

using MSS.Types;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MSetExplorer
{
	public interface IBitmapGrid
	{
		Dispatcher Dispatcher { get; }

		SizeDbl ViewportSize { get; }
		BigVector MapBlockOffset { get; set; }
		VectorDbl ImageOffset { get; set; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; set; }

		bool HighlightSelectedColorBand { get; set; }

		bool UseEscapeVelocities { get; set; }

		void ClearDisplay();
		bool DrawSections(IList<MapSection> mapSections);
		int ReDrawSections();

		bool GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors);
	}
}
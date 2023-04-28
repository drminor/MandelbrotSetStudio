using MSS.Types;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MSetExplorer
{
	public interface IBitmapGrid2
	{
		Dispatcher Dispatcher { get; }

		SizeDbl ViewPortSize { get; }
		BigVector MapBlockOffset { get; set; }
		VectorDbl ImageOffset { get; set; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; set; }

		bool HighlightSelectedColorBand { get; set; }

		bool UseEscapeVelocities { get; set; }

		void ClearDisplay();
		bool DrawSections(IList<MapSection> mapSections);
		int ReDrawSections();

		void GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors);
	}
}
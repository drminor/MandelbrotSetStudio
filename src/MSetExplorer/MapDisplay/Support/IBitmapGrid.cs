using MSS.Types;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MSetExplorer
{
	public interface IBitmapGrid
	{
		Dispatcher Dispatcher { get; }

		WriteableBitmap Bitmap { get; }

		SizeDbl LogicalViewportSize { get; set; }
		BigVector MapBlockOffset { get; set; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; set; }

		bool HighlightSelectedColorBand { get; set; }

		bool UseEscapeVelocities { get; set; }

		void ClearDisplay();
		void DrawSections(IList<MapSection> mapSections);
		int ClearSections(IList<MapSection> mapSections);
		int ReDrawSections();

		bool GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors);
	}
}
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MSetExplorer
{
	public interface IBitmapGrid
	{
		Dispatcher Dispatcher { get; }

		WriteableBitmap Bitmap { get; }

		VectorLong MapBlockOffset { get; set; }
		SizeDbl LogicalViewportSize { get; set; }
		VectorInt CanvasControlOffset { get; set; }
		SizeInt ImageSizeInBlocks { get; }
		SizeInt CanvasSizeInBlocks { get; }

		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; }
		int CurrentColorBandIndex { get; set; }

		bool HighlightSelectedColorBand { get; set; }

		bool UseEscapeVelocities { get; set; }

		void ClearDisplay();
		void DrawSections(IList<MapSection> mapSections);

		//int ClearSections(IList<MapSection> mapSections);
		int ClearSections(List<Tuple<int, PointInt, VectorLong>> jobAndScreenPositions);

		int ReDrawSections(bool reapplyColorMap);

		//bool DrawOneSection(MapSection mapSection, MapSectionVectors mapSectionVectors, string description);
		bool DrawOneSection(MapSection mapSection, MapSectionVectors mapSectionVectors, string description, bool reapplyColorMap = true);

		SizeInt CalculateImageSize(SizeDbl logicalViewportSize, VectorInt canvasControlOffset);
		List<MapSection> GetSectionsNotVisible();

		SizeInt CalculateCanvasSize(SizeDbl logicalViewportSize);

	}
}
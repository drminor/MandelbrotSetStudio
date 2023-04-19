using MSS.Types;
using System.Collections.Generic;
using System.Windows.Threading;

namespace MSetExplorer
{
	public interface IBitmapGrid
	{
		BigVector MapBlockOffset { get; set; }
		//VectorDbl ImageOffset { get; set; }

		void ClearDisplay();
		bool DrawSections(IList<MapSection> mapSections);
		int ReDrawSections();

		Dispatcher Dispatcher { get; }

		void GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors);

	}
}
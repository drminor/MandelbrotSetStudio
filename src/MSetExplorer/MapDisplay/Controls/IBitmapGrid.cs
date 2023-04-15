using MSS.Types;
using System.Collections.Generic;

namespace MSetExplorer
{
	public interface IBitmapGrid
	{
		void ClearDisplay();
		bool DrawSections(IList<MapSection> mapSections);
		bool GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors);
		List<MapSection> ReDrawSections();
	}
}
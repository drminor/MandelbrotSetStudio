using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace MSetExplorer
{
	public interface IBitmapGrid
	{
		SizeDbl ViewPortSize { get; }
		BigVector MapBlockOffset { get; set; }
		VectorDbl ImageOffset { get; set; }

		void ClearDisplay();
		bool DrawSections(IList<MapSection> mapSections);
		int ReDrawSections();

		Dispatcher Dispatcher { get; }

		void GetAndPlacePixels(MapSection mapSection, MapSectionVectors mapSectionVectors);


		ColorBandSet ColorBandSet { get; set; }
		ColorBand? CurrentColorBand { get; set; }

		SizeInt BlockSize { get; set; }	
		Action<MapSection>? DisposeMapSection { get; set; }
		ObservableCollection<MapSection> MapSections { get; set; }
	}
}
using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : IMapDisplayViewModel
	{
		public MapDisplayViewModel(SizeInt blockSize)
		{
			BlockSize = blockSize;
			MapSections = new ObservableCollection<MapSection>();
		}

		public SizeInt BlockSize { get; }

		public ObservableCollection<MapSection> MapSections { get; }

		public Action<MapSection> HandleMapSectionReady => OnMapSectionReady;

		public Action HandleMapNav => OnMapNav;

		private void OnMapSectionReady(MapSection mapSection)
		{
			MapSections.Add(mapSection);
		}

		private void OnMapNav()
		{
			MapSections.Clear();
		}
	}
}

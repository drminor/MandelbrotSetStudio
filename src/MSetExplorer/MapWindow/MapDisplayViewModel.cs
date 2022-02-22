using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	internal class MapDisplayViewModel : ViewModelBase, IMapDisplayViewModel
	{
		public MapDisplayViewModel(SizeInt blockSize)
		{
			BlockSize = blockSize;
			MapSections = new ObservableCollection<MapSection>();
		}

		public new bool InDesignMode => base.InDesignMode;

		public SizeInt BlockSize { get; }
		public SizeInt CanvasSize { get; set; }
		public SizeInt CanvasControlOffset { get; set; }

		public ObservableCollection<MapSection> MapSections { get; }

		public Action<MapSection> HandleMapSectionReady => OnMapSectionReady;
		public Action<SizeInt> HandleMapNav => OnMapNav;

		private void OnMapSectionReady(MapSection mapSection)
		{
			MapSections.Add(mapSection);
		}

		private void OnMapNav(SizeInt canvasControOffset)
		{
			CanvasControlOffset = canvasControOffset;
			MapSections.Clear();
		}
	}
}

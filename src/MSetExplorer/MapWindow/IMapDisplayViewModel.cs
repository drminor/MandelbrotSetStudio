using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel
	{
		bool InDesignMode { get; }

		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; set; }
		VectorInt CanvasControlOffset { get; set; }

		ObservableCollection<MapSection> MapSections { get; }
		Action<MapSection> HandleMapSectionReady { get; }
		Action<VectorInt> HandleMapNav { get; }
	}
}
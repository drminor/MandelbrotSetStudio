using MSS.Types;
using MSS.Types.Screen;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel
	{
		public event PropertyChangedEventHandler PropertyChanged;

		bool InDesignMode { get; }

		SizeInt BlockSize { get; }
		//SizeInt CanvasSize { get; set; }
		VectorInt CanvasControlOffset { get; set; }

		ObservableCollection<MapSection> MapSections { get; }
	}
}
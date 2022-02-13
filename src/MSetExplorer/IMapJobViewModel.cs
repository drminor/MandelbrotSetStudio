using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System.Collections.ObjectModel;
using System.Windows;

namespace MSetExplorer
{
	internal interface IMapJobViewModel
	{
		//bool InDesignMode { get; }

		SizeInt BlockSize { get; }
		bool CanGoBack { get; }
		Job CurrentJob { get; }

		Point GetBlockPosition(Point posYInverted);
		void GoBack();

		ObservableCollection<MapSection> MapSections { get; }
	}
}
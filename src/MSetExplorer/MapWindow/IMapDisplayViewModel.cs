using MSS.Types;
using MSS.Types.Screen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace MSetExplorer
{
	public interface IMapDisplayViewModel
	{
		public event PropertyChangedEventHandler PropertyChanged;

		bool InDesignMode { get; }

		ImageSource ImageSource { get; }
		SizeInt BlockSize { get; }
		SizeInt CanvasSize { get; set; }
		VectorInt CanvasControlOffset { get; set; }
		ObservableCollection<MapSection> MapSections { get; }

		IReadOnlyList<MapSection> GetMapSectionsSnapShot();
		void ShiftMapSections(VectorInt amount);
	}
}
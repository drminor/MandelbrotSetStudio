using MSS.Types;
using MSS.Types.Screen;
using System.Collections.Generic;
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

		private VectorInt _canvasControlOffset;
		public VectorInt CanvasControlOffset
		{ 
			get => _canvasControlOffset;
			set { _canvasControlOffset = value; OnPropertyChanged(); }
		}

		public ObservableCollection<MapSection> MapSections { get; }

		public IReadOnlyList<MapSection> GetMapSectionsSnapShot()
		{
			return new ReadOnlyCollection<MapSection>(MapSections);
		}
	}
}

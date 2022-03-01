using MSS.Types.Screen;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MSetExplorer
{
	internal class MapSectionCollectionBinder : IMapSectionCollectionBinder
	{
		private readonly IScreenSectionCollection _screenSectionCollection;

		#region Constructor

		public MapSectionCollectionBinder(IScreenSectionCollection screenSectionCollection, ObservableCollection<MapSection> mapSections)
		{
			_screenSectionCollection = screenSectionCollection;
			mapSections.CollectionChanged += MapSections_CollectionChanged;
		}

		#endregion

		public void Test()
		{
			_screenSectionCollection.Test();
		}

		#region Event Handlers

		private void MapSections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
			{
				//	Reset
				_screenSectionCollection.HideScreenSections();
			}
			else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
			{
				// Adding new items
				foreach (var mapSection in GetList(e.NewItems))
				{
					//Debug.WriteLine($"About to draw screen section at position: {mapSection.BlockPosition}. CanvasControlOff: {CanvasOffset}.");
					_screenSectionCollection.Draw(mapSection);
				}
			}
		}

		private IEnumerable<MapSection> GetList(IList lst)
		{
			return lst?.Cast<MapSection>() ?? new List<MapSection>();
		}

		#endregion

	}
}

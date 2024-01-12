using MSS.Types;
using System.Collections.ObjectModel;
using System.Windows.Data;

namespace MSetExplorer
{
	internal static class ColorBandSetViewHelper
	{
		public static ListCollectionView GetEmptyListCollectionView()
		{
			var newCollection = new ObservableCollection<ColorBand>();
			var result = (ListCollectionView)CollectionViewSource.GetDefaultView(newCollection);

			return result;
		}
	}
}

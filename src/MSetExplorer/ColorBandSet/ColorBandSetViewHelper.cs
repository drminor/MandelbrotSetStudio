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


		public static ColorBandSelectionType GetFrom(ColorBandSetEditMode editMode)
		{
			var selectionType =
				editMode == ColorBandSetEditMode.Bands
				? ColorBandSelectionType.Band
				: editMode == ColorBandSetEditMode.Cutoffs
					? ColorBandSelectionType.Cutoff
					: ColorBandSelectionType.Color;


			return selectionType;
		}

		public static ColorBandSetEditMode GetFrom(ColorBandSelectionType selectionType)
		{
			ColorBandSetEditMode result;

			if (selectionType == ColorBandSelectionType.None || selectionType == ColorBandSelectionType.Band)
			{
				result = ColorBandSetEditMode.Bands;
			}
			else
			{
				result = selectionType == ColorBandSelectionType.Cutoff
					? ColorBandSetEditMode.Cutoffs
					: ColorBandSetEditMode.Colors;
			}

			return result;
		}

	}
}

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

		public static ColorBandSelectionType GetSelectionType(ColorBandSetEditMode editMode)
		{
			var selectionType =
				editMode == ColorBandSetEditMode.Bands
				? ColorBandSelectionType.Band
				: editMode == ColorBandSetEditMode.Cutoffs
					? ColorBandSelectionType.Cutoff
					: ColorBandSelectionType.Color;


			return selectionType;
		}

		//public static ColorBandSetEditMode GetEditMode(ColorBandSelectionType selectionType)
		//{
		//	ColorBandSetEditMode result;

		//	if (selectionType == ColorBandSelectionType.None || selectionType == ColorBandSelectionType.Band)
		//	{
		//		result = ColorBandSetEditMode.Bands;
		//	}
		//	else
		//	{
		//		result = selectionType == ColorBandSelectionType.Cutoff
		//			? ColorBandSetEditMode.Cutoffs
		//			: ColorBandSetEditMode.Colors;
		//	}

		//	return result;
		//}

		//public static ColorBandSetEditMode? GetEditMode(bool isEditingCutoffs, bool isEditingColors)
		//{
		//	if (isEditingCutoffs)
		//	{
		//		return ColorBandSetEditMode.Cutoffs;
		//	}
		//	else if (isEditingColors)
		//	{
		//		return ColorBandSetEditMode.Colors;
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

	}
}

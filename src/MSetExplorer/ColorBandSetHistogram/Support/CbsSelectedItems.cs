using MSS.Types;
using System;
using System.Collections.Generic;
using System.Linq;


namespace MSetExplorer
{
	public class CbsSelectedItems
	{
		public CbsSelectedItems()
		{
			SelectedColorBands = new List<SelectedColorBand>();
		}

		public List<SelectedColorBand> SelectedColorBands { get; set; }

		public bool IsEmpty => SelectedColorBands.Count == 0;


		public ColorBandSelectionType Select(ColorBand colorBand, ColorBandSelectionType colorBandSelectionType)
		{
			var selectedItem = SelectedColorBands.FirstOrDefault(x => x.ColorBand == colorBand);

			if (selectedItem == null)
			{
				selectedItem = new SelectedColorBand(colorBand);
				SelectedColorBands.Add(selectedItem);
			}

			selectedItem.IsCutoffSelected = colorBandSelectionType.HasFlag(ColorBandSelectionType.Cutoff);
			selectedItem.IsColorSelected = colorBandSelectionType.HasFlag(ColorBandSelectionType.Color);
			selectedItem.IsEndColorSelected = colorBand.BlendStyle == ColorBandBlendStyle.End && colorBandSelectionType.HasFlag(ColorBandSelectionType.EndColor);


			return colorBandSelectionType;
		}
	}

	public class SelectedColorBand
	{
		public SelectedColorBand(ColorBand colorBand)
		{
			ColorBand = colorBand;
			IsColorSelected = true;
			IsColorSelected = true;
			IsEndColorSelected = ColorBand.BlendStyle == ColorBandBlendStyle.End;
		}

		public ColorBand ColorBand { get; set; }

		public bool IsCutoffSelected { get; set; }

		public bool IsColorSelected { get; set; }

		public bool IsEndColorSelected { get; set; }

		public bool IsColorBandSelected => IsCutoffSelected 
			&& IsColorSelected 
			&& (
				(ColorBand.BlendStyle == ColorBandBlendStyle.End && IsEndColorSelected)
				|| ColorBand.BlendStyle != ColorBandBlendStyle.End
			);

	}

	[Flags]
	public enum ColorBandSelectionType
	{
		Cutoff = 1,
		Color = 2,
		EndColor = 4,
		Band = 7,
		Colors = 6
	}
}

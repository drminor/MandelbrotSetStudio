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

		public void Clear()
		{
			SelectedColorBands.Clear();
		}

		public ColorBandSelectionType Select(ColorBand colorBand, ColorBandSelectionType colorBandSelectionType)
		{
			var selectedItem = SelectedColorBands.FirstOrDefault(x => x.ColorBand == colorBand);

			if (selectedItem == null)
			{
				selectedItem = new SelectedColorBand(colorBand);
				SelectedColorBands.Add(selectedItem);
			}

			//selectedItem.IsCutoffSelected = colorBandSelectionType.HasFlag(ColorBandSelectionType.Cutoff);
			//selectedItem.IsColorSelected = colorBandSelectionType.HasFlag(ColorBandSelectionType.Color);

			if (colorBandSelectionType.HasFlag(ColorBandSelectionType.Cutoff))
			{
				selectedItem.IsCutoffSelected = !selectedItem.IsCutoffSelected;
			}

			//selectedItem.IsEndColorSelected = colorBand.BlendStyle == ColorBandBlendStyle.End && colorBandSelectionType.HasFlag(ColorBandSelectionType.EndColor);
			//return colorBandSelectionType;

			if (colorBandSelectionType.HasFlag(ColorBandSelectionType.Color))
			{
				selectedItem.IsColorSelected = !selectedItem.IsColorSelected;
				//selectedItem.IsEndColorSelected = !selectedItem.IsColorSelected;
			}

			var result = selectedItem.IsCutoffSelected ? ColorBandSelectionType.Cutoff : ColorBandSelectionType.None;

			if (selectedItem.IsColorSelected)
			{
				result |= ColorBandSelectionType.Color; // | ColorBandSelectionType.EndColor;
			}

			return result;
		}
	}

	public class SelectedColorBand
	{
		public SelectedColorBand(ColorBand colorBand)
		{
			ColorBand = colorBand;
			IsColorSelected = true;
			//IsEndColorSelected = ColorBand.BlendStyle == ColorBandBlendStyle.End;
		}

		public ColorBand ColorBand { get; set; }

		public bool IsCutoffSelected { get; set; }

		public bool IsColorSelected { get; set; }

		//public bool IsEndColorSelected { get; set; }

		public bool IsColorBandSelected => IsCutoffSelected && IsColorSelected;
			//&& (ColorBand.BlendStyle == ColorBandBlendStyle.End && IsEndColorSelected || ColorBand.BlendStyle != ColorBandBlendStyle.End);

	}

	[Flags]
	public enum ColorBandSelectionType
	{
		None = 0,
		Cutoff = 1,
		Color = 2,
		//EndColor = 4,
		//Colors = 6,
		Band = 3
	}
}

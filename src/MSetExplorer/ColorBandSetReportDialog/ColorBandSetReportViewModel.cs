using MSS.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MSetExplorer
{
	public class ColorBandSetReportViewModel : INotifyPropertyChanged
	{
		//private readonly IHistogram _histogram;

		private IColorMapEntry[] _colorMapEntriesRGB;
		private IColorMapEntry[] _colorMapEntriesLCH;
		private readonly int[] _cutoffs;

		private readonly int _highColorBandCutoff;
		private readonly int _highColorBandIndex;

		private ColorMapCountInfo? _selectedColorMapCountInfo;

		#region Constructor

		public ColorBandSetReportViewModel(ColorBandSet colorBandSet, IHistogram histogram)
		{
			ColorBandSet = colorBandSet;
			//_histogram = histogram;

			_colorMapEntriesRGB = colorBandSet.Select(x => new ColorMapEntryRGB(x, false)).ToArray();
			_colorMapEntriesLCH = colorBandSet.Select(x => new ColorMapEntryLCH(x, false)).ToArray();

			_cutoffs = _colorMapEntriesRGB.Take(_colorMapEntriesRGB.Length - 1).Select(x => x.Cutoff).ToArray();

			_highColorBandIndex = _colorMapEntriesRGB.Length - 1;
			_highColorBandCutoff = _colorMapEntriesRGB[^1].Cutoff;

			var cmcInfos = BuildColorMapCountInfos(colorBandSet, histogram);
			ColorMapCountInfos = new ObservableCollection<ColorMapCountInfo>(cmcInfos);
		}

		#endregion

		#region Public Properties

		public ColorBandSet ColorBandSet { get; set; }	

		public ObservableCollection<ColorMapCountInfo> ColorMapCountInfos { get; init; }

		public ColorMapCountInfo? SelectedColorMapCountInfo
		{
			get => _selectedColorMapCountInfo;

			set
			{
				_selectedColorMapCountInfo = value;
				OnPropertyChanged();
			}
		}
		#endregion

		#region Private Methods

		private List<ColorMapCountInfo> BuildColorMapCountInfos(ColorBandSet colorBandSet, IHistogram histogram)
		{
			var result = new List<ColorMapCountInfo>();

			KeyValuePair<int, int>[] keyValuePairs = histogram.GetKeyValuePairs();

			foreach(var kvp in keyValuePairs)
			{
				var countVal = kvp.Key;
				var colorMapIndex = GetColorMapIndex(countVal);

				var rgbCme = _colorMapEntriesRGB[colorMapIndex];
				var lchCme = _colorMapEntriesLCH[colorMapIndex];

				var stepFactor = GetStepFactor(countVal, rgbCme);

				var rgbComps = rgbCme.GetBlendedVals(stepFactor);
				var lchComps = lchCme.GetBlendedVals(stepFactor);

				var cmci = new ColorMapCountInfo(countVal, colorMapIndex, stepFactor, rgbComps[0], rgbComps[1], rgbComps[2], lchComps[0], lchComps[1], lchComps[2]);
				result.Add(cmci);
			}

			return result;
		}

		private int GetColorMapIndex(int countVal)
		{
			int result;

			if (countVal >= _highColorBandCutoff)
			{
				result = _highColorBandIndex;
			}
			else
			{
				// Returns the index to the item with the matched cutoff value
				// or the (bitwise complement of the) index of the item with the smallest cutoff larger than the sought value.
				// If there is no element with a cutoff larger than the sought value, the (bitwise complement of the) 1 + the index of the last item.
				var newIndex = Array.BinarySearch(_cutoffs, countVal);

				result = newIndex < 0 ? ~newIndex : newIndex;
			}

			return result;
		}

		private double GetStepFactor(int countVal, IColorMapEntry cme)
		{
			var bucketDistance = countVal - cme.StartingCutoff;
			var stepFactor = bucketDistance > 0 ? bucketDistance * cme.StepAmount : 0;

			return stepFactor;
		}

		#endregion

		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}

using MSS.Types;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSetExplorer
{
	public class ColorBandSetReportViewModel : INotifyPropertyChanged
	{
		//private readonly IHistogram _histogram;

		private ColorMapCountInfo? _selectedColorMapCountInfo;

		#region Constructor

		public ColorBandSetReportViewModel(ColorBandSet colorBandSet, IHistogram histogram)
		{
			ColorBandSet = colorBandSet;
			//_histogram = histogram;

			var cmcInfos = GetColorMapCountInfos(colorBandSet, histogram);
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

		private List<ColorMapCountInfo> GetColorMapCountInfos(ColorBandSet colorBandSet, IHistogram histogram)
		{
			var result = new List<ColorMapCountInfo>();

			KeyValuePair<int, int>[] keyValuePairs = histogram.GetKeyValuePairs();

			foreach(var kvp in keyValuePairs)
			{
				result.Add(new ColorMapCountInfo(kvp.Key));
			}

			return result;
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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace MSS.Types
{
	public interface IColorBandSet<T> : ICollection<T>, INotifyCollectionChanged, INotifyPropertyChanged where T: IColorBand
	{
		Guid SerialNumber { get; set; }

		T HighColorBand { get; set; }
		int HighCutOff { get; set; }
		ColorBandColor HighStartColor { get; set; }
		ColorBandBlendStyle HighColorBlendStyle { get; set; }
		ColorBandColor HighEndColor { get; set; }

		ObservableCollection<T> ColorBands { get; }

		/// <summary>
		/// Preservers the value of SerialNumber
		/// </summary>
		/// <returns></returns>
		IColorBandSet<T> Clone();

		/// <summary>
		/// Receives a new SerialNumber
		/// </summary>
		/// <returns></returns>
		IColorBandSet<T> CreateNewCopy();
	}
}
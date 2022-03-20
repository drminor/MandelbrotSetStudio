using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MSS.Types
{
	public interface IColorBandSet : ICollection<IColorBand>
	{
		Guid SerialNumber { get; set; }

		//IColorBand HighColorBand { get; set; }
		//int HighCutOff { get; set; }
		//ColorBandColor HighStartColor { get; set; }
		//ColorBandBlendStyle HighColorBlendStyle { get; set; }
		//ColorBandColor HighEndColor { get; set; }

		ObservableCollection<IColorBand> ColorBands { get; }

		/// <summary>
		/// Preservers the value of SerialNumber
		/// </summary>
		/// <returns></returns>
		IColorBandSet Clone();

		/// <summary>
		/// Receives a new SerialNumber
		/// </summary>
		/// <returns></returns>
		IColorBandSet CreateNewCopy();
	}
}
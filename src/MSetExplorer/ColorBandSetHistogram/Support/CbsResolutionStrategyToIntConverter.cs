using MSS.Types.MSet;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MSetExplorer
{
	[ValueConversion(typeof(ColorBandSetResolutionStrategy), typeof(int))]
	internal class CbsResolutionStrategyToIntConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (int)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (ColorBandSetResolutionStrategy)value;
		}
	}
}

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MSetExplorer
{
	[ValueConversion(typeof(string), typeof(FontWeight))]
	public sealed class StringToFontWeightConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is string s)
			{
				return s == "Bold" ? FontWeights.Bold : FontWeights.Normal;
			}
			else
			{
				return FontWeights.Normal;
			}
		}
		
		// You don't need to convert back
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return string.Empty;
		}
	}
}

using MSS.Types;
using System;
using System.Globalization;
using System.Windows.Data;

namespace MSetExplorer
{
    [ValueConversion(typeof(ColorBandBlendStyle), typeof(int))]
    internal class BlendStyleToIntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (int)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (ColorBandBlendStyle)value;
        }
    }
}

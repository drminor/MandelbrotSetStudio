using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    // Uses Byte Array to store color value. The alpha value is always fully opaque, i.e., set to 255.
    public struct ColorBandColor : IEquatable<ColorBandColor>,  IEqualityComparer<ColorBandColor>
	{
        public static readonly ColorBandColor Black = new("#000000");
        public static readonly ColorBandColor White = new("#FFFFFF");

        [JsonConstructor]
        [BsonConstructor]
        public ColorBandColor(string cssColor) : this(GetComps(cssColor))
        {
            //_cssColor = cssColor;
        }

        public ColorBandColor(byte[] colorComps)
        {
            ColorComps = colorComps;
            //_cssColor = null;
        }

		//private string? _cssColor;
		//public string CssColor
		//{
		//	get
		//	{
		//		if (_cssColor == null)
		//		{
		//			_cssColor = GetCssColor(ColorComps);
		//		}
		//		return _cssColor;
		//	}
		//	init
		//	{
		//		_cssColor = null;
		//	}
		//}

        public string GetCssColor()
		{
            return GetCssColor(ColorComps);
		}

        public int GetColorNum()
		{
            return GetColorNum(ColorComps);
		}

        /// <summary>
        /// Array of 3 bytes in RGB order
        /// </summary>
        [BsonIgnore]
        public byte[] ColorComps { get; init; }

        private static string GetCssColor(byte[] cComps)
        {
            // #RRGGBB
            var result = $"#{Get2CharHex(cComps[0])}{Get2CharHex(cComps[1])}{Get2CharHex(cComps[2])}";
            return result;
        }

        private static string Get2CharHex(int c)
        {
            return c.ToString("X", CultureInfo.InvariantCulture).ToLower(CultureInfo.InvariantCulture).PadLeft(2, '0');
        }

        private static int GetColorNum(byte[] cComps)
        {
            var result = 255 << 24;         // Alpha
            result |= cComps[2] << 16;      // Blue
            result |= cComps[1] << 8;       // Green
            result |= cComps[0];            // Red

            return result;
        }

        private static byte[] GetComps(string cssColor)
		{
            var colorComps = new byte[3];
            colorComps[0] = byte.Parse(cssColor.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            colorComps[1] = byte.Parse(cssColor.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            colorComps[2] = byte.Parse(cssColor.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return colorComps;
        }

        public override string? ToString()
        {
            return GetCssColor(ColorComps);
        }

        #region IEquatable and IEqualityComparer Support

        public override bool Equals(object? obj)
		{
			return obj is ColorBandColor color && Equals(color);
		}

		public bool Equals(ColorBandColor other)
		{
            for(var i = 0; i < 3; i++)
			{
                if (ColorComps[i] != other.ColorComps[i])
                {
                    return false;
                }
            }

            return true;
        }

		public override int GetHashCode()
		{
			return HashCode.Combine(ColorComps);
		}

		public bool Equals(ColorBandColor x, ColorBandColor y)
		{
            return x == y;
		}

		public int GetHashCode([DisallowNull] ColorBandColor obj)
		{
            return obj.GetHashCode();
		}

		public static bool operator ==(ColorBandColor left, ColorBandColor right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ColorBandColor left, ColorBandColor right)
		{
			return !(left == right);
		}

		#endregion
	}
}

﻿using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Globalization;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    // Uses Byte Array to store color value. The alpha value is always fully opaque, i.e., set to 255.
    public struct ColorBandColor // : ICloneable 
    {
        public static ColorBandColor Black = new ColorBandColor("#000000");
        public static ColorBandColor White = new ColorBandColor("#FFFFFF");

        [JsonConstructor]
        [BsonConstructor]
        public ColorBandColor(string cssColor) : this(GetComps(cssColor))
        {
            _cssColor = cssColor;
        }

        public ColorBandColor(byte[] colorComps)
        {
            ColorComps = colorComps;
            _cssColor = null;
        }

		private string? _cssColor;
		public string CssColor
		{
			get
			{
				if (_cssColor == null)
				{
					_cssColor = GetCssColor(ColorComps);
				}
				return _cssColor;
			}
			init
			{
				_cssColor = null;
			}
		}

        public string GetCssColor()
		{
            return GetCssColor(ColorComps);
		}

        /// <summary>
        /// Array of 3 bytes in RGB order
        /// </summary>
        [BsonIgnore]
        public byte[] ColorComps { get; init; }

        //private int? _colorNum;

        //public int ColorNum
        //{
        //    get
        //    {
        //        if(!_colorNum.HasValue)
        //        {
        //            _colorNum = GetColorNum(ColorComps);
        //        }
        //        return _colorNum.Value;
        //    }
        //}

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
	}
}

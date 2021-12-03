using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace MSS.Types
{
    public class ColorMapColor
    {
        [JsonConstructor]
        [BsonConstructor]
        public ColorMapColor(string cssColor) : this(GetComps(cssColor))
        {
            _cssColor = cssColor;
        }

        public ColorMapColor(byte[] colorComps)
        {
            ColorComps = new byte[3];
            ColorComps[0] = colorComps[0];
            ColorComps[1] = colorComps[1];
            ColorComps[2] = colorComps[2];

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

        [BsonIgnore]
        public byte[] ColorComps { get; init; }

        private int? _colorNum;

        public int ColorNum
        {
            get
            {
                if(!_colorNum.HasValue)
                {
                    _colorNum = GetColorNum(ColorComps);
                }
                return _colorNum.Value;
            }
        }

        private static string GetCssColor(byte[] cComps)
        {
            string result = $"#{Get2CharHex(cComps[0])}{Get2CharHex(cComps[1])}{Get2CharHex(cComps[2])}";
            return result;
        }

        private static string Get2CharHex(int c)
        {
            return c.ToString("X").ToLower().PadLeft(2, '0');
        }

        private static int GetColorNum(byte[] cComps)
        {
            int result = 255 << 24;
            result |= cComps[2] << 16;
            result |= cComps[1] << 8;
            result |= cComps[0];

            return result;
        }

        private static byte[] GetComps(string cssColor)
		{
            byte[] colorComps = new byte[3];
            colorComps[0] = byte.Parse(cssColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            colorComps[1] = byte.Parse(cssColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            colorComps[2] = byte.Parse(cssColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);

            return colorComps;
        }

    }
}

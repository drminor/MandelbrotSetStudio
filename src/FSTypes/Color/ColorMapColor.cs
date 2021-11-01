using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace FSTypes
{
    public class ColorMapColor
    {
        [BsonConstructor]
        public ColorMapColor(string cssColor) : this(GetComps(cssColor))
        {
            _cssColor = cssColor;
        }

        public ColorMapColor(int[] colorComps)
        {
            ColorComps = new int[3];
            ColorComps[0] = colorComps[0];
            ColorComps[1] = colorComps[1];
            ColorComps[2] = colorComps[2];
        }

        private string _cssColor;
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
        }

        [BsonIgnore]
        [JsonIgnore]
        public int[] ColorComps { get; init; }

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

        private static string GetCssColor(int[] cComps)
        {
            string result = $"F{cComps[0].ToString("X")}{cComps[1].ToString("X")}{cComps[2].ToString("X")}";
            return result;
        }

        private static int GetColorNum(int[] cComps)
        {
            int result = 255 << 24;
            result |= cComps[2] << 16;
            result |= cComps[1] << 8;
            result |= cComps[0];

            return result;
        }

        private static int[] GetComps(string cssColor)
		{
            int[] colorComps = new int[3];
            colorComps[0] = int.Parse(cssColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            colorComps[1] = int.Parse(cssColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            colorComps[2] = int.Parse(cssColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);

            return colorComps;
        }

    }
}

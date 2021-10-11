namespace MFile
{
    public class ColorMapColor
    {
        public readonly int[] ColorComps;

        public ColorMapColor(int[] colorComps)
        {
            ColorComps = new int[3];
            ColorComps[0] = colorComps[0];
            ColorComps[1] = colorComps[1];
            ColorComps[2] = colorComps[2];
            _haveColorNum = false;
        }

        public ColorMapColor(string cssColor)
        {
            ColorComps = new int[3];
            ColorComps[0] = int.Parse(cssColor.Substring(1, 2), System.Globalization.NumberStyles.HexNumber);
            ColorComps[1] = int.Parse(cssColor.Substring(3, 2), System.Globalization.NumberStyles.HexNumber);
            ColorComps[2] = int.Parse(cssColor.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            _haveColorNum = false;
        }

        private readonly bool _haveColorNum;
        private int _colorNum;
        public int ColorNum
        {
            get
            {
                if(!_haveColorNum)
                {
                    _colorNum = GetColorNum(ColorComps);
                }
                return _colorNum;
            }
        }

        private static int GetColorNum(int[] cComps)
        {
            int result = 255 << 24;
            result |= cComps[2] << 16;
            result |= cComps[1] << 8;
            result |= cComps[0];

            return result;
        }

    }
}

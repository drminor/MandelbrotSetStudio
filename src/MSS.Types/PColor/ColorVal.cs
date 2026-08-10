using System;
using System.Globalization;

namespace MSS.Types.PColor
{
	public class ColorVal
	{
		//private static readonly float MIN_VAL = -1e5f;
		//private static readonly float MAX_VAL = 1.499f;

		public static readonly ColorVal Black = new("#000000");
		public static readonly ColorVal White = new("#FFFFFF");

		public float red { get; init; }
		public float green { get; init; }
		public float blue { get; init; }


		public ColorVal()
		{
			red = 0;
			green = 0;
			blue = 0;
		}

		public ColorVal(string cssColor)
		{
			var r = byte.Parse(cssColor.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var g = byte.Parse(cssColor.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var b = byte.Parse(cssColor.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

			red = r / 255f;
			green = g / 255f;
			blue = b / 255f;
		}

		public ColorVal(byte r, byte g, byte b)
		{
			red = r / 255f;
			green = g / 255f;
			blue = b / 255f;
		}

		public ColorVal(float r, float g, float b)
		{
			red = r;
			green = g;
			blue = b;
		}

		//public bool IsValid()
		//{
		//	if (red <= 0 || red > 1) return false;
		//	if (green <= 0 || green > 1) return false;
		//	if (blue <= 0 || blue > 1) return false;


		//	return true;
		//}

		public byte[] Get_sRGB_Vals()
		{
			byte[] result = new byte[3];

			result[0] = GetByteVal(red);
			result[1] = GetByteVal(green);
			result[2] = GetByteVal(blue);

			return result;
		}

		private byte GetByteVal(float f)
		{
			//if (IsValid())
			//{
			//	var t = f * 255;
			//	var r = Math.Round(t);
			//	return (byte)r;
			//}
			//else
			//{
			//	return 0;
			//}

			var t = f * 255;
			var r = Math.Round(t);
			return (byte)r;
		}

		public string GetCssColor()
		{
			var byteVals = Get_sRGB_Vals();
			var result = GetCssColor(byteVals);

			return result;
		}

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



	}
}

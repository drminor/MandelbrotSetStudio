using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MSS.Types.PColor
{
	public class Rgb
	{
		//private static readonly double MIN_VAL = -1e5f;
		//private static readonly double MAX_VAL = 1.499f;

		public static readonly Rgb Black = new("#000000");
		public static readonly Rgb White = new("#FFFFFF");

		public double red { get; init; }
		public double green { get; init; }
		public double blue { get; init; }


		public Rgb()
		{
			red = 0;
			green = 0;
			blue = 0;
		}

		public Rgb(string cssColor)
		{
			var r = byte.Parse(cssColor.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var g = byte.Parse(cssColor.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			var b = byte.Parse(cssColor.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

			red = r / 255d;
			green = g / 255d;
			blue = b / 255d;
		}

		public Rgb(byte r, byte g, byte b)
		{
			red = r / 255d;
			green = g / 255d;
			blue = b / 255d;
		}

		public Rgb(double r, double g, double b)
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

		//private byte GetByteVal(double f)
		//{
		//	//if (IsValid())
		//	//{
		//	//	var t = f * 255;
		//	//	var r = Math.Round(t);
		//	//	return (byte)r;
		//	//}
		//	//else
		//	//{
		//	//	return 0;
		//	//}

		//	var t = f * 255;
		//	var r = Math.Round(t);
		//	return (byte)r;
		//}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private byte GetByteVal(double f)
		{
			return (byte)Math.Round(f * 255);
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

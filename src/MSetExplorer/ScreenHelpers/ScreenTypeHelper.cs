using MSS.Types;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	public static class ScreenTypeHelper
	{
		public static IEnumerable<PointInt> Points(SizeInt size)
		{
			for (var yBlockPtr = 0; yBlockPtr < size.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < size.Width; xBlockPtr++)
				{
					yield return new PointInt(xBlockPtr, yBlockPtr);
				}
			}
		}

		#region Convert to MSS Types

		public static PointInt ConvertToPointInt(Point p)
		{
			return new PointDbl(p.X, p.Y).Round();
		}

		public static SizeDbl ConvertToSizeDbl(Size size)
		{
			return new SizeDbl(size.Width, size.Height);
		}

		public static RectangleDbl ConvertToRectangleDbl(Rect rect)
		{
			return new RectangleDbl(rect.Left, rect.Right, rect.Top, rect.Bottom);
		}

		#endregion

		#region Convert To Widows 

		public static Point ConvertToPoint(PointInt pointInt)
		{
			return new Point(pointInt.X, pointInt.Y);
		}

		public static Size ConvertToSize(SizeInt size)
		{
			return new Size(size.Width, size.Height);
		}

		public static Size ConvertToSize(SizeDbl size)
		{
			return new Size(size.Width, size.Height);
		}

		public static Rect CreateRect(PointInt pointInt, SizeInt sizeInt)
		{
			return new Rect(ConvertToPoint(pointInt), ConvertToSize(sizeInt));
		}

		public static Rect CreateRect(SizeDbl size)
		{
			return new Rect(new Point(), ConvertToSize(size));
		}

		public static Color ConvertToColor(ColorBandColor colorBandColor)
		{
			var result = Color.FromRgb(colorBandColor.ColorComps[0], colorBandColor.ColorComps[1], colorBandColor.ColorComps[2]);

			return result;
		}

		public static Color ConvertToColor(ColorBandColor colorBandColor, double opacity)
		{
			var alpha = 255 * opacity;
			var alphaAsByte = (byte)alpha;
			var result = Color.FromArgb(alphaAsByte, colorBandColor.ColorComps[0], colorBandColor.ColorComps[1], colorBandColor.ColorComps[2]);

			return result;
		}


		#endregion
	}
}

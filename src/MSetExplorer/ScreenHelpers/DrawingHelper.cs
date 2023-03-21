using MSS.Types;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	public static class DrawingHelper
	{
		public static void UpdateRectangleDrawing(GeometryDrawing rectangle, SizeDbl size, ColorBandColor startColor, ColorBandColor endColor, bool horizBlend)
		{
			if (size.Width > 0 && size.Height > 0)
			{
				rectangle.Brush = BuildBrush(startColor, endColor, horizBlend);
				rectangle.Geometry = new RectangleGeometry(ScreenTypeHelper.CreateRect(size));
			}
			else
			{
				rectangle.Brush = Brushes.Transparent;
				rectangle.Geometry = new RectangleGeometry(new Rect(0, 0, 1, 1));
			}
		}

		public static GeometryDrawing BuildRectangle(RectangleDbl rectangleDbl, ColorBandColor startColor, ColorBandColor endColor, bool horizBlend)
		{
			var result = new GeometryDrawing
				(
				BuildBrush(startColor, endColor, horizBlend),
				new Pen(Brushes.Transparent, 0),
				new RectangleGeometry(ScreenTypeHelper.ConvertToRect(rectangleDbl))
				);

			return result;
		}

		public static Brush BuildBrush(ColorBandColor startColor, ColorBandColor endColor, bool horizBlend)
		{
			var startC = ScreenTypeHelper.ConvertToColor(startColor);
			var endC = ScreenTypeHelper.ConvertToColor(endColor);

			Point blendStartPos;
			Point blendEndPos;

			if (horizBlend)
			{
				blendStartPos = new Point(0, 0.5);
				blendEndPos = new Point(1, 0.5);
			}
			else
			{
				blendStartPos = new Point(0.5, 0);
				blendEndPos = new Point(0.5, 1);
			}

			var result = new LinearGradientBrush
				(
				new GradientStopCollection
				{
					new GradientStop(startC, 0.0),
					new GradientStop(startC, 0.15),
					new GradientStop(endC, 0.85),
					new GradientStop(endC, 1.0),

				},
				blendStartPos,
				blendEndPos
				);

			return result;
		}
	}
}

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

		public static GeometryDrawing BuildRectangle(RectangleDbl rectangleDbl, Color fillColor, Color outlineColor)
		{
			var result = new GeometryDrawing
				(
				new SolidColorBrush(fillColor),
				new Pen(new SolidColorBrush(outlineColor), 0.75),
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

		#region Build Brush For Selection Rectangle

		public static DrawingBrush BuildSelectionDrawingBrush()
		{
			var aDrawingGroup = new DrawingGroup();

			var inc = 2;
			var x = 0;
			var y = 0;

			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.Black)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.White));

			x = 0;
			y += inc;
			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.White)); x += inc;
			aDrawingGroup.Children.Add(BuildDot(x, y, 2, Brushes.Black));

			var result = new DrawingBrush(aDrawingGroup)
			{
				TileMode = TileMode.Tile,
				ViewportUnits = BrushMappingMode.Absolute,
				Viewport = new Rect(0, 0, inc * 2, inc * 2)
			};

			return result;
		}

		private static GeometryDrawing BuildDot(int x, int y, int size, SolidColorBrush brush)
		{
			var result = new GeometryDrawing(
				brush,
				new Pen(brush, 0),
				new RectangleGeometry(new Rect(new Point(x, y), new Size(size, size)))
			);

			return result;
		}



		#endregion

		#region Rect and RectanbleDbl Sizing / Shifting 

		public static Rect Shorten(Rect r, double amount)
		{
			var result = new Rect(r.Location, new Size(r.Width - amount, r.Height));
			return result;
		}

		public static Rect MoveRectLeft(Rect r, double amount)
		{
			var result = new Rect(new Point(r.X - amount, r.Y), new Size(r.Width + amount, r.Height));
			return result;
		}

		public static Rect Lengthen(Rect r, double amount)
		{
			var result = new Rect(r.Location, new Size(r.Width + amount, r.Height));
			return result;
		}

		public static Rect MoveRectRight(Rect r, double amount)
		{
			var result = new Rect(new Point(r.X + amount, r.Y), new Size(r.Width - amount, r.Height));
			return result;
		}

		public static RectangleDbl Shorten(RectangleDbl rd, double amount)
		{
			var result = new RectangleDbl(rd.Position, new SizeDbl(rd.Width - amount, rd.Height));
			return result;
		}

		#endregion
	}
}

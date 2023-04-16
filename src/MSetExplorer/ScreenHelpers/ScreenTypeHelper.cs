﻿using MSS.Types;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace MSetExplorer
{
	public static class ScreenTypeHelper
	{
		#region Enumerate over 2-D set of points

		/// <summary>
		/// Produces an enumeration from 0 to the given horizontal and from 0 to the given vertical extent.
		/// Can be used in a foreach statement to iterate over each item. Each column in row 1, then each column in row 2, etc.
		/// </summary>
		/// <param name="size">Specfies the upperbound of the horizontal and vertical extent.</param>
		/// <returns>IEnumerable that returns each item by rows</returns>
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

		/// <summary>
		/// Can be used in a foreach statement to iterate over each item. Each column in row 1, then each column in row 2, etc.
		/// </summary>
		/// <param name="array">Array of rank 2</param>
		/// <returns>IEnumerable that returns each item by rows</returns>
		public static IEnumerable<PointInt> Points(object array)
		{
			if (array is Array s && s.Rank == 2)
			{
				var extents = new SizeInt(s.GetLength(0), s.GetLength(1));
				return Points(extents);
			}
			else
			{
				throw new ArgumentException("The array must have two dimensions.");
			}
		}

		#endregion

		public static bool IsSizeDblChanged(SizeDbl a, SizeDbl b)
		{
			if (a.IsNAN() || b.IsNAN())
			{
				return false;
			}

			return !a.Diff(b).IsNearZero();
		}

		#region Convert to MSS Types

		public static PointInt ConvertToPointInt(Point p)
		{
			return new PointDbl(p.X, p.Y).Round();
		}

		public static SizeInt ConvertToSizeInt(Size size)
		{
			var t = ConvertToSizeDbl(size);
			return t.Round();
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

		public static Point ConvertToPoint(PointDbl pointInt)
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

		public static Vector ConvertToVector(VectorInt vector)
		{
			return new Vector(vector.X, vector.Y);
		}

		public static Rect ConvertToRect(RectangleInt rectangle)
		{
			return new Rect(new Point(rectangle.X1, rectangle.Y1), new Point(rectangle.X2, rectangle.Y2));
		}

		public static Rect ConvertToRect(RectangleDbl rectangle)
		{
			return new Rect(ConvertToPoint(rectangle.Point1), ConvertToPoint(rectangle.Point2));
		}

		public static Rect CreateRect(PointInt pointInt, SizeInt sizeInt)
		{
			return new Rect(ConvertToPoint(pointInt), ConvertToSize(sizeInt));
		}

		public static Rect CreateRect(SizeInt size)
		{
			return new Rect(new Point(), ConvertToSize(size));
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

		//public static System.Drawing.Size ConvertToDrawingSize(SizeDbl size)
		//{
		//	var sizeInt = size.Round();
		//	return new System.Drawing.Size(sizeInt.Width, sizeInt.Height);
		//}

		//public static System.Drawing.Point ConvertToDrawingPoint(PointDbl point)
		//{
		//	var pointInt = point.Round();
		//	return new System.Drawing.Point(pointInt.X, pointInt.Y);
		//}

		#endregion

		#region Rectangle Intersection / BoundingArea Calculations

		public static RectangleDbl Intersect(RectangleDbl a, RectangleDbl b)
		{
			var t = ConvertToRect(a);
			t.Intersect(ConvertToRect(b));
			return ConvertToRectangleDbl(t);
		}

		public static RectangleDbl GetNewBoundingArea(SizeDbl baseSize, VectorDbl beforeOffset, VectorDbl afterOffset)
		{
			var result = GetNewBoundingArea(new RectangleDbl(new PointDbl(), baseSize), beforeOffset, afterOffset);
			return result;
		}

		public static RectangleDbl GetNewBoundingArea(RectangleDbl baseRect, VectorDbl beforeOffset, VectorDbl afterOffset)
		{
			var result = new RectangleDbl(new PointDbl().Translate(beforeOffset), baseRect.Point2.Translate(afterOffset));
			return result;
		}

		#endregion
	}
}

﻿using MSS.Types;
using System.Collections.Generic;
using System.Windows;

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

		public static Rect CreateRect(PointInt pointInt, SizeInt sizeInt)
		{
			return new Rect(ConvertToPoint(pointInt), ConvertToSize(sizeInt));
		}

		#endregion
	}
}
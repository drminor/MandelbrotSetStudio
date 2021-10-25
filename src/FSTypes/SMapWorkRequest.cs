using System;
using System.Diagnostics;

namespace FSTypes
{
	public class SMapWorkRequest
	{
		public string Name;

		public SCoords SCoords;

		public CanvasSize CanvasSize;

		public MapSection Area;

		public int MaxIterations;

		public string ConnectionId;

		public int JobId;

		private SMapWorkRequest()
		{
			Name = null;
			SCoords = null;
			CanvasSize = new CanvasSize(0, 0);
			Area = new MapSection(new Point(0, 0), new CanvasSize(0, 0));
			ConnectionId = null;
			JobId = -1;
		}

		public SMapWorkRequest(string name, SCoords sCoords, CanvasSize canvasSize, MapSection area, int maxIterations, string connectionId)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			SCoords = sCoords ?? throw new ArgumentNullException(nameof(sCoords));
			CanvasSize = canvasSize ?? throw new ArgumentNullException(nameof(canvasSize));
			Area = area ?? throw new ArgumentNullException(nameof(area));
			MaxIterations = maxIterations;
			ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
		}

		public bool RequiresQuadPrecision()
		{
			if (Coords.TryGetFromSCoords(SCoords, out Coords coords))
			{
				if (!HasPrecision(GetSamplePointDiff(coords.LeftBot.X, coords.RightTop.X, CanvasSize.Width))
					|| !HasPrecision(GetSamplePointDiff(coords.LeftBot.Y, coords.RightTop.Y, CanvasSize.Height)))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				// Cannot parse the values -- invalid string values.
				Debug.WriteLine("Cannot parse the SCoords value.");
				return false;
			}
		}

		private static double GetSamplePointDiff(double s, double e, int extent)
		{
			double unit = (e - s) / extent;
			double second = s + unit;
			double diff = second - s;
			return diff;
		}

		private static bool HasPrecision(double x)
		{
			if (x == 0)
				return false;

			if (IsSubnormal(x))
				return false;

			return true;
		}

		const long ExponentMask = 0x7FF0000000000000;
		private static bool IsSubnormal(double v)
		{
			long bithack = BitConverter.DoubleToInt64Bits(v);
			if (bithack == 0) return false;
			return (bithack & ExponentMask) == 0;
		}
	}

}

﻿using MSS.Types;
using MSS.Types.MSetOld;
using System;

namespace MClient
{
	public class SMapWorkRequest
	{
		public string Name;

		public ApCoords Coords;

		public SizeInt CanvasSize;

		public RectangleInt Area;

		public int MaxIterations;

		public string ConnectionId;

		public int JobId;

		private SMapWorkRequest()
		{
			Name = null;
			Coords = null;
			CanvasSize = new SizeInt(0, 0);
			Area = new RectangleInt(new PointInt(0, 0), new SizeInt(0, 0));
			ConnectionId = null;
			JobId = -1;
		}

		public SMapWorkRequest(string name, ApCoords coords, SizeInt canvasSize, RectangleInt area, int maxIterations, string connectionId)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Coords = coords ?? throw new ArgumentNullException(nameof(coords));
			CanvasSize = canvasSize;
			Area = area;
			MaxIterations = maxIterations;
			ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
		}

		// TODO: Implement a SCoords to Coords converter
		public bool RequiresQuadPrecision()
		{
			//if (Coords.TryGetFromSCoords(MqMessages.SCoords, out Coords coords))
			//{
			//	if (!HasPrecision(GetSamplePointDiff(coords.LeftBot.X, coords.RightTop.X, RectangleInt.Width))
			//		|| !HasPrecision(GetSamplePointDiff(coords.LeftBot.Y, coords.RightTop.Y, RectangleInt.Height)))
			//	{
			//		return true;
			//	}
			//	else
			//	{
			//		return false;
			//	}
			//}
			//else
			//{
			//	// Cannot parse the values -- invalid string values.
			//	Debug.WriteLine("Cannot parse the SCoords value.");
			//	return false;
			//}

			return false;
		}

		private double GetSamplePointDiff(double s, double e, int extent)
		{
			double unit = (e - s) / extent;
			double second = s + unit;
			double diff = second - s;
			return diff;
		}


	}

}
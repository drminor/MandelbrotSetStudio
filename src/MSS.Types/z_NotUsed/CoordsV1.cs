using System;

namespace MSS.Types
{
	[Serializable]
	public class CoordsV1
	{
		private SPoint _botLeft;
		private SPoint _topRight;

		public CoordsV1() : this("0", "0", "0", "0")
		{ }

		public CoordsV1(string startingX, string endingX, string startingY, string endingY)
		{
			StartingX = startingX;
			EndingX = endingX;
			StartingY = startingY;
			EndingY = endingY;

			_botLeft = new SPoint(StartingX, StartingY);
			_topRight = new SPoint(EndingX, EndingY);
		}

		//public Coords(double sx, double ex, double sy, double ey) : this()
		//{
		//}

		public string StartingX { get; init; }
		public string EndingX { get; init; }

		public string StartingY { get; init; }
		public string EndingY { get; init; }

		//public double StartingXD { get; init; }
		//public double StartingYD { get; init; }

		//public double EndingXD { get; init; }
		//public double EndingYD { get; init; }

		public SPoint BottomLeft => _botLeft;
		public SPoint TopRight => _topRight;
	}
}

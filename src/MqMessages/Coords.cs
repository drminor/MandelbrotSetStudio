using System;

namespace MqMessages
{
	[Serializable]
	public class Coords
	{
		public Coords()
		{

		}

		public Coords(string sx, string ex, string sy, string ey)
		{
			StartX = sx;
			EndX = ex;
			StartY = sy;
			EndY = ey;
		}

		public string StartX { get; set; }
		public string StartY { get; set; }

		public string EndX { get; set; }
		public string EndY { get; set; }
	}
}

using System;

namespace MClient
{
	public class SCoords
	{
		public SPoint LeftBot;

		public SPoint RightTop;

		private SCoords()
		{
			LeftBot = new SPoint("0", "0");
			RightTop = new SPoint("0", "0");
		}

		public SCoords(SPoint leftBot, SPoint rightTop)
		{
			LeftBot = leftBot ?? throw new ArgumentNullException(nameof(leftBot));
			RightTop = rightTop ?? throw new ArgumentNullException(nameof(rightTop));
		}

		public MqMessages.Coords GetCoords()
		{
			MqMessages.Coords result = new(LeftBot.X, RightTop.X, LeftBot.Y, RightTop.Y);
			return result;
		}

		public override string ToString()
		{
			string result = $"sx:{LeftBot.X}; sy:{LeftBot.Y}; ex:{RightTop.X}; ey:{RightTop.Y}";
			return result;
		}
	}

}

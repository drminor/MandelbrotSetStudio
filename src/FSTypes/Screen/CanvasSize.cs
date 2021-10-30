//using MqMessages;
using System;

namespace FSTypes
{
	public class CanvasSize
	{
        public int Width;

        public int Height;

        private CanvasSize()
        {
            Width = 0;
            Height = 0;
        }

        public CanvasSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

		//public SizeInt GetSizeInt()
		//{
		//	return new SizeInt(Width, Height);
		//}

		public CanvasSize GetWholeUnits(int blockSize)
		{
			CanvasSize result = new()
			{
				Width = (int) Math.Ceiling(Width / (double)blockSize),
				Height = (int) Math.Ceiling(Height / (double)blockSize)
			};
			return result;
		}

    }
}

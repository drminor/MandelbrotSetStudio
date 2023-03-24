using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfDisplayPOC
{
	public class SimpleSkiaCanvas : SkiaControl
	{
		private readonly Field _field = new Field(500);

		protected override void Draw(SKCanvas canvas, int width, int height)
		{
			//canvas.Clear();
			_field.Advance();

			var starColor = new SKColor(255, 60, 90, 255);
			var starPaint = new SKPaint() { IsAntialias = true, Color = starColor };

			foreach (var star in _field.GetStars())
			{
				float xPixel = (float)star.x * width;
				float yPixel = (float)star.y * height;
				float radius = (float)star.size - 1;
				var point = new SKPoint(xPixel, yPixel);
				canvas.DrawCircle(point, radius, starPaint);
			}
		}
	}
}

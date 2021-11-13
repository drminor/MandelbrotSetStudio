using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using qdDotNet;
namespace qdDotNetTests
{
	[TestClass]
	public class CoordMathTests
	{
		[TestMethod]
		public void ZoomInShouldWork()
		{
			FCoordsMath fCoordsMath = new FCoordsMath();
			PointDd start = new PointDd(new Dd(2), new Dd(-1));
			PointDd end = new PointDd(new Dd(4), new Dd(1));

			MCoordsDd coords = new MCoordsDd(start, end);
			SizeInt samplePoints = new SizeInt(100, 100);
			RectangleInt area = new RectangleInt(new PointInt(40, 40), new SizeInt(20, 20));

			MCoordsDd result = fCoordsMath.ZoomIn(coords, samplePoints, area);
		}
	}
}

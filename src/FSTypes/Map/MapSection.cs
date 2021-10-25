using MqMessages;

namespace FSTypes
{
	public class MapSection
	{
		public Point SectionAnchor;

		public CanvasSize CanvasSize;

		private MapSection()
		{
			SectionAnchor = null;
			CanvasSize = null;
		}

		public MapSection(Point sectionAnchor, CanvasSize canvasSize)
		{
			SectionAnchor = sectionAnchor;
			CanvasSize = canvasSize;
		}

		public MapSection(RectangleInt rectangleInt)
		{
			SectionAnchor = new Point(rectangleInt.Point.X, rectangleInt.Point.Y);
			CanvasSize = new CanvasSize(rectangleInt.Size.W, rectangleInt.Size.H);
		}

		public RectangleInt GetRectangleInt()
		{
			return new RectangleInt(SectionAnchor.GetPointInt(), CanvasSize.GetSizeInt());
		}

		public KPoint GetKPoint()
		{
			return SectionAnchor.GetKPoint();
		}
	}
}

using FSTypes;

namespace MClient
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
	}
}

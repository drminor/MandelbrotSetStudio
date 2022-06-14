using MSS.Types;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		SizeInt CanvasSizeInBlocks { get; set; }

		VectorInt SectionIndex { get; } // Just for diagnostics.

		void Draw(PointInt position, byte[] pixels);
		void Redraw(PointInt position);
		bool Hide(MapSection mapSection);

		void HideScreenSections(bool rebuild = false);
		void Shift(VectorInt amount);
	}
}
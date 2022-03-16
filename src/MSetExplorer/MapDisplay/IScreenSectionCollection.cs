using MSS.Types;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		DrawingGroup DrawingGroup { get; }
		SizeInt CanvasSizeInWholeBlocks { get; set; }

		void Draw(PointInt position, byte[] pixels);
		void Redraw(PointInt position);
		bool Hide(MapSection mapSection);

		void HideScreenSections();
		void Shift(VectorInt amount);
	}
}
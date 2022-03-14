using MSS.Types;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		DrawingGroup DrawingGroup { get; }
		SizeInt CanvasSizeInWholeBlocks { get; set; }

		void Draw(MapSection mapSection);
		void Redraw(MapSection mapSection);
		bool Hide(MapSection mapSection);

		void HideScreenSections();
		void Shift(VectorInt amount);
	}
}
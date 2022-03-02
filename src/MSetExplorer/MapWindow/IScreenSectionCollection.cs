using MSS.Types;
using MSS.Types.Screen;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		SizeInt CanvasSizeInBlocks { get; }

		void Draw(MapSection mapSection);
		void HideScreenSections();
		int Shift(VectorInt amount);

		void Test();
	}
}
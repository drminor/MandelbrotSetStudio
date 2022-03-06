using MSS.Types;
using MSS.Types.Screen;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		void Draw(MapSection mapSection);
		bool Hide(MapSection mapSection);

		void HideScreenSections();
		void Shift(VectorInt amount);
	}
}
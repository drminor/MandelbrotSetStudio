using MSS.Types.Screen;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		void Draw(MapSection mapSection);
		void HideScreenSections();
	}
}
using MSS.Types;
using MSS.Types.Screen;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		PointDbl Position { get; set; }
		void Draw(MapSection mapSection);
		void HideScreenSections();
	}
}
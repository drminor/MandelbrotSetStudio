using MSS.Types.Screen;
using System.Windows.Controls;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		Image MapDisplayImage { get; }

		void Draw(MapSection mapSection);
		void HideScreenSections();

		void Test();
	}
}
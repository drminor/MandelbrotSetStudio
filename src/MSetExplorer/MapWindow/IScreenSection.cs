using MSS.Types;

namespace MSetExplorer
{
	internal interface IScreenSection
	{
		void Place(PointInt position);
		void WritePixels(byte[] pixels);
	}
}
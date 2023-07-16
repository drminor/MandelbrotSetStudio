using MSS.Types;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IContentScaler
	{
		SizeDbl ContentViewportSize { get; set; }

		SizeDbl ContentScale { get; set; }

		RectangleDbl? TranslationAndClipSize { get; set; }

	}
}

using MSS.Types;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IContentScaler
	{
		SizeDbl ContentViewportSize { get; set; }

		TranslateTransform TranslateTransform { get; }

		ScaleTransform ScaleTransform { get;  }
	}
}

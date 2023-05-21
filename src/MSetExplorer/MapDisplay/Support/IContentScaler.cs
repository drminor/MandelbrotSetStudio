using MSS.Types;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IContentScaler
	{
		ScaleTransform ScaleTransform { get; set; }

		SizeDbl ContentViewportSize { get; set; }

		VectorDbl ContentOffset { get; set; }
	}
}

using MSS.Types;
using System.Windows.Media;

namespace MSetExplorer
{
	internal interface IContentScaler
	{
		SizeDbl ContentViewportSize { get; set; }

		//ScaleTransform ScaleTransform { get;  }
		SizeDbl ContentScale { get; set; }

		TranslateTransform TranslateTransform { get; }
		//VectorDbl ContentPresenterOffset { get; set; }

		//void Reset(SizeDbl contentViewportSize, SizeDbl contentScale);
	}
}

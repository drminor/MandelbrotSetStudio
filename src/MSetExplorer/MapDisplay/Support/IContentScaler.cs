using MSS.Types;
using System;

namespace MSetExplorer
{
	internal interface IContentScaler
	{
		//SizeDbl ContentViewportSize { get; set; }

		SizeDbl ContentScale { get; set; }

		RectangleDbl TranslationAndClipSize { get; set; }


		// TODO: Update the ViewportSizeChanged event to include the Scale and MinScale as well as the ViewportSize.

		event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;
	}
}

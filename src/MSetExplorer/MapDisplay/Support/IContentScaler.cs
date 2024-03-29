﻿using MSS.Types;
using System;

namespace MSetExplorer
{
	internal interface IContentScaler
	{
		//SizeDbl ContentViewportSize { get; set; }

		SizeDbl ContentScale { get; set; }

		RectangleDbl TranslationAndClipSize { get; set; }

		event EventHandler<ValueTuple<SizeDbl, SizeDbl>>? ViewportSizeChanged;
	}
}

﻿using MSS.Types;
using MSS.Types.Screen;

namespace MSetExplorer
{
	internal interface IScreenSectionCollection
	{
		SizeDbl CanvasOffset { get; set; }

		void Draw(MapSection mapSection);
		void HideScreenSections();
	}
}
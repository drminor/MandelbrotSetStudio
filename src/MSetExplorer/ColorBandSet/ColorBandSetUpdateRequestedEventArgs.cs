using MSS.Types;
using System;

namespace MSetExplorer
{
	public class ColorBandSetUpdateRequestedEventArgs : EventArgs
	{
		public ColorBandSet ColorBandSet { get; init; }
		public bool IsPreview { get; init; }


		public ColorBandSetUpdateRequestedEventArgs(ColorBandSet colorBandSet, bool isPreview)
		{
			ColorBandSet = colorBandSet;
			IsPreview = isPreview;
		}

	}

}

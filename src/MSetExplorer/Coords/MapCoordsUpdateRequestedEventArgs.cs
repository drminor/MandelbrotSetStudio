using MSS.Types;
using System;

namespace MSetExplorer
{
	public class MapCoordsUpdateRequestedEventArgs : EventArgs
	{
		public RRectangle Coords { get; init; }

		public MapCoordsUpdateRequestedEventArgs(RRectangle coords)
		{
			Coords = coords;
		}


	}


}

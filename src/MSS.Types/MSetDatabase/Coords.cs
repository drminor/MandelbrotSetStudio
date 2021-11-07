using System;

namespace MSS.Types.MSetDatabase
{
	[Serializable]
	public record Coords(string Display, RRectangle RRectangle, int ValueDepth)
	{
		public Coords() : this(string.Empty, new RRectangle(), 0)
		{ }
	}
}

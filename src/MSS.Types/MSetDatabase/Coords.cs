using System;

namespace MSS.Types.MSetDatabase
{
	[Serializable]
	public record Coords(string Display, CoordPoints CoordsPoints, int ValueDepth)
	{
		public Coords() : this(string.Empty, new CoordPoints(), 0)
		{ }
	}
}

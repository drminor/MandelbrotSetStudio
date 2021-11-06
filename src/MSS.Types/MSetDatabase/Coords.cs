using System;

namespace MSS.Types.MSetDatabase
{
	[Serializable]
	public record Coords(string[] SCoords, 
		long StartingX, long EndingX, long StartingY, long EndingY, int Exponent, 
		int ValueDepth)
	{

		public Coords() : this(new string[] { "0", "0", "0", "0" }, 0, 0, 0, 0, 0, 0)
		{ }


	}
}

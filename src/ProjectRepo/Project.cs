using MSS.Types;

namespace ProjectRepo
{
	public record Project(string Name, SizeInt CanvasSize, Coords BaseCoords) : RecordBase();

}

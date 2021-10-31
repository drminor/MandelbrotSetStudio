using FSTypes;
using System;

namespace ProjectRepo
{
	public record Project(Guid Id, DateTime DateCreated, string Name,
	SizeInt CanvasSize, MFile.SCoords BaseCoords) : RecordBase(Id, DateCreated, Name);

}

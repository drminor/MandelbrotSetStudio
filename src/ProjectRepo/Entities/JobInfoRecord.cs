using MongoDB.Bson;
using System;

namespace ProjectRepo.Entities
{
	public record JobInfoRecord
	(
		ObjectId Id,
		ObjectId? ParentJobId,
		DateTime DateCreated,
		int TransformType,
		ObjectId SubDivisionId,
		int MapCoordExponent
	);

}

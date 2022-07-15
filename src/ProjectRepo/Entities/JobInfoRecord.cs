using MongoDB.Bson;
using System;

namespace ProjectRepo.Entities
{
	public record JobInfoRecord
	(
		DateTime DateCreated,
		int TransformType,
		ObjectId SubDivisionId,
		int MapCoordExponent
	);

}

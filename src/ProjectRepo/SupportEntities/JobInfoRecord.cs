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

	//public class JobInfoRecord
	//{
	//	public JobInfoRecord(ObjectId id, ObjectId? parentJobId, DateTime dateCreated, int transformType, ObjectId subDivisionId, int mapCoordExponent)
	//	{
	//		Id = id;
	//		ParentJobId = parentJobId;
	//		DateCreated = dateCreated;
	//		TransformType = transformType;
	//		SubDivisionId = subDivisionId;
	//		MapCoordExponent = mapCoordExponent;
	//	}

	//	[BsonId]
	//	[BsonRepresentation(BsonType.ObjectId)]
	//	public ObjectId Id { get; set; }



	//	[BsonRepresentation(BsonType.ObjectId)]
	//	public ObjectId ParentJobId { get; set; }

	//	public DateTime DateCreated { get; set; }
	//	public int TransformType { get; set; }

	//	[BsonRepresentation(BsonType.ObjectId)]
	//	public ObjectId SubDivisionId { get; set; }
	//	public int MapCoordExponent { get; set; }

	//}

}

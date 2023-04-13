using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using System;

namespace ProjectRepo.Entities
{

	// TODO: Change OwnerId to JobId. 
	public record JobMapSectionRecord 
	(
		ObjectId MapSectionId,
		ObjectId SubdivisionId,
		ObjectId OwnerId,
		JobOwnerType OwnerType,
		bool IsInverted,
		BigVectorRecord MapBlockOffset
	)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public bool Onfile => Id != ObjectId.Empty;

		public DateTime LastSaved { get; set; }
	}

}

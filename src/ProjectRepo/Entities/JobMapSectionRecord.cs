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
		bool IsInverted
	)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public bool Onfile => Id != ObjectId.Empty;

		public DateTime LastSaved { get; set; }

		public ObjectId JobId { get; set; }
		public bool RefIsHard { get; set; } = true;
		public DateTime LastSavedUtc { get; set; }

		public BigVectorRecord? MapBlockOffset { get; set; }


	}

}

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	/// <summary>
	/// Record used to store the data found in a MapSectionResponse
	/// </summary>
	public record MapSectionZValuesRecord(
		DateTime DateCreatedUtc,
		ObjectId MapSectionId,

		ZValues ZValues
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

		public DateTime LastSavedUtc { get; set; }
		public DateTime LastAccessed { get; set; }
	}


}

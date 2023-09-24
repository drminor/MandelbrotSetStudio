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
		ObjectId MapSectionId,

		DateTime DateCreatedUtc,
		ZValues ZValues
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

		public DateTime LastSavedUtc { get; set; }
		public DateTime LastAccessedUtc { get; set; }
	}

}

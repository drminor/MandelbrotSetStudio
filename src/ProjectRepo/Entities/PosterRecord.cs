using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record PosterRecord(

		string Name, 
		string? Description,
		ObjectId SourceJobId,
		ObjectId CurrentJobId, 

		VectorIntRecord DisplayPosition,
		double DisplayZoom,

		DateTime DateCreatedUtc,
		DateTime LastSavedUtc,
		DateTime LastAccessedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;
	}
}

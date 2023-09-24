using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using System;

namespace ProjectRepo.Entities
{
	public record PosterRecord(

		string Name, 
		string? Description,
		ObjectId SourceJobId,
		ObjectId CurrentJobId, 

		VectorDblRecord DisplayPosition,
		double DisplayZoom,

		DateTime DateCreatedUtc,
		DateTime LastSavedUtc,
		DateTime LastAccessedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;


		public int Width { get; set; }		// TODO_schema: Use a SizeIntRecord for the PosterRecord.Size property
		public int Height { get; set; }

		public SizeDbl PosterSize => new SizeDbl(Width, Height);

	}
}

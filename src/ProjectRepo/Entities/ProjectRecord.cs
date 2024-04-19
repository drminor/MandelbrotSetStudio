using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	public record ProjectRecord(
		string? Name, 
		string? Description, 
		ObjectId CurrentJobId,

		DateTime DateCreatedUtc,
		DateTime LastSavedUtc,
		DateTime LastAccessedUtc
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public TargetIterationColorMapRecord[]? TargetIterationColorMapRecords { get; set; }

		[BsonDefaultValue(ColorBandSetResolutionStrategy.PerProject)]
		public ColorBandSetResolutionStrategy ColorBandSetResolutionStrategy { get; set; } = ColorBandSetResolutionStrategy.PerProject;

		[BsonDefaultValue(false)]
		[BsonIgnoreIfDefault]
		public bool IsArchived { get; set; } = false;

		[BsonIgnore]
		public string ProjectNameTemporary { get; set; } = RMapConstants.NAME_FOR_NEW_PROJECTS + "-" + Guid.NewGuid().ToString();
	}
}

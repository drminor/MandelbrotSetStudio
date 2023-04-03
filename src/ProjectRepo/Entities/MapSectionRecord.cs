using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MSS.Types;
using MSS.Types.MSet;
using System;

namespace ProjectRepo.Entities
{
	/// <summary>
	/// Record used to store the data found in a MapSectionResponse
	/// </summary>
	public record MapSectionRecord(
		DateTime DateCreatedUtc,
		ObjectId SubdivisionId,
		long BlockPosXHi,
		long BlockPosXLo,
		long BlockPosYHi,
		long BlockPosYLo,

		MapCalcSettings MapCalcSettings,
		bool AllRowsHaveEscaped,
		byte[] Counts,
		byte[] EscapeVelocities
		)
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; init; } = ObjectId.GenerateNewId();

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(128)]
		public int BlockWidth { get; init; } = 128;

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(128)]
		public int BlockHeight { get; init; } = 128;

		[BsonIgnore]
		public SizeInt BlockSize => new SizeInt(BlockWidth, BlockHeight);

		[BsonIgnoreIfDefault]
		[BsonDefaultValue(true)]
		public bool Complete { get; init; } = true;

		public DateTime LastSavedUtc { get; set; }
		public DateTime LastAccessed { get; set; }
	}

}

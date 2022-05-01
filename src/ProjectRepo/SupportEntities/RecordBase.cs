using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo.Entities
{
	//	public record RecordBase()
	//	{
	//		private ObjectId _id;

	//		[BsonId]
	//		[BsonRepresentation(BsonType.ObjectId)]
	//		public ObjectId Id
	//		{
	//			get => _id;
	//			set
	//			{
	//				if (value == ObjectId.Empty)
	//				{
	//					DateCreated = DateTime.UtcNow;
	//				}
	//				else
	//				{
	//					DateCreated = value.CreationTime;
	//				}
	//				_id = value;
	//			}
	//		}

	//		public DateTime DateCreated { get; set; }

	//		public bool Onfile => Id != ObjectId.Empty;

	//	}

	public record RecordBase()
	{
		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id { get; set; } = ObjectId.Empty;

		public DateTime DateCreated => Id.CreationTime;

		public bool Onfile => Id != ObjectId.Empty;
	}


}




using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace ProjectRepo
{
	public record RecordBase()
	{
		private readonly ObjectId _id;

		[BsonId]
		[BsonRepresentation(BsonType.ObjectId)]
		public ObjectId Id
		{
			get => _id;
			init
			{
				_id = ObjectId.GenerateNewId();
			}
		}

		public DateTime DateCreated => _id.CreationTime;
	}

}

using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Project
	{
		public ObjectId Id { get; init; }
		public string Name { get; init; }

		public Project(ObjectId id, string name)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public DateTime DateCreated => Id.CreationTime;
	}
}

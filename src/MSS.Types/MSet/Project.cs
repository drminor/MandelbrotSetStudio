using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Project
	{
		public ObjectId Id { get; init; }
		public string Name { get; init; }
		public string? Description { get; set; }

		public Project(ObjectId id, string name, string? description)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;
		}

		public DateTime DateCreated => Id.CreationTime;
	}
}

using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Project
	{
		public ObjectId Id { get; init; }
		public string Name { get; set; }
		public string? Description { get; set; }

		private ObjectId _currentColorBandSetId;

		public Project(string name, string? description, ObjectId currentColorBandSetId) 
			: this(ObjectId.Empty, name, description, currentColorBandSetId)
		{ }

		public Project(ObjectId id, string name, string? description, ObjectId currentColorBandSetId)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;

			_currentColorBandSetId = currentColorBandSetId;
		}

		public DateTime DateCreated => Id.CreationTime;

		public bool OnFile => Id != ObjectId.Empty;

		public ObjectId CurrentColorBandSetId
		{
			get => _currentColorBandSetId;
			set
			{
				if (value != _currentColorBandSetId)
				{
					_currentColorBandSetId = value;
				}
			}
		}

	}
}

using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Project
	{
		public ObjectId Id { get; init; }
		public string Name { get; set; }
		public string? Description { get; set; }

		public ObjectId? CurrentJobId { get; set; }
		private ObjectId _currentColorBandSetId;

		public Project(string name, string? description, ObjectId? currentJobId, ObjectId currentColorBandSetId) 
			: this(ObjectId.Empty, name, description, currentJobId, currentColorBandSetId)
		{ }

		public Project(ObjectId id, string name, string? description, ObjectId? currentJobId, ObjectId currentColorBandSetId)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;
			CurrentJobId = currentJobId;
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

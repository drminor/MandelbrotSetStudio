using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Project
	{
		public ObjectId Id { get; init; }
		public string Name { get; set; }
		public string? Description { get; set; }

		public DateTime LastUpdated { get; private set; }
		private DateTime _lastSavedUtc;

		public ObjectId? CurrentJobId { get; set; }
		public ObjectId CurrentColorBandSetId { get; set; }

		public Project(string name, string? description, ObjectId? currentJobId, ObjectId currentColorBandSetId) 
			: this(ObjectId.Empty, name, description, DateTime.UtcNow, currentJobId, currentColorBandSetId)
		{ }

		public Project(ObjectId id, string name, string? description, DateTime lastSavedUtc, ObjectId? currentJobId, ObjectId currentColorBandSetId)
		{
			Id = id;
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Description = description;

			LastSavedUtc = lastSavedUtc;
			CurrentJobId = currentJobId;
			CurrentColorBandSetId = currentColorBandSetId;
		}

		public DateTime DateCreated => Id == ObjectId.Empty ? LastSavedUtc : Id.CreationTime;

		public bool OnFile => Id != ObjectId.Empty;

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set
			{
				_lastSavedUtc = value;
				LastUpdated = value;
			}
		}
	}
}

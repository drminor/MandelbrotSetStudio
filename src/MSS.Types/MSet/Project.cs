using MongoDB.Bson;
using System;

namespace MSS.Types.MSet
{
	public class Project
	{
		private string _name;
		private string? _description;
		private ObjectId _currentJobId;
		private DateTime _lastSavedUtc;

		public Project(string name, string? description, ObjectId currentJobId) 
			: this(ObjectId.Empty, name, description, currentJobId, DateTime.MinValue)
		{ }

		public Project(ObjectId id, string name, string? description, ObjectId currentJobId, DateTime lastSavedUtc)
		{
			Id = id;

 			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;
			_currentJobId = currentJobId;

			LastSavedUtc = lastSavedUtc;
		}

		public DateTime DateCreated => Id == ObjectId.Empty ? LastSavedUtc : Id.CreationTime;
		public bool OnFile => Id != ObjectId.Empty;

		public ObjectId Id { get; init; }

		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public string? Description
		{
			get => _description;
			set
			{
				_description = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public ObjectId CurrentJobId
		{
			get => _currentJobId;
			set
			{
				_currentJobId = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
			}
		}

		public DateTime LastUpdatedUtc { get; private set; }
		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;
	}
}

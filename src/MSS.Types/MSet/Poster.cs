using MongoDB.Bson;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class Poster : INotifyPropertyChanged
	{
		private string _name;
		private string? _description;

		private ObjectId _subdivisionId;
		private JobAreaInfo _jobAreaInfo;
		private ObjectId _colorBandSetId;
		private MapCalcSettings _mapCalcSettings;

		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		#region Constructor

		public Poster(string name, string? description, ObjectId? sourceJobId,
			ObjectId subdivisionId, JobAreaInfo jobAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings)
			: this(ObjectId.GenerateNewId(), name, description, sourceJobId,
				  subdivisionId, jobAreaInfo, colorBandSetId, mapCalcSettings,
				  DateTime.UtcNow, DateTime.MinValue, DateTime.MinValue)
		{
			OnFile = false;
		}

		public Poster(ObjectId id, string name, string? description, ObjectId? sourceJobId,
			ObjectId subdivisionId, JobAreaInfo jobAreaInfo, ObjectId colorBandSetId, MapCalcSettings mapCalcSettings,
			DateTime dateCreated, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Id = id;

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			SourceJobId = sourceJobId;

			_subdivisionId = subdivisionId;
			_jobAreaInfo = jobAreaInfo;
			_colorBandSetId = colorBandSetId;
			_mapCalcSettings = mapCalcSettings;

			DateCreatedUtc = dateCreated;
			LastUpdatedUtc = DateTime.MinValue;
			LastSavedUtc = lastSavedUtc;
			LastAccessedUtc = lastAccessedUtc;

			//Debug.WriteLine($"Poster is loaded.");
		}

		#endregion

		#region Public Properties

		public ObjectId Id { get; init; }

		public string Name
		{
			get => _name;
			set
			{
				if (_name != value)
				{
					_name = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public string? Description
		{
			get => _description;
			set
			{
				if (_description != value)
				{
					_description = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public ObjectId? SourceJobId { get; init; }

		public ObjectId SubdivisionId
		{
			get => _subdivisionId;
			set
			{
				if (value != _subdivisionId)
				{
					_subdivisionId = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public JobAreaInfo JobAreaInfo
		{
			get => _jobAreaInfo;
			set
			{
				if (value != _jobAreaInfo)
				{
					_jobAreaInfo = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public ObjectId ColorBandSetId
		{
			get => _colorBandSetId;
			set
			{
				if (value != _colorBandSetId)
				{
					_colorBandSetId = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public MapCalcSettings MapCalcSettings
		{
			get => _mapCalcSettings;
			set
			{
				if (value != _mapCalcSettings)
				{
					_mapCalcSettings = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public DateTime DateCreatedUtc { get; init; }

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			private set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
				OnFile = true;
			}
		}

		public DateTime LastUpdatedUtc
		{
			get => _lastUpdatedUtc;

			private set
			{
				var isDirtyBefore = IsDirty;
				_lastUpdatedUtc = value;

				if (IsDirty != isDirtyBefore)
				{
					OnPropertyChanged(nameof(IsDirty));
				}
			}
		}

		public DateTime LastAccessedUtc { get; init; }

		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;
		public bool OnFile { get; private set; }

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}

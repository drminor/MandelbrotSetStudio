using MongoDB.Bson;
using MSS.Types.MSet;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MSS.Types
{
	public class Poster : INotifyPropertyChanged, ICloneable 
	{
		private string _name;
		private string? _description;

		private JobAreaInfo _jobAreaInfo;
		private ColorBandSet _colorBandSet;
		private MapCalcSettings _mapCalcSettings;

		private VectorInt _dispPosition;
		private double _dispZoom;

		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		#region Constructor

		public Poster(string name, string? description, ObjectId? sourceJobId, JobAreaInfo jobAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings)
			: this(ObjectId.GenerateNewId(), name, description, sourceJobId, jobAreaInfo, colorBandSet, mapCalcSettings, new VectorInt(), 1.0d,
				  DateTime.UtcNow, DateTime.MinValue, DateTime.MinValue)
		{
			OnFile = false;
		}

		public Poster(ObjectId id, string name, string? description, ObjectId? sourceJobId, JobAreaInfo jobAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings,
			VectorInt displayPosition, double displayZoom,
			DateTime dateCreatedUtc, DateTime lastSavedUtc, DateTime lastAccessedUtc)
		{
			Id = id;

			_name = name ?? throw new ArgumentNullException(nameof(name));
			_description = description;

			SourceJobId = sourceJobId;

			_jobAreaInfo = jobAreaInfo;
			_colorBandSet = colorBandSet;
			_mapCalcSettings = mapCalcSettings;

			_dispPosition = displayPosition;
			_dispZoom = displayZoom;

			DateCreatedUtc = dateCreatedUtc;
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

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					_colorBandSet = value;
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

		public VectorInt DisplayPosition
		{
			get => _dispPosition;
			set
			{
				if (value != _dispPosition)
				{
					_dispPosition = value;
					LastUpdatedUtc = DateTime.UtcNow;
					OnPropertyChanged();
				}
			}
		}

		public double DisplayZoom
		{
			get => _dispZoom;
			set
			{
				if (value != _dispZoom)
				{
					_dispZoom = value;
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

		#region Public Methods 

		public bool Save(IProjectAdapter projectAdapter)
		{
			// TODO: Implement Poster.Save
			//if (AnyJobIsDirty && !IsDirty && !(DateCreated > LastSavedUtc))
			//{
			//	Debug.WriteLine("Warning: Project is not marked as 'IsDirty', but one or more of the jobs are dirty.");
			//}

			//projectAdapter.UpdateProjectCurrentJobId(Id, CurrentJobId);
			//if (IsDirty || AnyJobIsDirty)
			//{
			//	SaveColorBandSets(Id, projectAdapter);
			//	SaveJobs(Id, projectAdapter);

			//	LastSavedUtc = DateTime.UtcNow;
			//	_originalCurrentJobId = CurrentJobId;
			//	return true;
			//}
			//else
			//{
			//	Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
			//	return false;
			//}

			return false;
		}

		#endregion

		#region ICloneable Support

		object ICloneable.Clone()
		{
			return Clone();
		}

		Poster Clone()
		{
			return new Poster(Name, Description, SourceJobId, JobAreaInfo.Clone(), ColorBandSet, MapCalcSettings);
		}

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

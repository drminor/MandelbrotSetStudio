using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common.MSet
{
	public class Job : IEquatable<Job?>, IEqualityComparer<Job?>, IComparable<Job?>, ICloneable
	{
		private static readonly Lazy<Job> _lazyJob = new Lazy<Job>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly Job Empty = _lazyJob.Value;

		private ObjectId _ownerId;
		private OwnerType _jobOwnerType;

		private ObjectId? _parentJobId;
		private bool _isOnPreferredPath;
		private ObjectId _colorBandSetId;

		private DateTime _lastSavedUtc;

		#region Constructor

		public Job()
		{
			Id = ObjectId.Empty;
			Label = "Empty";
			MapAreaInfo = new MapAreaInfo2();
			ColorBandSetId = ObjectId.Empty;
			MapCalcSettings = new MapCalcSettings();
			DateCreatedUtc = DateTime.UtcNow;
		}

		public Job(
			ObjectId ownerId,
			OwnerType jobOwnerType,
			ObjectId? parentJobId,
			string? label,
			TransformType transformType,
			RectangleInt? newArea,

			MapAreaInfo2 mapAreaInfo,

			ObjectId colorBandSetId,
			MapCalcSettings mapCalcSettings
			)

			: this(
				  ObjectId.GenerateNewId(),
				  ownerId,
				  jobOwnerType,
				  parentJobId,
				  label ?? transformType.ToString(),
				  transformType,
				  newArea,

				  mapAreaInfo,

				  colorBandSetId,
				  mapCalcSettings,
				  dateCreatedUtc: DateTime.UtcNow,
				  lastSavedUtc: DateTime.UtcNow
				  )
		{
			OnFile = false;
		}

		public Job(
			ObjectId id,
			ObjectId ownerId,
			OwnerType jobOwnerType,
			ObjectId? parentJobId,
			string label,

			TransformType transformType,
			RectangleInt? newArea,

			MapAreaInfo2 mapAreaInfo,

			ObjectId colorBandSetId,
			MapCalcSettings mapCalcSettings,
			DateTime dateCreatedUtc,
			DateTime lastSavedUtc
			)
		{
			if (parentJobId == null && !(TransformType == TransformType.Home/* || transformType == TransformType.CanvasSizeUpdate*/))
			{
				throw new ArgumentException("The ParentJobId can only be null for jobs with TransformType = 'Home.'");
			}

			OnFile = true;

			Id = id;
			_ownerId = ownerId;
			_jobOwnerType = jobOwnerType;
			_parentJobId = parentJobId;
			IsOnPreferredPath = false;

			Label = label;

			TransformType = transformType;
			NewArea = newArea;

			MapAreaInfo = mapAreaInfo;

			_colorBandSetId = colorBandSetId;
			MapCalcSettings = mapCalcSettings;

			DateCreatedUtc = dateCreatedUtc;
			LastSavedUtc = lastSavedUtc;
			LastAccessedUtc = default;
		}

		#endregion

		#region Public Properties

		public Subdivision Subdivision => MapAreaInfo.Subdivision;
		public BigVector MapBlockOffset => MapAreaInfo.MapBlockOffset;
		public VectorInt CanvasControlOffset => MapAreaInfo.CanvasControlOffset;

		public bool IsEmpty => MapAreaInfo.IsEmpty;
		public DateTime DateCreated => Id.CreationTime;
		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;

		public bool OnFile { get; private set; }

		public ObjectId Id { get; init; }

		public DateTime DateCreatedUtc { get; init; }

		public ObjectId OwnerId
		{
			get => _ownerId;
			set
			{
				_ownerId = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public OwnerType JobOwnerType
		{
			get => _jobOwnerType;
			set 
			{
				_jobOwnerType = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public ObjectId? ParentJobId
		{
			get => _parentJobId;
			set
			{   // Only used by JobOwnerHelper.CreateCopy
				_parentJobId = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public bool IsOnPreferredPath
		{
			get => _isOnPreferredPath;
			set
			{
				_isOnPreferredPath = value;
				//LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public string Label { get; init; }

		public TransformType TransformType { get; set; } //TODO: Make this set init.
		
		public RectangleInt? NewArea { get; init; }

		public MapAreaInfo2 MapAreaInfo { get; init; }

		public ObjectId ColorBandSetId
		{
			get => _colorBandSetId;
			set
			{
				if (value != _colorBandSetId)
				{
					_colorBandSetId = value;
					LastUpdatedUtc = DateTime.UtcNow;
				}
			}
		}

		public MapCalcSettings MapCalcSettings { get; init; }

		public IterationUpdateRecord[]? IterationUpdates { get; set; }
		public ColorMapUpdateRecord[]? ColorMapUpdates { get; set; }

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set
			{
				_lastSavedUtc = value;
				_lastUpdatedUtc = value;
				OnFile = true;
			}
		}

		private DateTime _lastUpdatedUtc;

		public DateTime LastUpdatedUtc
		{
			get => _lastUpdatedUtc;
			set => _lastUpdatedUtc = value;
		}

		public DateTime LastAccessedUtc { get; set; }

		#endregion

		#region ToString and ICloneable Support

		public override string ToString()
		{
			if (ParentJobId != null)
			{
				return $"{TransformType} {Id} {ParentJobId} {DateCreated}";
			}
			else
			{
				return $"{TransformType} {Id} {DateCreated}";
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Job Clone()
		{
			var result = new Job(Id, OwnerId, JobOwnerType, ParentJobId, Label, TransformType, NewArea, MapAreaInfo.Clone(),
				ColorBandSetId, MapCalcSettings.Clone(), DateCreatedUtc, LastSavedUtc)
			{
				OnFile = OnFile,
				IsOnPreferredPath = IsOnPreferredPath
			};

			result.IterationUpdates = (IterationUpdateRecord[]?)IterationUpdates?.Clone() ?? null;
			result.ColorMapUpdates = (ColorMapUpdateRecord[]?)ColorMapUpdates?.Clone() ?? null;
			
			return result;
		}

		public Job CreateNewCopy()
		{
			var result = new Job(ObjectId.GenerateNewId(), OwnerId, JobOwnerType, ParentJobId, Label, TransformType, NewArea, MapAreaInfo.Clone(),
				ColorBandSetId, MapCalcSettings.Clone(), DateTime.UtcNow, DateTime.UtcNow)
			{
				OnFile = false,
			};

			result.IterationUpdates = (IterationUpdateRecord[]?) IterationUpdates?.Clone() ?? null;
			result.ColorMapUpdates = (ColorMapUpdateRecord[]?) ColorMapUpdates?.Clone() ?? null;

			return result;
		}

		#endregion

		#region IEqualityComparer, IEquatable, IComparable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as Job);
		}

		public bool Equals(Job? other)
		{
			return other != null && Id.Equals(other.Id);
		}

		public bool Equals(Job? x, Job? y)
		{
			if (x == null)
			{
				return y == null;
			}
			else
			{
				return x.Equals(y);
			}
		}

		public int GetHashCode([DisallowNull] Job? obj)
		{
			return obj.GetHashCode();
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id);
		}

		public static bool operator ==(Job? left, Job? right)
		{
			return EqualityComparer<Job?>.Default.Equals(left, right);
		}

		public static bool operator !=(Job? left, Job? right)
		{
			return !(left == right);
		}

		public int CompareTo(Job? other)
		{
			if (other is null)
			{
				return 1;
			}

			return string.Compare(Id.ToString(), other.Id.ToString(), StringComparison.Ordinal);

		}

		#endregion
	}

}

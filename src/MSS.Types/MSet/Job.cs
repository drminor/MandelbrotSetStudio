using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public class Job : IEquatable<Job?>, IEqualityComparer<Job?>, ICloneable
	{
		private static readonly Lazy<Job> _lazyJob = new Lazy<Job>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly Job Empty = _lazyJob.Value;

		private ObjectId _projectId;
		private ObjectId? _parentJobId;
		private ObjectId _colorBandSetId;

		private bool _isPreferredChild;
		private DateTime _lastSavedUtc;

		#region Constructor

		public Job()
		{
			MapAreaInfo = new MapAreaInfo();
			ColorBandSetId = ObjectId.Empty;
			MapCalcSettings = new MapCalcSettings();
		}

		public Job(
			ObjectId? parentJobId, 
			bool isPreferredChild, 
			ObjectId projectId, 
			string? label, 
			TransformType transformType, 
			RectangleInt? newArea,

			MapAreaInfo mapAreaInfo, 

			SizeInt canvasSizeInBlocks,
			ObjectId colorBandSetId, 
			MapCalcSettings mapCalcSettings
			)

			: this(
				  ObjectId.GenerateNewId(), 
				  parentJobId, 
				  isPreferredChild, 
				  projectId, 
				  label, 
				  transformType, 
				  newArea,

				  mapAreaInfo, 

				  canvasSizeInBlocks,
				  colorBandSetId, 
				  mapCalcSettings, 
				  DateTime.UtcNow
				  )
		{
			OnFile = false;
		}

		public Job(
			ObjectId id,
			ObjectId? parentJobId,
			bool isPreferredChild,
			ObjectId projectId,
			string? label,

			TransformType transformType,
			RectangleInt? newArea,

			MapAreaInfo mapAreaInfo,

			SizeInt canvasSizeInBlocks,
			ObjectId colorBandSetId,
			MapCalcSettings mapCalcSettings,
			DateTime lastSaved
			)
		{
			Id = id;
			_parentJobId = parentJobId;
			_isPreferredChild = isPreferredChild;
			_projectId = projectId;
			Label = label;

			TransformType = transformType;
			NewArea = newArea;

			MapAreaInfo = mapAreaInfo;

			CanvasSizeInBlocks = canvasSizeInBlocks;
			_colorBandSetId = colorBandSetId;
			MapCalcSettings = mapCalcSettings;

			LastSavedUtc = lastSaved;
		}

		#endregion

		#region Public Properties

		public RRectangle Coords => MapAreaInfo.Coords;
		public SizeInt CanvasSize => MapAreaInfo.CanvasSize;
		public Subdivision Subdivision => MapAreaInfo.Subdivision;
		public BigVector MapBlockOffset => MapAreaInfo.MapBlockOffset;
		public VectorInt CanvasControlOffset => MapAreaInfo.CanvasControlOffset;

		public bool IsEmpty => Coords.WidthNumerator == 0;
		public DateTime DateCreated => Id.CreationTime;
		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;

		public bool OnFile { get; private set; }

		public ObjectId Id { get; init; }

		public ObjectId ProjectId
		{
			get => _projectId;
			set
			{
				_projectId = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public ObjectId? ParentJobId
		{
			get => _parentJobId;
			set
			{
				_parentJobId = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public bool IsPreferredChild
		{
			get => _isPreferredChild;
			set
			{
				_isPreferredChild = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public string? Label { get; init; }
		public TransformType TransformType { get; init; }
		public RectangleInt? NewArea { get; init; }

		public MapAreaInfo MapAreaInfo { get; init; }

		public SizeInt CanvasSizeInBlocks { get; init; }

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

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
				OnFile = true;
			}
		}

		public DateTime LastUpdatedUtc { get; private set; }

		#endregion

		#region ICloneable Support

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Job Clone()
		{
			var result = new Job(Id, ParentJobId, IsPreferredChild, ProjectId, Label, TransformType, NewArea, MapAreaInfo.Clone(),
				CanvasSizeInBlocks, ColorBandSetId, MapCalcSettings.Clone(), LastSavedUtc)
			{
				OnFile = OnFile
			};

			return result;
		}

		public Job CreateNewCopy()
		{
			var result = new Job(ObjectId.GenerateNewId(), ParentJobId, IsPreferredChild, ProjectId, Label, TransformType, NewArea, MapAreaInfo.Clone(), 
				CanvasSizeInBlocks, ColorBandSetId, MapCalcSettings.Clone(), DateTime.UtcNow)
			{
				OnFile = false
			};

			return result;
		}

		#endregion

		#region IEqualityComparer / IEquatable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as Job);
		}

		public bool Equals(Job? other)
		{
			//return other != null
			//	&& Id.Equals(other.Id)
			//	&& LastSavedUtc == other.LastSavedUtc;
			
			if (other == null)
			{
				return false;
			}
			else
			{
				if (Id.Equals(other.Id))
				{
					if (LastSavedUtc != other.LastSavedUtc)
					{
						return false;
					}
					else
					{
						return true;
					}
				}
				else
				{
					return false;
				}
			}
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

		#endregion
	}
}

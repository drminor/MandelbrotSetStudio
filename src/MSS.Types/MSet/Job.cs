using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public class Job : IEquatable<Job?>, IEqualityComparer<Job?>, ICloneable
	{
		private static Lazy<Job> _lazyJob = new Lazy<Job>(System.Threading.LazyThreadSafetyMode.PublicationOnly);
		public static readonly Job Empty = _lazyJob.Value;

		private ObjectId _projectId;
		private ObjectId _colorBandSetId;

		private bool _isPreferredChild;
		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		#region Constructor

		public Job()
		{
			Subdivision = new Subdivision();
			Coords = new RRectangle();
			MapCalcSettings = new MapCalcSettings();

			ColorBandSetId = ObjectId.Empty;
			MapBlockOffset = new BigVector();
		}

		public Job(ObjectId? parentJobId, bool isPreferredChild, ObjectId projectId, string? label, TransformType transformType, RectangleInt? newArea, ObjectId colorBandSetId, 
			JobAreaInfo jobAreaInfo, MapCalcSettings mapCalcSettings)
			: this(ObjectId.GenerateNewId(), parentJobId, isPreferredChild, projectId, label, transformType, newArea, colorBandSetId, 
				  jobAreaInfo.Coords, jobAreaInfo.Subdivision, jobAreaInfo.MapBlockOffset, jobAreaInfo.CanvasControlOffset, jobAreaInfo.CanvasSizeInBlocks, 
				  mapCalcSettings, DateTime.UtcNow)
		{ }

		public Job(
			ObjectId id,
			ObjectId? parentJobId,
			bool isPreferredChild,
			ObjectId projectId,
			string? label,

			TransformType transformType,
			RectangleInt? newArea,
			ObjectId colorBandSetId,

			//MSetInfo mSetInfo,
			RRectangle coords,
			Subdivision subdivision,
			BigVector mapBlockOffset,
			VectorInt canvasControlOffset,
			SizeInt canvasSizeInBlocks,
			MapCalcSettings mapCalcSettings,
			DateTime lastSaved
			)
		{
			Id = id;
			ParentJobId = parentJobId;
			_isPreferredChild = isPreferredChild;
			_projectId = projectId;
			Subdivision = subdivision;
			Label = label;

			TransformType = transformType;
			NewArea = newArea;

			//MSetInfo = mSetInfo;
			Coords = coords;
			_colorBandSetId = colorBandSetId;
			CanvasSizeInBlocks = canvasSizeInBlocks;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
			MapCalcSettings = mapCalcSettings;

			LastSavedUtc = lastSaved;
		}

		#endregion

		#region Public Properties

		public bool IsEmpty => Coords.WidthNumerator == 0;
		public DateTime DateCreated => Id.CreationTime;
		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;

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

		public ObjectId? ParentJobId { get; init; }

		public bool IsPreferredChild
		{
			get => _isPreferredChild;
			set
			{
				_isPreferredChild = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public Subdivision Subdivision { get; init; }
		public string? Label { get; init; }
		public TransformType TransformType { get; init; }
		public RectangleInt? NewArea { get; init; }

		//public MSetInfo MSetInfo { get; init; }
		public RRectangle Coords { get; init; }
		public MapCalcSettings MapCalcSettings { get; init; }
	
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

		public SizeInt CanvasSizeInBlocks { get; init; }
		public BigVector MapBlockOffset { get; init; }
		public VectorInt CanvasControlOffset { get; init; }

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set
			{
				_lastSavedUtc = value;
				LastUpdatedUtc = value;
			}
		}

		public DateTime LastUpdatedUtc
		{
			get => _lastUpdatedUtc;

			private set
			{
				//var isDirtyBefore = IsDirty;
				_lastUpdatedUtc = value;

				//if (IsDirty != isDirtyBefore)
				//{
				//	OnPropertyChanged(nameof(IsDirty));
				//}
			}
		}

		#endregion

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Job Clone()
		{
			var result = new Job(Id, ParentJobId, IsPreferredChild, ProjectId, Label, TransformType, NewArea, ColorBandSetId, Coords.Clone(), Subdivision, MapBlockOffset.Clone(), CanvasControlOffset, CanvasSizeInBlocks, MapCalcSettings.Clone(), LastSavedUtc);
			return result;
		}

		#region IEqualityComparer / IEquatable Support

		public override bool Equals(object? obj)
		{
			return Equals(obj as Job);
		}

		public bool Equals(Job? other)
		{
			return other != null
				&& Id.Equals(other.Id)
				&& LastSavedUtc == other.LastSavedUtc;
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

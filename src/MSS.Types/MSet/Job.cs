using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public class Job : IEquatable<Job?>, IEqualityComparer<Job?>, ICloneable
	{
		public ObjectId Id { get; init; }
		public ObjectId? ParentJobId { get; set; }
		public ObjectId ProjectId { get; set; }

		public Subdivision Subdivision { get; init; }
		public string? Label { get; init; }

		public TransformType TransformType { get; init; }
		public RectangleInt? NewArea { get; init; }

		private MSetInfo _mSetInfo;
		private ColorBandSet _colorBandSet;
		private SizeInt _canvasSizeInBlocks;
		private BigVector _mapBlockOffset;
		private VectorInt _canvasControlOffset;

		private bool _isDirty;

		private DateTime _lastSavedUtc;

		private static Lazy<Job> _lazyJob = new Lazy<Job>(System.Threading.LazyThreadSafetyMode.PublicationOnly);

		public static Job Empty = _lazyJob.Value;

		public bool IsEmpty => MSetInfo.Coords.WidthNumerator == 0;

		public Job()
		{
			Subdivision = new Subdivision();
			_mSetInfo = new MSetInfo();
			_colorBandSet = new ColorBandSet();
			_mapBlockOffset = new BigVector();
		}

		public Job(ObjectId? parentJobId, ObjectId projectId, Subdivision subdivision, string? label, TransformType transformType, RectangleInt? newArea, MSetInfo mSetInfo, ColorBandSet colorBandSet,
			SizeInt canvasSizeInBlocks, BigVector mapBlockOffset, VectorInt canvasControlOffset)
			: this(ObjectId.GenerateNewId(), parentJobId, projectId, subdivision, label, transformType, newArea, mSetInfo, colorBandSet, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset, DateTime.UtcNow)
		{ }

		public Job(
			ObjectId id,
			ObjectId? parentJobId,
			ObjectId projectId,
			Subdivision subdivision,
			string? label,

			TransformType transformType,
			RectangleInt? newArea,

			MSetInfo mSetInfo,
			ColorBandSet colorBandSet,
			SizeInt canvasSizeInBlocks,
			BigVector mapBlockOffset,
			VectorInt canvasControlOffset,
			DateTime lastSaved
			)
		{
			Id = id;
			ParentJobId = parentJobId;
			ProjectId = projectId;
			Subdivision = subdivision;
			Label = label;

			TransformType = transformType;
			NewArea = newArea;

			_mSetInfo = mSetInfo;
			_colorBandSet = colorBandSet;
			_canvasSizeInBlocks = canvasSizeInBlocks;
			_mapBlockOffset = mapBlockOffset;
			_canvasControlOffset = canvasControlOffset;
			LastSavedUtc = lastSaved;
		}

		public DateTime DateCreated => Id.CreationTime;
		public DateTime LastUpdatedUtc { get; private set; }

		public bool IsDirty
		{ 
			get
			{
				if (LastUpdatedUtc > DateCreated && !_isDirty)
				{
					Debug.WriteLine($"WARNING: IsDirty = false, but DateCreated is less than LastUpdated.");
				}
				if (LastUpdatedUtc > LastSavedUtc && !_isDirty)
				{
					Debug.WriteLine($"WARNING: IsDirty = false, but LastSaved is less than LastUpdated.");
				}
				return _isDirty;
			}
			set
			{
				_isDirty = value;
			}
		}

		public MSetInfo MSetInfo
		{
			get => _mSetInfo;
			set
			{
				_mSetInfo = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				_colorBandSet = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public SizeInt CanvasSizeInBlocks
		{
			get => _canvasSizeInBlocks;
			set
			{
				_canvasSizeInBlocks = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public BigVector MapBlockOffset
		{
			get => _mapBlockOffset;
			set
			{
				_mapBlockOffset = value;
				LastUpdatedUtc = DateTime.UtcNow;
			}
		}

		public VectorInt CanvasControlOffset
		{
			get => _canvasControlOffset;
			set
			{
				_canvasControlOffset = value;
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

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Job Clone()
		{
			var result = new Job(Id, ParentJobId, ProjectId, Subdivision, Label, TransformType, NewArea, MSetInfo.Clone(), ColorBandSet.Clone(), CanvasSizeInBlocks, MapBlockOffset.Clone(), CanvasControlOffset, LastSavedUtc);
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

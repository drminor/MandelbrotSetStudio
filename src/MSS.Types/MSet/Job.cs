using MongoDB.Bson;
using System;
using System.Collections.Generic;
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
		public RectangleInt NewArea { get; init; }

		//public MSetInfo MSetInfo { get; set; }
		//public SizeInt CanvasSizeInBlocks { get; set; }
		//public BigVector MapBlockOffset { get; set; }
		//public VectorInt CanvasControlOffset { get; set; }

		private MSetInfo _mSetInfo;
		private SizeInt _canvasSizeInBlocks;
		private BigVector _mapBlockOffset;
		private VectorInt _canvasControlOffset;

		public DateTime LastUpdated { get; private set; }
		private DateTime _lastSaved;

		public bool IsDirty { get; set; }

		public Job(ObjectId? parentJobId, ObjectId projectId, Subdivision subdivision, string? label, TransformType transformType, RectangleInt newArea, MSetInfo mSetInfo, 
			SizeInt canvasSizeInBlocks, BigVector mapBlockOffset, VectorInt canvasControlOffset)
			: this(ObjectId.GenerateNewId(), parentJobId, projectId, subdivision, label, transformType, newArea, mSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset, DateTime.MinValue)
		{ }

		public Job(
			ObjectId id,
			ObjectId? parentJobId,
			ObjectId projectId,
			Subdivision subdivision,
			string? label,

			TransformType transformType,
			RectangleInt newArea,

			MSetInfo mSetInfo,
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
			_canvasSizeInBlocks = canvasSizeInBlocks;
			_mapBlockOffset = mapBlockOffset;
			_canvasControlOffset = canvasControlOffset;

			LastSaved = lastSaved;
		}

		public DateTime DateCreated => Id.CreationTime;

		public MSetInfo MSetInfo
		{
			get => _mSetInfo;
			set
			{
				_mSetInfo = value;
				LastUpdated = DateTime.UtcNow;
			}
		}

		public SizeInt CanvasSizeInBlocks
		{
			get => _canvasSizeInBlocks;
			set
			{
				_canvasSizeInBlocks = value;
				LastUpdated = DateTime.UtcNow;
			}
		}

		public BigVector MapBlockOffset
		{
			get => _mapBlockOffset;
			set
			{
				_mapBlockOffset = value;
				LastUpdated = DateTime.UtcNow;
			}
		}

		public VectorInt CanvasControlOffset
		{
			get => _canvasControlOffset;
			set
			{
				_canvasControlOffset = value;
				LastUpdated = DateTime.UtcNow;
			}
		}

		public DateTime LastSaved
		{
			get => _lastSaved;
			set
			{
				_lastSaved = value;
				LastUpdated = value;
			}
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Job Clone()
		{
			var result = new Job(Id, ParentJobId, ProjectId, Subdivision, Label, TransformType, NewArea, MSetInfo, CanvasSizeInBlocks, MapBlockOffset, CanvasControlOffset, LastSaved);
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
				&& LastSaved == other.LastSaved;
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
			if (left is null)
			{
				return right is null;
			}
			else
			{
				return left.Equals(right);
			}
		}

		public static bool operator !=(Job? left, Job? right)
		{
			return !(left == right);
		}

		#endregion
	}
}

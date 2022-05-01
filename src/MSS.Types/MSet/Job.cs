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
		//private ObjectId? _parentJobId;
		private ObjectId _colorBandSetId;

		private bool _isPreferredChild;
		private DateTime _lastUpdatedUtc;
		private DateTime _lastSavedUtc;

		#region Constructor

		public Job()
		{
			Subdivision = new Subdivision();
			MSetInfo = new MSetInfo();
			ColorBandSetId = ObjectId.Empty;
			MapBlockOffset = new BigVector();
		}

		public Job(ObjectId? parentJobId, bool isPreferredChild, ObjectId projectId, Subdivision subdivision, string? label, TransformType transformType, RectangleInt? newArea, MSetInfo mSetInfo, ObjectId colorBandSetId,
			SizeInt canvasSizeInBlocks, BigVector mapBlockOffset, VectorInt canvasControlOffset)
			: this(ObjectId.GenerateNewId(), parentJobId, isPreferredChild, projectId, subdivision, label, transformType, newArea, mSetInfo, colorBandSetId, 
				  canvasSizeInBlocks, mapBlockOffset, canvasControlOffset, DateTime.UtcNow)
		{ }

		public Job(
			ObjectId id,
			ObjectId? parentJobId,
			bool isPreferredChild,
			ObjectId projectId,
			Subdivision subdivision,
			string? label,

			TransformType transformType,
			RectangleInt? newArea,

			MSetInfo mSetInfo,
			ObjectId colorBandSetId,
			SizeInt canvasSizeInBlocks,
			BigVector mapBlockOffset,
			VectorInt canvasControlOffset,
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

			MSetInfo = mSetInfo;
			_colorBandSetId = colorBandSetId;
			CanvasSizeInBlocks = canvasSizeInBlocks;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;

			LastSavedUtc = lastSaved;
		}

		#endregion

		#region Public Properties

		public bool IsEmpty => MSetInfo.Coords.WidthNumerator == 0;
		public DateTime DateCreated => Id.CreationTime;
		public bool IsDirty => LastUpdatedUtc > LastSavedUtc;

		public ObjectId Id { get; init; }

		//public ObjectId ProjectId { get; init; }

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

		//public ObjectId? ParentJobId
		//{
		//	get => _parentJobId;
		//	set
		//	{
		//		if (value == ObjectId.Empty)
		//		{
		//			_parentJobId = null;
		//		}
		//		else
		//		{
		//			_parentJobId = value;
		//		}
		//		LastUpdatedUtc = DateTime.UtcNow;
		//	}
		//}

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
		public MSetInfo MSetInfo { get; init; }

		//public ObjectId ColorBandSetId { get; init; }

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
			var result = new Job(Id, ParentJobId, IsPreferredChild, ProjectId, Subdivision, Label, TransformType, NewArea, MSetInfo.Clone(), ColorBandSetId, CanvasSizeInBlocks, MapBlockOffset.Clone(), CanvasControlOffset, LastSavedUtc);
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

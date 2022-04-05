using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public class Job : IEquatable<Job?>, IEqualityComparer<Job?>, ICloneable
	{
		public ObjectId Id { get; init; }
		public Job? ParentJob { get; set; }
		public Project Project { get; set; }
		public Subdivision Subdivision { get; init; }
		public string? Label { get; init; }

		public TransformType TransformType { get; init; }
		public RectangleInt NewArea { get; init; }

		public MSetInfo MSetInfo { get; set; }
		public SizeInt CanvasSizeInBlocks { get; set; }
		public BigVector MapBlockOffset { get; set; }
		public VectorInt CanvasControlOffset { get; set; }

		public bool IsDirty { get; set; }

		public Job(Job? parentJob, Project project, Subdivision subdivision, string? label, TransformType transformType, RectangleInt newArea, MSetInfo mSetInfo, 
			SizeInt canvasSizeInBlocks, BigVector mapBlockOffset, VectorInt canvasControlOffset)
			: this(ObjectId.GenerateNewId(), parentJob, project, subdivision, label, transformType, newArea, mSetInfo, canvasSizeInBlocks, mapBlockOffset, canvasControlOffset)
		{ }

		public Job(
			ObjectId id,
			Job? parentJob,
			Project project,
			Subdivision subdivision,
			string? label,

			TransformType transformType,
			RectangleInt newArea,

			MSetInfo mSetInfo,
			SizeInt canvasSizeInBlocks,
			BigVector mapBlockOffset,
			VectorInt canvasControlOffset
			)
		{
			Id = id;
			ParentJob = parentJob;
			Project = project ?? throw new ArgumentNullException(nameof(project));
			Subdivision = subdivision;
			Label = label;

			TransformType = transformType;
			NewArea = newArea;

			MSetInfo = mSetInfo;
			CanvasSizeInBlocks = canvasSizeInBlocks;
			MapBlockOffset = mapBlockOffset;
			CanvasControlOffset = canvasControlOffset;
		}

		public DateTime DateCreated => Id.CreationTime;

		object ICloneable.Clone()
		{
			return Clone();
		}

		public Job Clone()
		{
			var result = new Job(Id, ParentJob, Project, Subdivision, Label, TransformType, NewArea, MSetInfo, CanvasSizeInBlocks, MapBlockOffset, CanvasControlOffset);
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
				&& Id.Equals(other.Id);
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

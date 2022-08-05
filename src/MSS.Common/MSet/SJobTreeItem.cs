using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MSS.Common
{
	public class SJobTreeItem : IComparer, IComparer<SJobTreeItem>
	{
		private bool _isActiveAlternateBranchHead;
		private bool _isParkedAlternateBranchHead;

		#region Constructor

		public SJobTreeItem() : this(new Job(), null, false, false)
		{ }

		public SJobTreeItem(Job job) : this(job, null, false, false)
		{ }

		public SJobTreeItem(Job job, SJobTreeItem? parentNode, bool isIterationChange, bool isColorMapChange)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			ParentNode = parentNode;
			Children = new SortedList<ObjectId, SJobTreeItem>();
			IsIterationChange = isIterationChange;
			IsColorMapChange = isColorMapChange;
			AlternateDispSizes = null;
		}

		#endregion

		#region Public Properties

		public bool IsIterationChange { get; set; }

		public bool IsColorMapChange { get; set; }

		public Job Job { get; init; }
		public SortedList<ObjectId, SJobTreeItem> Children { get; init; }

		public List<Job>? AlternateDispSizes { get; private set; }

		public DateTime Created => Job.DateCreated;
		public ObjectId Id => Job.Id;

		public ObjectId? ParentJobId
		{
			get => Job.ParentJobId;
			set => Job.ParentJobId = value;
		}

		public TransformType TransformType
		{
			get => Job.TransformType;
			set => Job.TransformType = value;
		}

		public ObjectId NodeId => Job.Id;
		public SJobTreeItem? ParentNode { get; set; }

		public bool IsActiveAlternateBranchHead
		{
			get => _isActiveAlternateBranchHead;
			set
			{
				if (value != _isActiveAlternateBranchHead)
				{
					_isActiveAlternateBranchHead = value;
				}
			}
		}

		public bool IsParkedAlternateBranchHead
		{
			get => _isParkedAlternateBranchHead;
			set
			{
				if (value != _isParkedAlternateBranchHead)
				{
					_isParkedAlternateBranchHead = value;
				}
			}
		}

		public string? PathHeadType => IsActiveAlternateBranchHead ? "[Alt]" : IsParkedAlternateBranchHead ? "[Prk]" : null;

		#endregion

		#region Public Methods 

		public SJobTreeItem AddJob(Job job)
		{
			var newNode = new SJobTreeItem(job)
			{
				ParentJobId = Id
			};

			Children.Add(newNode.Id, newNode);

			return newNode;
		}

		public void AddCanvasSizeUpdateJob(Job job)
		{
			if (TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException("Cannot add a CanvasSizeUpdate child to a CanvasSizeUpdate JobTreeItem.");
			}

			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"The AddCanvasSizeUpdateJob method was called, but the job's TransformType is {job.TransformType}.");
			}

			if (AlternateDispSizes == null)
			{
				AlternateDispSizes = new List<Job>();
			}

			AlternateDispSizes.Add(job);
		}

		#endregion

		#region IComparer Support
		public int Compare(SJobTreeItem? x, SJobTreeItem? y)
		{
			var x1 = x?.Id.ToString() ?? string.Empty;
			var y1 = y?.Id.ToString() ?? string.Empty;

			return string.Compare(x1, y1, StringComparison.Ordinal);
		}

		public int Compare(object? x, object? y)
		{
			return (x is JobTreeItem a && y is JobTreeItem b)
				? Compare(a, b)
				: 0;
		}

#endregion

	}
}

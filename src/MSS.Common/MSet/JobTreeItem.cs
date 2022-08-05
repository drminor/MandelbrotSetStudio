using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MSS.Common
{
	public class JobTreeItem : INotifyPropertyChanged, IEqualityComparer<JobTreeItem>, IEquatable<JobTreeItem>, IComparer<JobTreeItem>, IComparer
	{
		private bool _isActiveAlternateBranchHead;
		private bool _isParkedAlternateBranchHead;
		private bool _isIterationChange;
		private bool _isColorMapChange;
		private bool _isSelected;
		private bool _isExpanded;

		private readonly ObjectIdComparer _comparer;

		#region Static Methods

		public static JobTreeItem CreateRoot()
		{
			var result = new JobTreeItem();

			Debug.Assert(result.Job.Id == ObjectId.Empty, "Creating a Root JobTreeItem that has a JobId != ObjectId.Empty.");

			return result;
		}

		#endregion

		#region Constructor

		private JobTreeItem()
		{
			IsRoot = true;

			Job = new Job();
			ParentNode = null;
			Children = new ObservableCollection<JobTreeItem>();
			IsIterationChange = false;
			IsColorMapChange = false;
			AlternateDispSizes = null;
			RealChildJobs = new List<Job>();

			_comparer = new ObjectIdComparer();
		}

		private JobTreeItem(Job job, JobTreeItem parentNode, bool isIterationChange, bool isColorMapChange)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			ParentNode = parentNode;
			Children = new ObservableCollection<JobTreeItem>();
			IsIterationChange = isIterationChange;
			IsColorMapChange = isColorMapChange;
			AlternateDispSizes = null;
			RealChildJobs = new List<Job>();

			_comparer = new ObjectIdComparer();
		}

		#endregion

		#region Public Properties

		public Job Job { get; init; }
		public ObservableCollection<JobTreeItem> Children { get; init; }
		public List<JobTreeItem>? AlternateDispSizes { get; private set; }
		public List<Job> RealChildJobs { get; set; }

		#region Convenience Properties

		public int Zoom => -1 * Job.MapAreaInfo.Coords.Exponent;
		public int Iterations => Job.MapCalcSettings.TargetIterations;
		public DateTime Created => Job.DateCreated;

		public ObjectId JobId => IsRoot ? ObjectId.Empty : Job.Id;

		//IsRoot? Children[0].Job Job.ParentJobId;

		public ObjectId? ParentJobId => Job.ParentJobId;

		public TransformType TransformType => Job.TransformType;

		#endregion

		#region Branch Properties

		public bool IsRoot { get; init; }

		public bool IsHome { get; private set; }

		public bool IsOrphan => ParentNode is null;

		public JobTreeItem? ParentNode
		{
			get;
			private set;
		}

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsExpanded
		{
			get => _isExpanded;
			set
			{
				if (value != _isExpanded)
				{
					_isExpanded = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsActiveAlternateBranchHead
		{
			get => _isActiveAlternateBranchHead;
			set
			{
				if (value != _isActiveAlternateBranchHead)
				{
					_isActiveAlternateBranchHead = value;
					OnPropertyChanged();
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
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region UI Properties

		public bool IsIterationChange
		{
			get => _isIterationChange;
			set
			{
				if (value != _isIterationChange)
				{
					_isIterationChange = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsColorMapChange
		{
			get => _isColorMapChange;
			set
			{
				if (value != _isColorMapChange)
				{
					_isColorMapChange = value;
					OnPropertyChanged();
				}
			}
		}

		public string? PathHeadType => IsActiveAlternateBranchHead ? "[Alt]" : IsParkedAlternateBranchHead ? "[Prk]" : null;

		public string IdAndParentId
		{
			get
			{
				var result = Job.Id + ", " + (Job.ParentJobId?.ToString() ?? "null");
				if (IsActiveAlternateBranchHead)
				{
					result += ", Alt: Yes";
				}

				if (IsParkedAlternateBranchHead)
				{
					result += ", Prk: Yes";
				}

				return result;
			}
		}

		#endregion

		#endregion

		#region Public Methods 

		public JobTreeItem AddJob(Job job)
		{
			// TODO: Determine the isIterationChange and isColorMapChange when adding  a new job.
			var newNode = new JobTreeItem(job, this, isIterationChange: false, isColorMapChange: false);
			Add(newNode);

			if (!IsRoot && !IsParkedAlternateBranchHead)
			{
				IsActiveAlternateBranchHead = true;
				newNode.IsParkedAlternateBranchHead = true;
			}

			return newNode;
		}

		public void Add(JobTreeItem jobTreeItem)
		{
			if (jobTreeItem.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Cannot add a JobTreeItem with TransformType = {jobTreeItem.Job.TransformType} to a JobTreeItem.");
			}

			if (jobTreeItem.Job.Id == ObjectId.Empty)
			{
				throw new InvalidOperationException("Cannot add a JobTreeItem with a Job that has an Id == ObjectId.Empty.");
			}

			if (IsRoot && (!Children.Any()))
			{
				jobTreeItem.IsHome = true;
			}

			if (!Children.Any())
			{
				Children.Add(jobTreeItem);
			}
			else
			{
				var index = GetSortPosition(jobTreeItem.Job);
				if (index < 0)
				{
					index = ~index;
				}
				Children.Insert(index, jobTreeItem);
			}

			jobTreeItem.ParentNode = this;
		}

		public bool Move(JobTreeItem destination)
		{
			if (IsRoot)
			{
				throw new InvalidOperationException("Moving the root node is not supported.");
			}

			if (ParentNode is null)
			{
				throw new InvalidOperationException("Cannot move an orphan JobTreeItem.");
			}

			var parentNode = ParentNode;
			var result = parentNode.Remove(this);
			destination.Add(this);

			return result;
		}

		public bool Remove(JobTreeItem jobTreeItem)
		{
			jobTreeItem.ParentNode = null;
			var result = Children.Remove(jobTreeItem);
			return result;
		}

		private int GetSortPosition(Job job)
		{
			var cnt = Children.Count;
			if (cnt == 0)
			{
				return 0;
			}

			if (-1 == _comparer.Compare(Children[^1].Job.Id, job.Id))
			{
				return cnt;
			}

			if (1 == _comparer.Compare(Children[0].Job.Id, job.Id))
			{
				return 0;
			}

			//var index = Children.ToList().BinarySearch(item);
			//return index < 0 ? ~index : index;

			for (var i = 0; i < cnt; i++)
			{
				//if (Children[i].c)
				//if (Children[i].Job.Id.Equals(job.Id.ToString(), StringComparison.Ordinal))
				if (_comparer.Equals(Children[i].Job.Id, job.Id))
				{
					return i;
				}

				if (1 == _comparer.Compare(Children[i].Job.Id, job.Id))
				{
					return ~i;
				}
			}

			return cnt;
		}

		//private int GetSortPosition(ObservableCollection<JobTreeItem> lst, JobTreeItem item)
		//{
		//	if (lst.Count == 0)
		//	{
		//		return 0;
		//	}

		//	if (lst[lst.Count - 1].Created < item.Created)
		//	{
		//		return lst.Count;
		//	}

		//	if (lst[0].Created > item.Created)
		//	{
		//		return 0;
		//	}

		//	int index = lst.ToList().BinarySearch(item);
		//	return index < 0 ? ~index : index;
		//}

		public JobTreeItem AddCanvasSizeUpdateJob(Job job)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Cannot add a jobs with TransformType = {job.TransformType} to the list of AlternateDispSizes.");
			}

			if (AlternateDispSizes == null)
			{
				AlternateDispSizes = new List<JobTreeItem>();
			}

			var newNode = new JobTreeItem(job, this, isIterationChange: false, isColorMapChange: false);

			AlternateDispSizes.Add(newNode);

			return newNode;
		}

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

		#region IComparer and IComparer<JobTreeItem> Support

		public int Compare(JobTreeItem? x, JobTreeItem? y)
		{
			//return string.Compare(x?.JobId.ToString() ?? string.Empty, y?.JobId.ToString() ?? string.Empty, StringComparison.Ordinal);

			return _comparer.Compare(x, y);
		}

		public int Compare(object? x, object? y)
		{
			return (x is JobTreeItem a && y is JobTreeItem b)
				? _comparer.Compare(x, y)
				: 0;
		}

		#endregion

		#region IEqualityComparer and IEquatable Support

		public bool Equals(JobTreeItem? x, JobTreeItem? y)
		{
			return x?.Equals(y) ?? y is null;
		}

		public override bool Equals(object? obj)
		{
			return Equals(obj as JobTreeItem);
		}

		public bool Equals(JobTreeItem? other)
		{
			return other != null &&
				   _comparer.Equals(Job.Id, other.Job.Id);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(JobId);
		}

		public int GetHashCode([DisallowNull] JobTreeItem obj)
		{
			return obj.GetHashCode();
		}

		public static bool operator ==(JobTreeItem? left, JobTreeItem? right)
		{
			return EqualityComparer<JobTreeItem>.Default.Equals(left, right);
		}

		public static bool operator !=(JobTreeItem? left, JobTreeItem? right)
		{
			return !(left == right);
		}

		#endregion
	}

	public class ObjectIdComparer : IComparer<ObjectId>, IComparer, IEqualityComparer<ObjectId>
	{
		public int Compare(object? x, object? y)
		{
			return (x is ObjectId a && y is ObjectId b)
				? Compare(x, y)
				: 0;
		}

		public int Compare(ObjectId x, ObjectId y)
		{
			return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
		}

		public bool Equals(ObjectId x, ObjectId y)
		{
			return 0 == Compare(x, y);
		}

		public int GetHashCode([DisallowNull] ObjectId obj)
		{
			return obj.GetHashCode();
		}
	}

	//public class JobTreeItemObjectIdComparer : IComparer
	//{
	//	public int Compare(object? x, object? y)
	//	{
	//		if (x is JobTreeItem jX && y is JobTreeItem jY)
	//		{
	//			return Compare(jX.JobId, jY.JobId);
	//		}
			
	//		else if (x is JobTreeItem jX2 && y is ObjectId oY)
	//		{
	//			return Compare(jX2.JobId, oY);
	//		}

	//		else
	//		{
	//			return 0;
	//		}
	//	}

	//	private int Compare(ObjectId x, ObjectId y)
	//	{
	//		return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
	//	}

	//}


}

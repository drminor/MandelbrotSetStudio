using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MSS.Common
{
	public class JobTreeItem : INotifyPropertyChanged, IEqualityComparer<JobTreeItem>, IEquatable<JobTreeItem>, IComparable<JobTreeItem>
	{
		private bool _isActiveAlternateBranchHead;
		private bool _isParkedAlternateBranchHead;

		private bool _isIterationChange;
		private bool _isColorMapChange;

		private bool _isCurrent;
		private bool _isExpanded;

		private bool _isSelected;
		private bool _isParentOfSelected;
		private bool _isSiblingOfSelected;
		private bool _isChildOfSelected;
		private string? _itemColor;

		private readonly ObjectIdComparer _comparer;

		#region Static Members

		public static JobTreePath CreateRoot()
		{
			var result = new JobTreePath(new JobTreeItem());

			Debug.Assert(result.Job.Id == ObjectId.Empty, "Creating a Root JobTreeItem that has a JobId != ObjectId.Empty.");

			return result;
		}

		public static string IsSelectedColor { get; set; } = "";
		public static string IsParentSelectedColor { get; set; } = "";
		public static string IsSiblingSelectedColor { get; set; } = "";
		public static string IsChildSelectedColor { get; set; } = "";


		#endregion

		#region Constructor

		private JobTreeItem() : this(new Job(), null, false, false)
		{
			IsRoot = true;
		}

		private JobTreeItem(Job job, JobTreeItem? parentNode, bool isIterationChange, bool isColorMapChange)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			ParentNode = parentNode;
			Children = new ObservableCollection<JobTreeItem>();

			IsIterationChange = isIterationChange;
			IsColorMapChange = isColorMapChange;

			_itemColor = null; //"#20fff8dc";
			_isExpanded = false;

			_isSelected = false;
			_isParentOfSelected = false;
			_isSiblingOfSelected = false;
			_isChildOfSelected = false;

			AlternateDispSizes = null;
			RealChildJobs = new SortedList<ObjectId, Job>(new ObjectIdComparer());
			_comparer = new ObjectIdComparer();
		}

		#endregion

		#region Public Properties

		public Job Job { get; init; }
		public ObservableCollection<JobTreeItem> Children { get; init; }
		public List<JobTreeItem>? AlternateDispSizes { get; private set; }

		public SortedList<ObjectId, Job> RealChildJobs { get; set; }

		#endregion

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

		public bool IsActiveAlternate
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

		public bool IsParkedAlternate
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

		public bool IsCurrent
		{
			get => _isCurrent;
			set
			{
				if (value != _isCurrent)
				{
					_isCurrent = value;
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

		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (value != _isSelected)
				{
					_isSelected = value;
					ItemColor = GetItemColor(_isSelected, _isParentOfSelected, _isSiblingOfSelected, _isChildOfSelected);
					OnPropertyChanged();
				}
			}
		}

		public bool IsParentOfSelected
		{
			get => _isParentOfSelected;
			set
			{
				if (value != _isParentOfSelected)
				{
					_isParentOfSelected = value;
					ItemColor = GetItemColor(_isSelected, _isParentOfSelected, _isSiblingOfSelected, _isChildOfSelected);
					OnPropertyChanged();
				}
			}
		}

		public bool IsSiblingOfSelected
		{
			get => _isSiblingOfSelected;
			set
			{
				if (value != _isSiblingOfSelected)
				{
					_isSiblingOfSelected = value;
					ItemColor = GetItemColor(_isSelected, _isParentOfSelected, _isSiblingOfSelected, _isChildOfSelected);
					OnPropertyChanged();
				}
			}
		}

		public bool IsChildOfSelected
		{
			get => _isChildOfSelected;
			set
			{
				if (value != _isChildOfSelected)
				{
					_isChildOfSelected = value;
					ItemColor = GetItemColor(_isSelected, _isParentOfSelected, _isSiblingOfSelected, _isChildOfSelected);
					OnPropertyChanged();
				}
			}
		}

		public string? ItemColor
		{
			get => _itemColor;
			set
			{
				if (value != _itemColor)
				{
					_itemColor = value;
					OnPropertyChanged();
				}
			}
		}

		public string? PathHeadType => IsActiveAlternate ? "[Alt]" : IsParkedAlternate ? "[Prk]" : null;

		public string IdAndParentId
		{
			get
			{
				var result = Job.Id + ", " + (Job.ParentJobId?.ToString() ?? "null");
				if (IsActiveAlternate)
				{
					result += ", Alt: Yes";
				}

				if (IsParkedAlternate)
				{
					result += ", Prk: Yes";
				}

				return result;
			}
		}

		#endregion

		#region Public Methods 

		public JobTreeItem AddJob(Job job)
		{
			// TODO: Determine the isIterationChange and isColorMapChange when adding  a new job.
			var node = new JobTreeItem(job, this, isIterationChange: false, isColorMapChange: false);

			if (job.TransformType == TransformType.CanvasSizeUpdate)
			{
				AddCanvasSizeUpdateNode(node);
			}
			else
			{
				AddNode(node);

			}

			return node;
		}

		public void AddNode(JobTreeItem node)
		{
			if (node.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Cannot add a JobTreeItem with TransformType = {node.TransformType} to a JobTreeItem.");
			}

			if (node.Job.Id == ObjectId.Empty)
			{
				throw new InvalidOperationException("Cannot add a JobTreeItem with a Job that has an Id == ObjectId.Empty.");
			}

			if (IsRoot && (!Children.Any()))
			{
				node.IsHome = true;
			}

			if (!Children.Any())
			{
				Children.Add(node);
			}
			else
			{
				var index = GetSortPosition(node.Job);
				if (index < 0)
				{
					index = ~index;
				}
				Children.Insert(index, node);
			}

			node.ParentNode = this;

			if (!IsRoot && !IsParkedAlternate)
			{
				IsActiveAlternate = true;
				node.IsParkedAlternate = true;
			}

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
			destination.AddNode(this);

			return result;
		}

		public bool Remove(JobTreeItem jobTreeItem)
		{
			jobTreeItem.ParentNode = null;

			bool result;

			if (jobTreeItem.TransformType == TransformType.CanvasSizeUpdate)
			{
				result = AlternateDispSizes?.Remove(jobTreeItem) ?? false;
			}
			else
			{
				result = Children.Remove(jobTreeItem);
			}

			return result;
		}

		public int GetSortPosition(Job job)
		{
			var cnt = Children.Count;
			if (cnt == 0)
			{
				return 0;
			}

			if (_comparer.Compare(Children[^1].Job.Id, job.Id) < 0)
			{
				return cnt;
			}

			if (_comparer.Compare(Children[0].Job.Id, job.Id) > 0)
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

				if (_comparer.Compare(Children[i].Job.Id, job.Id) > 0)
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
				throw new InvalidOperationException($"Cannot add a JobTreeItem that has a Job with TransformType = {job.TransformType} via call to AddCanvasSizeUpdateNode.");
			}

			var newNode = new JobTreeItem(job, this, isIterationChange: false, isColorMapChange: false);
			AddCanvasSizeUpdateNode(newNode);
			return newNode;
		}

		private void AddCanvasSizeUpdateNode(JobTreeItem node)
		{
			if (AlternateDispSizes == null)
			{
				AlternateDispSizes = new List<JobTreeItem>();
			}

			AlternateDispSizes.Add(node);
		}

		public int AddRealChild(Job job)
		{
			RealChildJobs.Add(job.Id, job);
			var result = RealChildJobs.IndexOfKey(job.Id);
			return result;
		}

		#endregion

		#region Private Methods

		private string? GetItemColor(bool isSelected, bool isParentSelected, bool isSiblingSelected, bool isChildSelected)
		{
			if (isSelected)
			{
				return IsSelectedColor;
			}
			else if (isParentSelected)
			{
				return IsParentSelectedColor;
			}
			else if (isSiblingSelected)
			{
				return IsSiblingSelectedColor;
			}
			else if (isChildSelected)
			{
				return IsChildSelectedColor;
			}
			else
			{
				return null; // "#fff8dc"; // (Cornflower Silk)
			}
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

		//public int Compare(JobTreeItem? x, JobTreeItem? y)
		//{
		//	
		//
		//	return _comparer.Compare(x, y);
		//}

		//public int Compare(object? x, object? y)
		//{
		//	return (x is JobTreeItem a && y is JobTreeItem b)
		//		? _comparer.Compare(x, y)
		//		: 0;
		//}

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

		public int CompareTo(JobTreeItem? other)
		{
			var result = other == null ? 1 : JobId.CompareTo(other.JobId);
			return result;
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

}

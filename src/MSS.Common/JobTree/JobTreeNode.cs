using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

//using JobNodeType = MSS.Types.ITreeNode<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;

namespace MSS.Common
{
	using JobNodeType = ITreeNode<JobTreeNode, Job>;

	public class JobTreeNode : TreeNode<JobTreeNode, Job>, INotifyPropertyChanged, ICloneable
	{
		private static readonly string PreferredPathMark = new('\u0077', 1);

		private bool _isActiveAlternate;
		private bool _isParkedAlternate;

		private bool _isIterationChange;
		private bool _isColorMapChange;

		private bool _isSelected;
		private bool _isParentOfSelected;
		private bool _isSiblingOfSelected;
		private bool _isChildOfSelected;
		private string _itemColor;

		#region Static Properties

		public static string IsSelectedColor { get; set; } = "";
		public static string IsParentSelectedColor { get; set; } = "";
		public static string IsSiblingSelectedColor { get; set; } = "";
		public static string IsChildSelectedColor { get; set; } = "";

		#endregion

		#region Constructor

		public JobTreeNode() : this(null)
		{ }

		public JobTreeNode(Job? job) : this(new Job(), parentNode: null, isIterationChange: false, isColorMapChange: false)
		{
			IsRoot = true;
			if (job != null)
			{
				_ = AddItem(job);
			}
		}

		private JobTreeNode(Job job, JobNodeType? parentNode, bool isIterationChange, bool isColorMapChange)

			: this(job, parentNode, new ObservableCollection<JobNodeType>(), isRoot: false, isHome: false, isCurrent: false, isExpanded: false,
				  isIterationChange, isColorMapChange, isActiveAlternate: false, isParkedAlternate: false, 
				  isSelected: false, isParentOfSelected: false, isSiblingOfSelected: false, isChildOfSelected: false, 
				  alternateDispSizes: null, realChildJobs: new SortedList<ObjectId, Job>(new ObjectIdComparer()), isOnPreferredPath: false)
		{ }

		private JobTreeNode(Job job, JobNodeType? parentNode, ObservableCollection<JobNodeType> children, bool isRoot, bool isHome, bool isCurrent, bool isExpanded,
			bool isIterationChange, bool isColorMapChange, bool isActiveAlternate, bool isParkedAlternate,
			bool isSelected, bool isParentOfSelected, bool isSiblingOfSelected, bool isChildOfSelected, 
			List<JobTreeNode>? alternateDispSizes, SortedList<ObjectId, Job> realChildJobs, bool isOnPreferredPath)

			: base(job, parentNode, isRoot, isHome, isCurrent, isExpanded)
		{
			Children = children;
			Id = job.Id;
			ParentId = job.ParentJobId;

			IsIterationChange = isIterationChange;
			IsColorMapChange = isColorMapChange;

			IsActiveAlternate = isActiveAlternate;
			IsParkedAlternate = isParkedAlternate;

			_isSelected = isSelected;
			_isParentOfSelected = isParentOfSelected;
			_isSiblingOfSelected = isSiblingOfSelected;
			_isChildOfSelected = isChildOfSelected;

			_itemColor = GetItemColor(_isSelected, _isParentOfSelected, _isSiblingOfSelected, _isChildOfSelected);

			AlternateDispSizes = alternateDispSizes;
			RealChildJobs = realChildJobs;
			IsOnPreferredPath = isOnPreferredPath;
		}

		#endregion

		#region Public Properties

		public override ObservableCollection<JobNodeType> Children { get; init; }

		public bool IsOnPreferredPath { get; set; }

		public JobNodeType? PreferredChild
		{
			get => Children.Cast<JobTreeNode>().FirstOrDefault(x => x.IsOnPreferredPath) ?? Children.LastOrDefault();
			set
			{
				var currentValue = PreferredChild;
				if (currentValue != null && currentValue != value)
				{
					currentValue.Node.IsOnPreferredPath = false;
				}

				if (value != null)
				{
					value.Node.IsOnPreferredPath = true;
				}

				if (ParentNode != null)
				{
					ParentNode.Node.PreferredChild = this;
				}
			}
		}

		public List<JobTreeNode>? AlternateDispSizes { get; private set; }

		public SortedList<ObjectId, Job> RealChildJobs { get; set; }

		#endregion

		#region Convenience Properties

		public int Zoom => -1 * Item.MapAreaInfo.Coords.Exponent;
		public int Iterations => Item.MapCalcSettings.TargetIterations;
		public DateTime Created => Item.DateCreated;

		public ObjectId JobId => Item.Id;
		public ObjectId? ParentJobId => Item.ParentJobId;
		public TransformType TransformType => Item.TransformType;

		#endregion

		#region Branch Properties

		public bool IsActiveAlternate
		{
			get => _isActiveAlternate;
			set
			{
				if (value != _isActiveAlternate)
				{
					_isActiveAlternate = value;
					//Item.IsAlternatePathHead = value;
					
					// TODO: Move this code to the Tree class.
					//if (ParentNode != null)
					//{
					//	if (ParentNode.RealChildJobs.TryGetValue(Job.Id, out var parentsRef))
					//	{
					//		parentsRef.IsAlternatePathHead = value;
					//	}
					//	else
					//	{
					//		Debug.WriteLine($"Could not find the Parent's Reference for the Active Alt Node: {Job.Id}. The Active Alt Node's ParentNode has Id: {ParentNode.Item.Id}.");
					//	}
					//}
					OnPropertyChanged();
				}
			}
		}

		public bool IsParkedAlternate
		{
			get => _isParkedAlternate;
			set
			{
				if (value != _isParkedAlternate)
				{
					_isParkedAlternate = value;
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

		public string ItemColor
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

		public string IsOnPreferredPathMarker => IsOnPreferredPath ? PreferredPathMark : " ";

		public string? PathHeadType => IsActiveAlternate ? "[Alt]" : IsParkedAlternate ? "[Prk]" : null;

		public new string IdAndParentId
		{
			get
			{
				var result = Item.Id + ", " + (Item.ParentJobId?.ToString() ?? "null");
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

		public override JobTreeNode AddItem(Job job)
		{
			// TODO: Determine the isIterationChange and isColorMapChange when adding  a new job.
			var node = new JobTreeNode(job, this, isIterationChange: false, isColorMapChange: false);

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

		public override void AddNode(JobNodeType node)
		{
			if (node.Item.TransformType == TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Cannot add a JobTreeItem with TransformType = {node.Item.TransformType} to a JobTreeItem.");
			}

			if (node.Item.Id == ObjectId.Empty)
			{
				throw new InvalidOperationException("Cannot add a JobTreeItem for a Job that has a Empty ObjectId.");
			}

			if (IsRoot && (!Children.Any()))
			{
				node.Node.IsHome = true;
			}

			if (!Children.Any())
			{
				Children.Add(node);
			}
			else
			{
				var index = GetSortPosition(node.Item);
				if (index < 0)
				{
					index = ~index;
				}

				Children.Insert(index, node);
			}

			node.ParentNode = this;

			//if (!IsRoot && !IsParkedAlternate)
			//{
			//	IsActiveAlternate = true;
			//}

			//if (IsActiveAlternate)
			//{
			//	node.Node.IsParkedAlternate = true;
			//}
		}

		public override bool Remove(JobNodeType node)
		{
			node.ParentNode = null;

			bool result;

			if (node.Item.TransformType == TransformType.CanvasSizeUpdate)
			{
				result = AlternateDispSizes?.Remove(node.Node) ?? false;
				return result;
			}

			result = Children.Remove(node);

			//if (IsActiveAlternate && !Children.Any())
			//{
			//	IsActiveAlternate = false;
			//}

			return result;
		}

		public JobTreeNode AddCanvasSizeUpdateJob(Job job)
		{
			if (job.TransformType != TransformType.CanvasSizeUpdate)
			{
				throw new InvalidOperationException($"Cannot add a JobTreeItem that has a Job with TransformType = {job.TransformType} via call to AddCanvasSizeUpdateNode.");
			}

			var newNode = new JobTreeNode(job, this, isIterationChange: false, isColorMapChange: false);
			AddCanvasSizeUpdateNode(newNode);
			return newNode;
		}

		private void AddCanvasSizeUpdateNode(JobTreeNode node)
		{
			if (AlternateDispSizes == null)
			{
				AlternateDispSizes = new List<JobTreeNode>();
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

		private string GetItemColor(bool isSelected, bool isParentSelected, bool isSiblingSelected, bool isChildSelected)
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
				return "#fff8dc";// (Cornflower Silk)
			}
		}

		#endregion

		#region ToString and ICloneable Support

		public override string ToString()
		{
			var sb = new StringBuilder()
				.Append($"TransformType: {Item.TransformType}")
				.Append($" Id: {Id}");

			if (IsHome)
			{
				_ = sb.Append(" [Home]");
			}
			else if (IsRoot)
			{
				_ = sb.Append(" [Root]");
			}
			else
			{
				if (IsActiveAlternate)
				{
					_ = sb.Append(" [Alt]");
				}
				else if (IsParkedAlternate)
				{
					_ = sb.Append(" [Prk]");

				}
				_ = sb.Append($" ParentId: {ParentNode?.Id}");
			}

			_ = sb.Append($" CanvasSizeUpdates: {AlternateDispSizes?.Count ?? 0}")
				.Append($" Children: {Children.Count};")
				.Append($" Real Childern: {RealChildJobs.Count}");

			return sb.ToString();
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public override JobTreeNode Clone()
		{
			var result = new JobTreeNode(Item, ParentNode, Children, IsRoot, IsHome, IsCurrent, IsExpanded, 
				IsIterationChange, IsColorMapChange, IsActiveAlternate, IsParkedAlternate, 
				IsSelected, IsParentOfSelected, IsSiblingOfSelected, IsChildOfSelected, 
				AlternateDispSizes, RealChildJobs, IsOnPreferredPath);

			return result;
		}

		#endregion
	}
}

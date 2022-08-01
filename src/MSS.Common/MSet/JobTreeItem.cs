using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace MSS.Common
{
	public class JobTreeItem : INotifyPropertyChanged
	{
		private bool _isIterationChange;
		private bool _isColorMapChange;
		private bool _isSelected;
		private bool _isExpanded;

		#region Constructor

		public JobTreeItem() : this(new Job(), false, false)
		{ }

		public JobTreeItem(Job job) : this(job, false, false)
		{ }

		public JobTreeItem(Job job, bool isIterationChange, bool isColorMapChange)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			Children = new ObservableCollection<JobTreeItem>();
			IsIterationChange = isIterationChange;
			IsColorMapChange = isColorMapChange;
			AlternateDispSizes = null;
		}

		#endregion

		#region Public Properties

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

		public Job Job { get; init; }
		public ObservableCollection<JobTreeItem> Children { get; init; }

		public List<Job>? AlternateDispSizes { get; private set; }


		public int Zoom => -1 * Job.MapAreaInfo.Coords.Exponent;
		public int Iterations => Job.MapCalcSettings.TargetIterations;
		public DateTime Created => Job.DateCreated;

		public ObjectId? JobId => Job.Id == ObjectId.Empty ? null : Job.Id;

		public ObjectId? ParentJobId
		{
			get => Job.ParentJobId;
			set => Job.ParentJobId = value;
		}

		public TransformType TransformType
		{
			get => Job.TransformType;
			set
			{
				if (value != Job.TransformType)
				{
					Job.TransformType = value;
					OnPropertyChanged();
				}
			}
		}

		public bool IsAlternatePathHead
		{
			get => Job.IsAlternatePathHead;
			set => Job.IsAlternatePathHead = value;
		}

		public bool IsParkedAlternatePathHead => !IsAlternatePathHead && Children.Any() && !Job.IsEmpty;

		public string? PathHeadType => IsAlternatePathHead ? "Alt" : IsParkedAlternatePathHead ? "Prk" : null;

		public string IdAndParentId
		{
			get
			{
				var result = Job.Id + ", " + (Job.ParentJobId?.ToString() ?? "null");
				if (Job.IsAlternatePathHead)
				{
					result += $", Alt: Yes";
				}

				return result;
			}
		}

		#endregion

		#region Public Methods 

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

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}

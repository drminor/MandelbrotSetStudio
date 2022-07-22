using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types.MSet
{
	public class JobTreeItem : INotifyPropertyChanged
	{
		private bool _isSelected;
		private bool _isExpanded;

		#region Constructor

		public JobTreeItem()
		{
			Job = new Job();
			Children = new ObservableCollection<JobTreeItem>();
		}

		public JobTreeItem(Job job, ObservableCollection<JobTreeItem>? children = null)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			Children = children ?? new ObservableCollection<JobTreeItem>();
		}

		#endregion

		#region Public Properties

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

		public string TransformType => Job.TransformType.ToString();
		public int Zoom => -1 * Job.MapAreaInfo.Coords.Exponent;
		public DateTime Created => Job.DateCreated;
		public string Id => Job.Id.ToString();

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

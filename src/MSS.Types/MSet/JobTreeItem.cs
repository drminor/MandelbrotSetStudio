using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types.MSet
{
	public class JobTreeItem : INotifyPropertyChanged
	{
		#region Constructor

		public JobTreeItem(int ordinal, Job job, ObservableCollection<JobTreeItem>? children = null)
		{
			Ordinal = ordinal;
			Job = job ?? throw new ArgumentNullException(nameof(job));
			Children = children ?? new ObservableCollection<JobTreeItem>();
		}

		#endregion

		#region Public Properties

		public int Ordinal { get; set; }
		public Job Job { get; init; }
		public ObservableCollection<JobTreeItem> Children { get; init; }

		public string TransformType => Job.TransformType.ToString();
		public int Zoom => -1 * Job.MapAreaInfo.Subdivision.SamplePointDelta.Exponent;
		public DateTime Created => Job.DateCreated;
		public string Id => Job.Id.ToString();

		#endregion

		#region Property Changed Support

		public event PropertyChangedEventHandler? PropertyChanged;

		//private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		//{
		//	PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		//}

		#endregion
	}
}

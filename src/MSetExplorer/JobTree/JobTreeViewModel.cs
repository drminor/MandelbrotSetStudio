using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSetExplorer
{
	public class JobTreeViewModel : ViewModelBase, IJobTreeViewModel
	{
		private Project? _currentProject;

		#region Constructor

		public JobTreeViewModel()
		{
			JobItems = new ObservableCollection<JobTreeItem>();
			_currentProject = null;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ObservableCollection<JobTreeItem> JobItems { get; init; }


		public Project? CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					if (_currentProject != null)
					{
						_currentProject.PropertyChanged -= CurrentProject_PropertyChanged;
					}

					_currentProject = value;
					LoadTree(_currentProject?.GetJobs().ToList());

					if (_currentProject != null)
					{
						_currentProject.PropertyChanged += CurrentProject_PropertyChanged;
					}

				}
			}
		}

		private void CurrentProject_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Project.CurrentJob))
			{
				OnPropertyChanged(nameof(IJobTreeViewModel.CurrentJob));
			}
		}


		public Job CurrentJob => CurrentProject?.CurrentJob ?? Job.Empty;


		#endregion

		#region Private Methods

		private void LoadTree(IList<Job>? jobs)
		{
			JobItems.Clear();

			if (jobs == null)
			{
				return;
			}

			if (!TryGetRoot(jobs, out var root))
			{
				return;
			}

			JobItems.Add(root);

			var jc = new JobCollection(jobs);

			var firstChild = jobs.OrderBy(x => x.DateCreated).FirstOrDefault(x => x.ParentJobId != null);

			if (firstChild != null)
			{
				jc.MoveCurrentTo(firstChild);
				LoadChildItemsRecurse(jc, root);
			}

			// Add Miscellaneous children of the top node (for example children with TransformType = CanvasSizeUpdate)
			var topLevelNonPreferredJobs = GetTopLevelNonPreferredJobs(jobs);

			var childCntr = root.Children.Count + 1;
			foreach (var job in topLevelNonPreferredJobs)
			{
				root.Children.Add(new JobTreeItem(childCntr++, job));
			}
		}

		private void LoadChildItemsRecurse(JobCollection jc, JobTreeItem jobTreeItem)
		{
			var childCntr = 0;
			var childJobs = GetChildren(jc);
			foreach (var job in childJobs)
			{
				var jobTreeItemChild = new JobTreeItem(childCntr++, job);
				jobTreeItem.Children.Add(jobTreeItemChild);
				LoadChildItemsRecurse(jc, jobTreeItemChild);
			}
		}

		private IList<Job> GetChildren(JobCollection jc)
		{
			//result = jobs.Where(x => x.ParentJobId == jobId).OrderBy(GetSortKey).ToList();
			//result = jp.Jobs.Skip(jp.Ptr).Where(x => x.ParentJobId == jobId).OrderBy(x => x.DateCreated).ToList();

			IList<Job> result;

			if (TryGetNextNonPanJob(jc, out var job))
			{
				result = GetAllChildren(jc, job);
			}
			else
			{
				result = GetAllChildren(jc, null);
			}

			return result;
		}

		private IList<Job> GetAllChildren(JobCollection jc, Job? job)
		{
			var result = new List<Job>();

			var curJob = jc.CurrentJob;

			var jobs = jc.GetJobs().ToList();

			var children = jobs.Where(x => x.ParentJobId == curJob.ParentJobId);
			if (job != null && children.Contains(job))
			{
				return result;
			}
			else
			{
				result.AddRange(children);
			}

			return result;
		}

		private bool TryGetNextNonPanJob(JobCollection jc, [MaybeNullWhen(false)] out Job job)
		{
			if (jc.TryGetNextJob(skipPanJobs: true, out job))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		private bool TryGetRoot(IList<Job> jobs, [MaybeNullWhen(false)] out JobTreeItem root)
		{
			var rootJob = jobs.Where(x => x.TransformType == TransformType.Home).FirstOrDefault();

			if (rootJob == null)
			{
				root = null;
				return false;
			}
			else
			{
				root = new JobTreeItem(0, rootJob);
				return true;
			}
		}

		private IList<Job> GetTopLevelNonPreferredJobs(IList<Job> jobs)
		{
			var result = jobs.Where(x => x.ParentJobId == null && x.TransformType != TransformType.Home).OrderBy(x => x.DateCreated).ToList();
			return result;
		}

		#endregion

		//#region JobsListAndPtr Support Class

		//private class JobListAndPtr
		//{
		//	public JobListAndPtr(IList<Job> jobs)
		//	{
		//		Jobs = jobs;
		//	}

		//	public IList<Job> Jobs { get; init; }
		//	public int Ptr { get; set; }
		//}

		//#endregion
	}
}

/*

	Home,
	CoordinatesUpdate,
	IterationUpdate,
	ColorMapUpdate,
	ZoomIn,
	Pan,
	ZoomOut,
	CanvasSizeUpdate

*/
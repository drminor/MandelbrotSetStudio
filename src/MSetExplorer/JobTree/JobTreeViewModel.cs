using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
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


		public event EventHandler<NavigateToJobRequestedEventArgs>? NavigateToJobRequested;

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

		#region Public Methods

		public void NavigateToJob(string jobId)
		{
			if (CurrentProject != null)
			{
				var oJobId = new ObjectId(jobId);
				var jobs = CurrentProject.GetJobs().ToList();

				var job = jobs.FirstOrDefault(X => X.Id == oJobId);

				if (job != null)
				{
					NavigateToJobRequested?.Invoke(this, new NavigateToJobRequestedEventArgs(job));
				}
			}
		}

		#endregion

		#region Private Methods

		private int visited;

		private void LoadTree(IList<Job>? jobs)
		{
			JobItems.Clear();

			if (jobs == null)
			{
				return;
			}

			var jobTreeItems = GetTree(jobs);

			var totalConsolidated = ConsolidatePans(jobTreeItems);

			Debug.WriteLine($"Consolidated {totalConsolidated} jobs.");

			foreach(var jobTreeItem in jobTreeItems)
			{
				JobItems.Add(jobTreeItem);
			}
		}

		private int ConsolidatePans(IList<JobTreeItem> jobTreeItems)
		{
			var totalResult = 0;

			int currentResult;

			do
			{
				currentResult = 0;

				foreach(var jobTreeItem in jobTreeItems)
				{
					var numConsolidated = ConsolidatePansRecurse(jobTreeItem);
					currentResult += numConsolidated;
					totalResult += numConsolidated;
				}
			}
			while (currentResult > 0);


			return totalResult;
		}

		private int ConsolidatePansRecurse(JobTreeItem jobTreeItem)
		{
			var result = 0;

			for(var j = 0; j < jobTreeItem.Children.Count; j++)
			{
				var child = jobTreeItem.Children[j];
				if (child.Job.TransformType == TransformType.Pan)
				{
					for (var i = 0; i < child.Children.Count; i++)
					{
						var granChild = child.Children[i];
						if (granChild.Job.TransformType == TransformType.Pan)
						{
							_ = child.Children.Remove(granChild);
							jobTreeItem.Children.Add(granChild);
							result++;
						}
					}
				}

				var tResult = ConsolidatePansRecurse(child);
				result += tResult;
			}

			return result;
		}

		private IList<JobTreeItem> GetTree(IList<Job> jobs)
		{
			visited = 0;

			var result = new List<JobTreeItem>();

			if (!TryGetRoot(jobs, out var root))
			{
				return result;
			}

			result.Add(root);
			visited++;

			LoadChildItemsRecurse(jobs, root);

			// Add Miscellaneous children of the top node (for example children with TransformType = CanvasSizeUpdate)
			var topLevelNonPreferredJobs = GetTopLevelNonPreferredJobs(jobs);

			var childCntr = root.Children.Count + 1;
			foreach (var job in topLevelNonPreferredJobs)
			{
				root.Children.Add(new JobTreeItem(childCntr++, job));
				visited++;
			}

			if (visited != jobs.Count)
			{
				Debug.WriteLine("Not all jobs were included.");
			}

			return result;
		}

		private void LoadChildItemsRecurse(IList<Job> jobs, JobTreeItem jobTreeItem)
		{
			var childCntr = 0;
			var childJobs = GetChildren(jobs, jobTreeItem);
			foreach (var job in childJobs)
			{
				var jobTreeItemChild = new JobTreeItem(childCntr++, job);
				jobTreeItem.Children.Add(jobTreeItemChild);
				visited++;
				LoadChildItemsRecurse(jobs, jobTreeItemChild);
			}
		}

		private IList<Job> GetChildren(IList<Job> jobs, JobTreeItem jobTreeItem)
		{
			var result = jobs.Where(x => x.ParentJobId == jobTreeItem.Job.Id).OrderBy(x => x.DateCreated).ToList();
			return result;
		}

		private bool TryGetRoot(IList<Job> jobs, [MaybeNullWhen(false)] out JobTreeItem root)
		{
			var rootJob = jobs.Where(x => x.TransformType == TransformType.Home).FirstOrDefault();

			if (rootJob != null)
			{
				root = new JobTreeItem(0, rootJob);
				return true;
			}
			else
			{
				root = null;
				return false;
			}
		}

		private IList<Job> GetTopLevelNonPreferredJobs(IList<Job> jobs)
		{
			var result = jobs.Where(x => x.ParentJobId == null && x.TransformType != TransformType.Home).OrderBy(x => x.DateCreated).ToList();
			return result;
		}

		#endregion
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
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
		private Job? _currentJob;
		private IList<Job> _jobsList;

		#region Constructor

		public JobTreeViewModel()
		{
			JobItems = new ObservableCollection<JobTreeItem>();
			_currentProject = null;
			_jobsList = new List<Job>();
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
					_jobsList = _currentProject?.GetJobs().ToList() ?? new List<Job>();
					ShowOriginalVersion();

					if (_currentProject != null)
					{
						_currentProject.PropertyChanged += CurrentProject_PropertyChanged;
					}
				}
			}
		}

		public Job? CurrentJob
		{
			get => _currentJob;
			private set
			{
				if (value != _currentJob)
				{
					_currentJob = value;
					ShowJob(_currentJob);
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Event Handlers

		private void CurrentProject_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Project.CurrentJob))
			{
				CurrentJob = CurrentProject?.CurrentJob;
			}
		}

		#endregion

		#region Public Methods

		public void RaiseNavigateToJobRequested(ObjectId jobId)
		{
			if (CurrentProject != null)
			{
				var jobs = CurrentProject.GetJobs().ToList();

				var job = jobs.FirstOrDefault(X => X.Id == jobId);

				if (job != null)
				{
					NavigateToJobRequested?.Invoke(this, new NavigateToJobRequestedEventArgs(job));
				}
			}
		}

		public void ShowOriginalVersion()
		{
			LoadTree(_jobsList);
		}

		public void RollupPans()
		{
			var jobTreeItems = new List<JobTreeItem>(JobItems);
			JobItems.Clear(); 
			
			var totalPansConsolidated = ConsolidatePans(jobTreeItems);
			Debug.WriteLine($"Pans consolidated = {totalPansConsolidated}.");

			foreach (var jobTreeItem in jobTreeItems)
			{
				JobItems.Add(jobTreeItem);
			}
		}

		public void RollupSingles()
		{
			var jobTreeItems = new List<JobTreeItem>(JobItems);
			JobItems.Clear();

			var totalSingesConsolidated = ConsolidateSingles(JobItems);
			Debug.WriteLine($"Singles consolidated = {totalSingesConsolidated}.");

			foreach (var jobTreeItem in jobTreeItems)
			{
				JobItems.Add(jobTreeItem);
			}
		}

		#endregion

		#region Private UI Methods

		private void ShowJob(Job? job)
		{
			if (job == null)
			{
				return;
			}

			var foundNode = JobItems.FirstOrDefault(x => x.Job.Id == job.Id);

			if (foundNode != null)
			{
				foundNode.IsExpanded = true;
				foundNode.IsSelected = true;
			}
			else
			{
				foreach (var jobTreeItem in JobItems)
				{
					if (ShowJobRecurse(job, jobTreeItem))
					{
						jobTreeItem.IsExpanded = true;
					}
				}
			}
		}

		private bool ShowJobRecurse(Job job, JobTreeItem jobTreeItem)
		{
			var foundNode = jobTreeItem.Children.FirstOrDefault(x => x.Job.Id == job.Id);

			if (foundNode != null)
			{
				foundNode.IsExpanded = true;
				foundNode.IsSelected = true;
				return true;
			}
			else
			{
				foreach (var child in jobTreeItem.Children)
				{
					if (ShowJobRecurse(job, child))
					{
						child.IsExpanded = true;
						return true;
					}
				}

				return false;
			}
		}

		#endregion

		#region Private Methods

		private void LoadTree(IList<Job>? jobs)
		{
			JobItems.Clear();

			if (jobs == null)
			{
				return;
			}

			var jobTreeItems = GetTree(jobs);

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

		private int ConsolidateSingles(IList<JobTreeItem> jobTreeItems)
		{
			var totalResult = 0;

			int currentResult;

			do
			{
				currentResult = 0;

				foreach (var jobTreeItem in jobTreeItems)
				{
					var numConsolidated = ConsolidateSinglesRecurse(jobTreeItem);
					currentResult += numConsolidated;
					totalResult += numConsolidated;
				}
			}
			while (currentResult > 0);


			return totalResult;
		}

		private int ConsolidateSinglesRecurse(JobTreeItem jobTreeItem)
		{
			var result = 0;

			for (var j = 0; j < jobTreeItem.Children.Count; j++)
			{
				var child = jobTreeItem.Children[j];
				if (child.Children.Count == 1)
				{
					var granChild = child.Children[0];
					_ = child.Children.Remove(granChild);
					jobTreeItem.Children.Add(granChild);
					result++;
				}

				var tResult = ConsolidateSinglesRecurse(child);
				result += tResult;
			}

			return result;
		}

		private IList<JobTreeItem> GetTree(IList<Job> jobs)
		{
			var visited = 0;

			var result = new List<JobTreeItem>();

			if (!TryGetRoot(jobs, out var root))
			{
				return result;
			}

			result.Add(root);
			visited++;

			LoadChildItemsRecurse(jobs, root, ref visited);

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

		private void LoadChildItemsRecurse(IList<Job> jobs, JobTreeItem jobTreeItem, ref int visited)
		{
			var childCntr = 0;
			var childJobs = GetChildren(jobs, jobTreeItem);
			foreach (var job in childJobs)
			{
				var jobTreeItemChild = new JobTreeItem(childCntr++, job);
				jobTreeItem.Children.Add(jobTreeItemChild);
				visited++;
				LoadChildItemsRecurse(jobs, jobTreeItemChild, ref visited);
			}
		}

		private IList<Job> GetChildren(IList<Job> jobs, JobTreeItem jobTreeItem)
		{
			var result = jobs.Where(x => x.ParentJobId == jobTreeItem.Job.Id).OrderBy(x => x.Id.Timestamp).ToList();
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
			var result = jobs.Where(x => x.ParentJobId == null && x.TransformType != TransformType.Home).OrderBy(x => x.Id.Timestamp).ToList();
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
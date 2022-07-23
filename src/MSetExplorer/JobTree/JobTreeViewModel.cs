using MongoDB.Bson;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace MSetExplorer
{
	public class JobTreeViewModel : ViewModelBase, IJobTreeViewModel
	{
		private Project? _currentProject;

		#region Constructor

		public JobTreeViewModel()
		{
			_currentProject = null;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ObservableCollection<JobTreeItem>? JobItems => CurrentProject?.JobTree.JobItems;

		public Project? CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					_currentProject = value;
					OnPropertyChanged(nameof(IJobTreeViewModel.JobItems));
				}
			}
		}

		public Job? CurrentJob => CurrentProject?.CurrentJob;

		#endregion

		#region Public Methods

		public IReadOnlyCollection<JobTreeItem>? GetCurrentPath()
		{
			return CurrentProject?.GetCurrentPath();
		}

		public IReadOnlyCollection<JobTreeItem>? GetPath(ObjectId jobId)
		{
			return CurrentProject?.GetPath(jobId);
		}

		public bool RaiseNavigateToJobRequested(ObjectId jobId)
		{
			if (CurrentProject != null)
			{
				var job = CurrentProject.GetJob(jobId);

				if (job != null)
				{
					if (CurrentProject.CurrentJob != job)
					{
						CurrentProject.CurrentJob = job;
					}
					else
					{
						Debug.WriteLine($"The Current Job is already {job.Id}.");
					}
				}
			}
		}

		public bool RestoreBranch(ObjectId jobId)
		{
			if (CurrentProject != null)
			{
				var result = CurrentProject.RestoreBranch(jobId);
				return result;
			}
			else
			{
				return false;
			}
		}

		public bool DeleteBranch(ObjectId jobId)
		{
			if (CurrentProject != null)
			{
				var result = CurrentProject.DeleteBranch(jobId);
				return result;
			}
			else
			{
				return false;
			}
		}

		public string GetDetails(ObjectId jobId)
		{
			return $"Getting details for {jobId}.";
		}

		#endregion

	}
}

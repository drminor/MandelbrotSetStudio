using MongoDB.Bson;
using MSS.Types.MSet;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

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

		//public Job? CurrentJob => CurrentProject?.CurrentJob;

		#endregion

		#region Public Methods

		//public void RaiseNavigateToJobRequested(Job job)
		//{
		//	if (CurrentProject != null)
		//	{
		//		CurrentProject.CurrentJob = job;
		//	}
		//}

		public void RaiseNavigateToJobRequested(ObjectId jobId)
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
						//CurrentProject.RefreshCurrentJob();
					}
				}
			}

		}

		#endregion

	}
}

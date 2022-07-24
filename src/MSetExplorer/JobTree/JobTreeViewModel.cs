using MongoDB.Bson;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

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

		public Job? CurrentJob
		{
			get => CurrentProject?.CurrentJob;
			set
			{
				var currentProject = CurrentProject;
				if (currentProject != null)
				{
					if (value == null)
					{
						Debug.WriteLine("Not setting the current job to null.");
						return;
					}

					if (value != currentProject.CurrentJob)
					{
						currentProject.CurrentJob = value;
					}
					else
					{
						Debug.WriteLine($"The Current Job is already {value.Id}.");
					}

					OnPropertyChanged(nameof(IJobTreeViewModel.CurrentJob));
				}
			}
		}

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

		public bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job)
		{
			if (CurrentProject != null)
			{
				job = CurrentProject.GetJob(jobId);
				return job != null;
			}
			else
			{
				job = null;
				return false;
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

		public long DeleteBranch(ObjectId jobId)
		{
			if (CurrentProject != null)
			{
				var result = CurrentProject.DeleteBranch(jobId);
				return result;
			}
			else
			{
				return 0;
			}
		}

		public string GetDetails(ObjectId jobId)
		{
			var path = GetPath(jobId);

			if (path == null)
			{
				return $"Could not find a job with JobId: {jobId}.";
			}

			var jobTreeItem = path.ToArray()[^1];

			var sb = new StringBuilder();

			sb.AppendLine(jobTreeItem.IdAndParentId);
			sb.AppendLine($"AlternatePathHead: {jobTreeItem.IsAlternatePathHead}");

			return sb.ToString();
		}

		#endregion

	}
}

using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace MSetExplorer
{
	public class JobTreeViewModel : ViewModelBase, IJobTreeViewModel
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private Project? _currentProject;

		#region Constructor

		public JobTreeViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_currentProject = null;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ObservableCollection<JobTreeItem>? JobItems => CurrentProject?.JobItems;

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

		public List<JobTreeItem>? GetCurrentPath()
		{
			return CurrentProject?.GetCurrentPath();
		}

		public List<JobTreeItem>? GetPath(ObjectId jobId)
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

		public long DeleteBranch(ObjectId jobId, out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			if (CurrentProject != null)
			{
				var result = 0L;
				var jobAndDescendants = CurrentProject.GetJobAndDescendants(jobId) ?? new List<Job>();

				foreach(var job in jobAndDescendants)
				{
					numberOfMapSectionsDeleted += ProjectAndMapSectionHelper.DeleteJob(job, _projectAdapter, _mapSectionAdapter);
					result++;
				}

				//CurrentProject.GoBack(skipPanJobs: true);

				CurrentProject.DeleteBranch(jobId);

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

			var jobTreeItem = path[^1];

			var sb = new StringBuilder();

			sb.AppendLine($"TransformType: {jobTreeItem.TransformType}");
			sb.AppendLine($"Id: {jobTreeItem.JobId}");
			sb.AppendLine($"ParentId: {jobTreeItem.ParentJobId}");
			sb.AppendLine($"Is Alternate Path Head: {jobTreeItem.IsAlternatePathHead}");
			sb.AppendLine($"Is Parked Alternate: {jobTreeItem.IsParkedAlternatePathHead} ");
			sb.AppendLine($"Children: {jobTreeItem.Children.Count}");
			sb.AppendLine($"CanvasSize Disp Alternates: {jobTreeItem.AlternateDispSizes?.Count ?? 0}");

			if (jobTreeItem.Job.ParentJobId.HasValue)
			{
				var parentPath = GetPath(jobTreeItem.Job.ParentJobId.Value);

				if (parentPath != null)
				{
					var parentJobTreeItem = parentPath[^1];
					var idx = parentJobTreeItem.Children.IndexOf(jobTreeItem);
					var numberOfFollowingNodes = parentJobTreeItem.Children.Count - idx - 1;

					_ = sb.AppendLine($"Following Nodes: {numberOfFollowingNodes}.");
				}
			}

			return sb.ToString();
		}

		#endregion



	}
}

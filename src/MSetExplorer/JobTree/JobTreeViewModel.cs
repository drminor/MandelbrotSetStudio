using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;
using System;
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

		public long DeleteBranch(ObjectId jobId, out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			if (CurrentProject != null)
			{
				var result = 0L;
				var jobAndDescendants = CurrentProject.GetJobAndDescendants(jobId) ?? new List<Job>();

				CurrentProject.GoBack(skipPanJobs: true);

				foreach(var job in jobAndDescendants)
				{
					numberOfMapSectionsDeleted += ProjectAndMapSectionHelper.DeleteJob(job, _projectAdapter, _mapSectionAdapter);
					result++;
				}

				return result;
			}
			else
			{
				return 0;
			}
		}

		//public long DeleteBranch(ObjectId jobId, IMapSectionDeleter mapSectionDeleter)
		//{
		//	if (!TryFindJobTreeItem(jobId, _root, out var path))
		//	{
		//		throw new InvalidOperationException($"Cannot find job: {jobId} that is being deleted.");
		//	}

		//	Debug.WriteLine($"Deleting all jobs in branch anchored by job: {jobId}.");

		//	var currentItem = path[^1];
		//	var jobs = GetJobs(currentItem).ToList();

		//	var result = 0L;
		//	foreach (var job in jobs)
		//	{
		//		var numberDeleted = mapSectionDeleter.DeleteMapSectionsForJob(job.Id, JobOwnerType.Project);
		//		if (numberDeleted.HasValue)
		//		{
		//			result += numberDeleted.Value;
		//		}
		//	}

		//	Debug.WriteLine($"{result} jobs were deleted.");

		//	var parentPath = GetParentPath(path);

		//	if (parentPath != null)
		//	{
		//		var parentItem = parentPath[^1];
		//		_ = parentItem.Children.Remove(currentItem);
		//	}

		//	return result;
		//}

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

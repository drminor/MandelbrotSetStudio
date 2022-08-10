using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
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
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private IJobOwner? _currentProject;

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

		public IJobOwner? CurrentProject
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

		public JobTreeItem? SelectedViewItem
		{
			get => CurrentProject?.SelectedViewItem;
			set
			{
				if (CurrentProject != null)
				{
					CurrentProject.SelectedViewItem = value;
				}
			}
		}

		#endregion

		#region Public Methods

		public JobTreePath? GetCurrentPath()
		{
			return CurrentProject?.GetCurrentPath();
		}

		public JobTreePath? GetPath(ObjectId jobId)
		{
			return CurrentProject?.GetPath(jobId);
		}

		public bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job)
		{
			job = CurrentProject?.GetPath(jobId)?.LastTerm?.Job;
			return job != null;
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

				_ = CurrentProject.DeleteBranch(jobId);

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

			if (path == null || path.IsEmpty)
			{
				return $"Could not find a job with JobId: {jobId}.";
			}

			var jobTreeItem = path.GetItemUnsafe();

			var job = jobTreeItem.Job;

			var coordVals = RValueHelper.GetValuesAsStrings(job.MapAreaInfo.Coords);

			var sb = new StringBuilder()
				.AppendLine("Job Details:")
				.AppendLine($"X1: {coordVals[0]}\tY1: {coordVals[2]}")
				.AppendLine($"X2: {coordVals[1]}\tY2: {coordVals[3]}")

				.AppendLine($"\tTransformType: {jobTreeItem.TransformType}")
				.AppendLine($"\tId: {jobTreeItem.JobId}")
				.AppendLine($"\tParentId: {jobTreeItem.ParentJobId}")
				.AppendLine($"\tCanvasSize Disp Alternates: {jobTreeItem.AlternateDispSizes?.Count ?? 0}");

			if (jobTreeItem.IsActiveAlternate)
			{
				_ = sb.AppendLine("\nThis Job is on the Active Branch.");
				_ = sb.AppendLine("List of all Branches:");
				DisplayAlternates(jobTreeItem, sb, jobTreeItem);
			}
			else if(jobTreeItem.IsParkedAlternate)
			{
				_ = sb.AppendLine("\nThis job is not on the Active Branch:");
				_ = sb.AppendLine("List of all Branches:");
				var activeAltParentPath = path.GetParentPathUnsafe();
				DisplayAlternates(jobTreeItem, sb, activeAltParentPath.GetItemUnsafe());
			}
			else
			{
				_ = sb.AppendLine($"Children: {jobTreeItem.Children.Count}");
			}

			//if (parentNode != null)
			//{
			//	var idx = parentNode.Children.IndexOf(jobTreeItem);
			//	var numberOfFollowingNodes = parentNode.Children.Count - idx - 1;

			//	_ = sb.AppendLine($"Following Nodes: {numberOfFollowingNodes}.");
			//}

			return sb.ToString();
		}

		private void DisplayAlternates(JobTreeItem item, StringBuilder sb, JobTreeItem parentNode)
		{
			_ = sb.AppendLine("  TransformType\tDateCreated\t\tChild Count\tIsActive");

			var altNodes = new List<JobTreeItem>(parentNode.Children);
			var sortPosition = parentNode.GetSortPosition(item.Job);
			altNodes.Insert(sortPosition, item);

			foreach (var altNode in altNodes)
			{
				if (altNode == item)
				{
					_ = sb.Append("*-");
				}
				_ = sb.AppendLine($"{altNode.TransformType}\t{altNode.Job.DateCreated}\t{altNode.Children.Count()}\t\t{altNode.IsActiveAlternate}");
			}

		}

		#endregion



	}
}

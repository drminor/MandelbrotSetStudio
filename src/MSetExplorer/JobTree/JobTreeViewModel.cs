using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Common.MSet.Job>;

namespace MSetExplorer
{
	public class JobTreeViewModel : ViewModelBase, IJobTreeViewModel
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly bool _useSimpleJobTree;
		private IJobOwner? _currentProject;

		#region Constructor

		public JobTreeViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, bool useSimpleJobTree)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_useSimpleJobTree = useSimpleJobTree;
			_currentProject = null;
		}

		#endregion

		#region Public Properties

		public new bool InDesignMode => base.InDesignMode;

		public ObservableCollection<JobTreeNode>? JobNodes => CurrentProject?.JobNodes;

		public IJobOwner? CurrentProject
		{
			get => _currentProject;
			set
			{
				if (value != _currentProject)
				{
					_currentProject = value;
					OnPropertyChanged(nameof(IJobTreeViewModel.JobNodes));
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

		public JobTreeNode? SelectedViewItem
		{
			get => CurrentProject?.SelectedViewItem;
			set
			{
				try
				{
					if (CurrentProject != null)
					{
						CurrentProject.SelectedViewItem = value;
					}
				}
				catch (Exception e)
				{
					Debug.WriteLine($"JobTreeViewModel received exception: {e} while attempting to set the SelectedViewItem to value: {value}");
				}
			}
		}

		#endregion

		#region Public Methods

		public JobPathType? GetCurrentPath()
		{
			return CurrentProject?.GetCurrentPath();
		}

		public JobPathType? GetPath(ObjectId jobId)
		{
			return CurrentProject?.GetPath(jobId);
		}

		public bool TryGetJob(ObjectId jobId, [MaybeNullWhen(false)] out Job job)
		{
			job = CurrentProject?.GetPath(jobId)?.LastTerm?.Item;
			return job != null;
		}

		public bool MarkBranchAsPreferred(ObjectId jobId)
		{
			if (CurrentProject != null)
			{
				var result = CurrentProject.MarkBranchAsPreferred(jobId);
				return result;
			}
			else
			{
				return false;
			}
		}

		public long DeleteJobs(JobPathType path, NodeSelectionType selectionType, out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			if (CurrentProject != null)
			{
				var result = 0L;
				var nodesRemoved = CurrentProject.RemoveJobs(path, selectionType);
				Debug.WriteLine($"RemoveJobs returned {nodesRemoved.Count} jobs.");

				//foreach(var jobPath in jobPathsRemoved)
				//{
				//	numberOfMapSectionsDeleted += ProjectAndMapSectionHelper.DeleteJob(jobPath.Item, _projectAdapter, _mapSectionAdapter);
				//	result++;
				//}

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

			var node = path.Node;
			var job = node.Item;

			//var coordVals = RValueHelper.GetValuesAsStrings(job.MapAreaInfo.Coords);
			var coordVals = RValueHelper.GetValuesAsStrings(job.MapAreaInfo.PositionAndDelta);

			var nt = node.IsRoot ? " [Root]" : node.IsHome ? " [Home]" : null;

			var isBranchHead = IsBranchHead(node);

			var sb = new StringBuilder()
				.AppendLine($"Job Details:{nt}")
				.AppendLine($"X1: {coordVals[0]}\tY1: {coordVals[2]}")
				.AppendLine($"X2: {coordVals[1]}\tY2: {coordVals[3]}")

				.AppendLine($"\tTransformType: {job.TransformType}")
				.AppendLine($"\tId: {job.Id}")
				.AppendLine($"\tParentId: {job.ParentJobId ?? ObjectId.Empty} ParentNodeId: {node.ParentNode?.Id ?? ObjectId.Empty}")
				.AppendLine($"\tChildern: {node.Children.Count}")
				.AppendLine($"\tReal Child Jobs: {node.RealChildJobs.Count}")
				.AppendLine($"\tDisp Alternates: {node.AlternateDispSizes?.Count ?? 0}")
				.AppendLine($"\tOn Preferred Path: {node.IsOnPreferredPath}")
				.AppendLine($"\tIs Branch Head: {isBranchHead}")
				.AppendLine($"\tHas Real Siblings: {node.HasRealSiblings}")
				.AppendLine($"\tIsDirty: {node.IsDirty}");

			if (!_useSimpleJobTree)
			{
				AddDetailsForFlattenedJobTree(path, sb);
			}

			return sb.ToString();
		}

		//private bool IsBranchHead(JobTreeNode node)
		//{
		//	if (node.IsHome || node.ParentNode == null)
		//	{
		//		return true;
		//	}

		//	if (node.ParentNode.Id == node.ParentId)
		//	{
		//		if (node.ParentNode.RealChildJobs.Count > 1 || DoesNodeChangeZoom(node.ParentNode))
		//		{
		//			return true;
		//		}
		//		else
		//		{
		//			return false;
		//		}
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}

		private bool IsBranchHead(JobTreeNode node)
		{
			var realChildCount = node.RealChildJobs.Count;
			var changesZoom = DoesNodeChangeZoom(node);
			var isHome = node.IsHome;

			Debug.Assert((node.ParentNode == null && node.IsHome) || node.ParentNode != null, "Found a non-IsHome job that has no ParentNode.");

			var result = isHome || realChildCount > 1 || changesZoom;
			return result;
		}

		private bool DoesNodeChangeZoom(JobTreeNode node)
		{
			var result = node.TransformType is TransformType.ZoomIn or TransformType.ZoomOut or TransformType.Home;
			return result;
		}

		private void AddDetailsForFlattenedJobTree(JobPathType path, StringBuilder sb)
		{
			var node = path.Node;

			if (node.IsActiveAlternate)
			{
				_ = sb.AppendLine("\nThis Job is on the Active Branch.");
				_ = sb.AppendLine("List of all Branches:");
				DisplayAlternates(node, sb, node);
			}
			else if (node.IsParkedAlternate)
			{
				_ = sb.AppendLine("\nThis job is not on the Active Branch:");
				_ = sb.AppendLine("List of all Branches:");
				var activeAltParentPath = path.GetParentPath()!;
				DisplayAlternates(node, sb, activeAltParentPath.Node);
			}
			else
			{
				_ = sb.AppendLine($"Children: {node.Children.Count}");
			}

			//if (parentNode != null)
			//{
			//	var idx = parentNode.Children.IndexOf(jobTreeItem);
			//	var numberOfFollowingNodes = parentNode.Children.Count - idx - 1;

			//	_ = sb.AppendLine($"Following Nodes: {numberOfFollowingNodes}.");
			//}
		}

		private void DisplayAlternates(JobTreeNode node, StringBuilder sb, JobTreeNode parentNode)
		{
			_ = sb.AppendLine("  TransformType\tDateCreated\t\tChild Count\tIsActive");

			var altNodes = parentNode.Children;
			var sortPosition = parentNode.GetSortPosition(node);
			altNodes.Insert(sortPosition, node);

			foreach (var altNode in altNodes)
			{
				var strItemIndicator = altNode == node ? "*-" : "  ";
				_ = sb.AppendLine($"{strItemIndicator}{altNode.Item.TransformType}\t{altNode.Item.DateCreated}\t{altNode.Children.Count()}\t\t{altNode.IsActiveAlternate}");
			}
		}

		#endregion



	}
}

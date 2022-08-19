using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common
{
	using JobPathType = ITreePath<JobTreeNode, Job>;
	using JobNodeType = ITreeNode<JobTreeNode, Job>;

	public interface IJobTree : ITree<JobTreeNode, Job>, IDisposable
	{
		//ObservableCollection<JobNodeType> Nodes { get; }

		//Job CurrentItem { get; set; }
		//bool IsDirty { get; set; }
		//bool AnyItemIsDirty { get; }

		//JobPathType? GetCurrentPath();
		//JobPathType? GetPath(ObjectId jobId);

		//JobPathType Add(Job job, bool selectTheAddedItem);

		//bool RemoveBranch(ObjectId jobId);
		//bool RemoveBranch(JobPathType path);

		//bool CanGoBack { get; }
		//bool CanGoForward { get; }

		//bool TryGetNextItem([MaybeNullWhen(false)] out Job item, Func<JobNodeType, bool>? predicate = null);
		//bool TryGetPreviousItem([MaybeNullWhen(false)] out Job item, Func<JobNodeType, bool>? predicate = null);

		//bool MoveBack(Func<JobNodeType, bool>? predicate = null);
		//bool MoveForward(Func<JobNodeType, bool>? predicate = null);

		//Job? GetParentItem(Job job);
		//Job? GetItem(ObjectId jobId);

		//IEnumerable<Job> GetItems();
		//List<Job>? GetItemAndDescendants(ObjectId jobId);

		JobNodeType? SelectedNode { get; set; }
		bool UseRealRelationShipsToUpdateSelected { get; set; }

		Job? GetParentItem(Job job);

		bool RestoreBranch(ObjectId jobId);
		bool RestoreBranch(JobPathType path);

		bool TryGetNextJob([MaybeNullWhen(false)] out Job item, bool skipPanJobs);
		bool TryGetPreviousJob([MaybeNullWhen(false)] out Job item, bool skipPanJobs);

		bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy);
	}
}
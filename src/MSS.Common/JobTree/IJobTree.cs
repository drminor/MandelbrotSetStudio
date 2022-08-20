using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics.CodeAnalysis;

using JobPathType = MSS.Types.ITreePath<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;
using JobNodeType = MSS.Types.ITreeNode<MSS.Common.JobTreeNode, MSS.Types.MSet.Job>;

namespace MSS.Common
{
	public interface IJobTree : ITree<JobTreeNode, Job>, IDisposable
	{
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
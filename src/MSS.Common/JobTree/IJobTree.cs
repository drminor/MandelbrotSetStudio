using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common
{
	using JobPathType = ITreePath<JobTreeNode, Job>;

	public interface IJobTree : ITree<JobTreeNode, Job>, IDisposable
	{
		JobTreeSelectionMode SelectionMode { get; set; }

		Job? GetParentItem(Job job);

		bool RestoreBranch(ObjectId jobId);
		bool RestoreBranch(JobPathType path);

		bool TryGetNextJob([MaybeNullWhen(false)] out Job item, bool skipPanJobs);
		bool TryGetPreviousJob([MaybeNullWhen(false)] out Job item, bool skipPanJobs);

		bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy);
	}
}
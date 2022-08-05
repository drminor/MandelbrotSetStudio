using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Common
{
	public interface IJobTree : IDisposable
	{
		ObservableCollection<JobTreeItem> JobItems { get; }

		Job CurrentJob { get; set; }
		bool IsDirty { get; set; }
		bool AnyJobIsDirty { get; }

		List<JobTreeItem>? GetCurrentPath();
		List<JobTreeItem>? GetPath(ObjectId jobId);

		//List<JobTreeItem> AddHomeJob(Job job, JobTreeItem root);
		List<JobTreeItem> Add(Job job, bool selectTheAddedJob);

		bool RestoreBranch(ObjectId jobId);
		bool RemoveBranch(ObjectId jobId);

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		bool TryGetNextJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job);
		bool TryGetPreviousJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job);

		bool MoveBack(bool skipPanJobs);
		bool MoveForward(bool skipPanJobs);

		Job? GetParent(Job job);
		Job? GetJob(ObjectId jobId);

		IEnumerable<Job> GetJobs();
		List<Job>? GetJobAndDescendants(ObjectId jobId);

		bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy);
	}
}
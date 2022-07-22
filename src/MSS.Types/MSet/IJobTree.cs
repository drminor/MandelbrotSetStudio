using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace MSS.Types.MSet
{
	public interface IJobTree : IDisposable
	{
		ObservableCollection<JobTreeItem> JobItems { get; }

		Job? CurrentJob { get; set; }

		void Add(Job job, bool selectAddedJob);

		bool CanGoBack { get; }
		bool CanGoForward { get; }

		bool TryGetNextJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job);
		bool TryGetPreviousJob(bool skipPanJobs, [MaybeNullWhen(false)] out Job job);

		bool MoveBack(bool skipPanJobs);
		bool MoveForward(bool skipPanJobs);

		Job? GetParent(Job job);
		Job? GetJob(ObjectId jobId);

		bool TryGetCanvasSizeUpdateProxy(Job job, SizeInt canvasSizeInBlocks, [MaybeNullWhen(false)] out Job proxy);

		bool AnyJobIsDirty { get; }
		IEnumerable<Job> GetJobs();
	}
}
using MongoDB.Bson;
using MSetRepo;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using ZstdSharp.Unsafe;

namespace MSetExplorer.StorageManagement
{
	internal class StorageModelPOC
	{
		private readonly ProjectAdapter _projectAdapter;
		private readonly MapSectionAdapter _mapSectionAdapter;

		private long _mapSectionCollectionSize;
		private int _mapSectionDocSize;

		public StorageModelPOC(ProjectAdapter projectAdapter, MapSectionAdapter mapSectionAdapter)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;

			_mapSectionCollectionSize = _mapSectionAdapter.GetSizeOfCollectionInMB();
			_mapSectionDocSize = _mapSectionAdapter.GetSizeOfDocZero();
		}

		//public void PlayWithStorageModel(ObjectId projectId)
		//{
		//	var jobAndSubIds = _projectAdapter.GetJobAndSubdivisionIdsForOwner(projectId);

		//	var jobs = new List<StorageModel.Job>();

		//	foreach (var (jobId, subId) in jobAndSubIds)
		//	{
		//		jobs.Add(new StorageModel.Job(jobId, subId));
		//	}

		//	var currentJobId = jobs[0].JobId;

		//	var storageModel = new StorageModel(projectId, jobs, currentJobId);

		//	var jobOwner = storageModel.Owner;

		//	foreach (var smJob in jobOwner.Jobs)
		//	{
		//		var jobMapSectionRecords = _mapSectionAdapter.GetByJobId(smJob.JobId);

		//		foreach (var jobMapSectionRecord in jobMapSectionRecords)
		//		{
		//			var sectionId = jobMapSectionRecord.MapSectionId;

		//			var sectionSubId = _mapSectionAdapter.GetSubdivisionId(sectionId);

		//			if (!sectionSubId.HasValue)
		//			{
		//				throw new InvalidOperationException($"Cannot get the SubdivisionId from the MapSection with Id: {sectionId}.");
		//			}

		//			storageModel.Sections.Add(new StorageModel.Section(sectionId, sectionSubId.Value));

		//			var js = new StorageModel.JobSection(jobMapSectionRecord.Id, jobMapSectionRecord.JobId, sectionId)
		//			{
		//				JobOwnerType = jobMapSectionRecord.OwnerType,
		//				SubdivisionId = jobMapSectionRecord.MapSectionSubdivisionId,
		//				OriginalSourceSubdivisionId = jobMapSectionRecord.JobSubdivisionId,
		//			};

		//			jobOwner.JobSections.Add(js);
		//		}
		//	}
		//}

		public void TestGetDocSizes()
		{
			var mapSectionCollectionSizeInMBytes = _mapSectionAdapter.GetSizeOfCollectionInMB();
			Debug.WriteLine($"At start, the size of the collection in MegaByte is {mapSectionCollectionSizeInMBytes}.");

			var sizeOfDocZero = _mapSectionAdapter.GetSizeOfDocZero();
			Debug.WriteLine($"At start, the size of the zeroth document is {sizeOfDocZero}.");
		}

	}
}

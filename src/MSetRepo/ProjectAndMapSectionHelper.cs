using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSetRepo
{
	public static class ProjectAndMapSectionHelper
	{
		#region Delete Project / Delete Poster

		public static bool DeleteProject(ObjectId projectId, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, out long numberOfMapSectionsDeleted)
		{
			if (projectAdapter.ProjectExists(projectId))
			{
				var jobIds = projectAdapter.GetAllJobIdsForProject(projectId);

				numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForMany(jobIds, JobOwnerType.Project) ?? 0;
				if (numberOfMapSectionsDeleted == 0)
				{
					Debug.WriteLine("WARNING: No MapSections were removed as the project is being deleted.");
				}

				return projectAdapter.DeleteProject(projectId)	? true : throw new InvalidOperationException("Cannot delete existing project record.");
			}
			else
			{
				numberOfMapSectionsDeleted = 0;
				return false;
			}
		}

		public static bool DeletePoster(ObjectId posterId, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, out long numberOfMapSectionsDeleted)
		{
			if (projectAdapter.PosterExists(posterId))
			{
				var jobIds = projectAdapter.GetAllJobIdsForPoster(posterId);

				numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForMany(jobIds, JobOwnerType.Poster) ?? 0;
				if (numberOfMapSectionsDeleted == 0)
				{
					Debug.WriteLine("WARNING: No MapSections were removed as the poster is being deleted.");
				}

				return projectAdapter.DeletePoster(posterId) ? true : throw new InvalidOperationException("Cannot delete existing poster record.");
			}
			else
			{
				numberOfMapSectionsDeleted = 0;
				return false;
			}
		}

		public static long DeleteJobs(IEnumerable<Job> jobs, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			var jobIds = jobs.Select(x => x.Id);
			var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForMany(jobIds, JobOwnerType.Project);

			foreach(var job in jobs.Where(x => x.OnFile))
			{
				if (!projectAdapter.DeleteJob(job.Id))
				{
					throw new InvalidOperationException("Cannot delete existing job record.");
				}
			}

			return numberOfMapSectionsDeleted ?? 0;
		}

		public static long DeleteJob(Job job, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForJob(job.Id, JobOwnerType.Project);
			if (numberOfMapSectionsDeleted == 0)
			{
				Debug.WriteLine($"WARNING: No MapSections were removed for job: {job.Id}.");
			}

			if (job.OnFile && !projectAdapter.DeleteJob(job.Id))
			{
				throw new InvalidOperationException("Cannot delete existing job record.");
			}

			//projectAdapter.d

			return numberOfMapSectionsDeleted ?? 0;
		}

		#endregion


		#region Populate MapJobSections

		public static string PopulateMapJobSectionsForAllProjects(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			var numberOfRecordsInserted = 0L;

			var jobOwnerTypeForThisRun = JobOwnerType.Project;

			IEnumerable<IProjectInfo> projectInfos = projectAdapter.GetAllProjectInfos();

			foreach (var projectInfo in projectInfos)
			{
				var projectId = projectInfo.ProjectId;

				var colorBandSets = projectAdapter.GetColorBandSetsForProject(projectId);
				var jobs = projectAdapter.GetAllJobsForProject(projectId, colorBandSets);

				var displaySize = new SizeDbl(1024);

				numberOfRecordsInserted += CreateMissingJobMapSectionRecords(projectId, projectInfo.Name, jobs, jobOwnerTypeForThisRun, displaySize, mapSectionAdapter, mapJobHelper);

				//foreach (var job in jobs)
				//{
				//	if (job.JobOwnerType != jobOwnerTypeForThisRun)
				//	{
				//		Debug.WriteLine("WARNING: Found a JobOwnerType mismatch.");
				//	}

				//	var mapSectionRequests = GetMapSectionRequests(job, jobOwnerTypeForThisRun, displaySize, mapJobHelper, mapSectionBuilder);

				//	for (var i = 0; i < mapSectionRequests.Count; i++)
				//	{
				//		var mapSectionRequest = mapSectionRequests[i];
				//		var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
				//		var blockPosition = mapSectionRequest.BlockPosition;

				//		var mapSectionId = mapSectionAdapter.GetMapSectionId(subdivisionId, blockPosition);

				//		if (mapSectionId != null)
				//		{
				//			//var subdivision = subdivisonProvider.GetSubdivision(mapSectionRequest.SamplePointDelta, mapSectionRequest.MapBlockOffset, out var localMapBlockOffset);

				//			var inserted = mapSectionAdapter.InsertIfNotFoundJobMapSection(mapSectionId.Value, subdivisionId, job.Id, jobOwnerTypeForThisRun, mapSectionRequest.IsInverted, refIsHard: false, out var mapJobSectionId);

				//			numberOfRecordsInserted += inserted ? 1 : 0;
				//		}
				//	}
				//}
			}

			return $"For JobOwnerType: Project: {numberOfRecordsInserted} new JobMapSection records were created.";
		}

		private static long CreateMissingJobMapSectionRecords(ObjectId ownerId, string ownerName, List<Job> jobs, JobOwnerType jobOwnerTypeForThisRun, SizeDbl displaySize, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			var mapSectionBuilder = new MapSectionBuilder();

			var numberOfRecordsInserted = 0L;

			foreach (var job in jobs)
			{
				if (job.JobOwnerType != jobOwnerTypeForThisRun)
				{
					Debug.WriteLine($"WARNING: Found a JobOwnerType mismatch: Expecting {jobOwnerTypeForThisRun} but found {job.JobOwnerType}. JobOwnerId: {ownerId} - Name: {ownerName}.");
				}

				var mapSectionRequests = GetMapSectionRequests(job, jobOwnerTypeForThisRun, displaySize, mapJobHelper, mapSectionBuilder);

				for (var i = 0; i < mapSectionRequests.Count; i++)
				{
					var mapSectionRequest = mapSectionRequests[i];
					var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
					var blockPosition = mapSectionRequest.BlockPosition;

					var mapSectionId = mapSectionAdapter.GetMapSectionId(subdivisionId, blockPosition);

					if (mapSectionId != null)
					{
						//var subdivision = subdivisonProvider.GetSubdivision(mapSectionRequest.SamplePointDelta, mapSectionRequest.MapBlockOffset, out var localMapBlockOffset);

						var inserted = mapSectionAdapter.InsertIfNotFoundJobMapSection(mapSectionId.Value, subdivisionId, job.Id, jobOwnerTypeForThisRun, mapSectionRequest.IsInverted, refIsHard: false, out var mapJobSectionId);

						numberOfRecordsInserted += inserted ? 1 : 0;
					}
				}
			}

			return numberOfRecordsInserted;
		}

		public static string PopulateMapJobSectionsForAllPosters(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			var numberOfRecordsInserted = 0L;

			var jobOwnerTypeForThisRun = JobOwnerType.Poster;

			IEnumerable<IPosterInfo> posterInfos = projectAdapter.GetAllPosterInfos();

			foreach (var posterInfo in posterInfos)
			{
				var posterId = posterInfo.PosterId;

				var colorBandSets = projectAdapter.GetColorBandSetsForProject(posterId);
				var jobs = projectAdapter.GetAllJobsForPoster(posterId, colorBandSets);

				var displaySize = new SizeDbl(posterInfo.Size);

				//foreach (var job in jobs)
				//{
				//	if (job.JobOwnerType != jobOwnerTypeForThisRun)
				//	{
				//		Debug.WriteLine("WARNING: Found a JobOwnerType mismatch.");
				//	}

				//	var mapSectionRequests = GetMapSectionRequests(job, jobOwnerTypeForThisRun, displaySize, mapJobHelper, mapSectionBuilder);

				//	for (var i = 0; i < mapSectionRequests.Count; i++)
				//	{
				//		var mapSectionRequest = mapSectionRequests[i];
				//		var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
				//		var blockPosition = mapSectionRequest.BlockPosition;

				//		var mapSectionId = mapSectionAdapter.GetMapSectionId(subdivisionId, blockPosition);

				//		if (mapSectionId != null)
				//		{
				//			var inserted = mapSectionAdapter.InsertIfNotFoundJobMapSection(mapSectionId.Value, subdivisionId, job.Id, jobOwnerTypeForThisRun, mapSectionRequest.IsInverted, refIsHard: false, out var mapJobSectionId);

				//			numberOfRecordsInserted += inserted ? 1 : 0;
				//		}
				//	}
				//}

				numberOfRecordsInserted += CreateMissingJobMapSectionRecords(posterId, posterInfo.Name, jobs, jobOwnerTypeForThisRun, displaySize, mapSectionAdapter, mapJobHelper);

			}

			return $"For JobOwnerType: Poster: {numberOfRecordsInserted} new JobMapSection records were created.";
		}


		//public long DeleteNonEssentialMapSections(Job job, SizeDbl posterSize)
		//{
		//	var mapSectionRequests = GetMapSectionRequests(job, posterSize);

		//	if (mapSectionRequests.Count == 0)
		//	{
		//		return 0;
		//	}

		//	// Get a list of all MapSectionIdsAll for the current project.

		//	var jobId = job.Id;
		//	var jobOwnerType = job.JobOwnerType;

		//	var allMapSectionIds = _mapSectionAdapter.GetMapSectionIds(jobId, jobOwnerType);

		//	if (allMapSectionIds.Count == 0)
		//	{
		//		return 0;
		//	}

		//	// For each MapSectionRequest in the provided list, retrieve the MapSectionId
		//	// and remove that Id from the list of AllMapSectionIds.

		//	for (var i = 0; i < mapSectionRequests.Count; i++)
		//	{
		//		var mapSectionRequest = mapSectionRequests[i];
		//		var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
		//		var blockPosition = mapSectionRequest.BlockPosition;

		//		var mapSectionId = _mapSectionAdapter.GetMapSectionId(subdivisionId, blockPosition);

		//		if (mapSectionId != null)
		//		{
		//			allMapSectionIds.Remove(mapSectionId.Value);
		//		}
		//	}

		//	// Now the AllMapSectionIds only contains Ids not included in the provided list.

		//	// Delete all MapSections for the given job, except for those in the specified list
		//	// or those MapSections referenced by some other Job.
		//	var result = _mapSectionAdapter.DeleteMapSectionsWithJobType(allMapSectionIds, jobOwnerType);

		//	return result ?? -1;
		//}

		private static List<MapSectionRequest> GetMapSectionRequests(Job job, JobOwnerType jobOwnerType, SizeDbl displaySize, MapJobHelper mapJobHelper, MapSectionBuilder mapSectionBuilder)
		{
			var jobId = job.Id;
			var mapAreaInfo = job.MapAreaInfo;
			var mapCalcSettings = job.MapCalcSettings;

			var mapAreaInfoV1 = mapJobHelper.GetMapAreaWithSizeFat(mapAreaInfo, displaySize);
			var emptyMapSections = mapSectionBuilder.CreateEmptyMapSections(mapAreaInfoV1, mapCalcSettings);
			var mapSectionRequests = mapSectionBuilder.CreateSectionRequestsFromMapSections(jobId.ToString(), jobOwnerType, mapAreaInfoV1, mapCalcSettings, emptyMapSections);

			return mapSectionRequests;
		}

		#endregion

	}
}

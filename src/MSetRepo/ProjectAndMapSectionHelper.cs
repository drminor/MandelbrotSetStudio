using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

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

		#region Populate JobMapSections

		public static string PopulateJobMapSectionsForAllProjects(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
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
			}

			return $"For JobOwnerType: Project: {numberOfRecordsInserted} new JobMapSection records were created.";
		}

		public static string PopulateJobMapSectionsForAllPosters(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			var numberOfRecordsInserted = 0L;

			var jobOwnerTypeForThisRun = JobOwnerType.Poster;

			IEnumerable<IPosterInfo> posterInfos = projectAdapter.GetAllPosterInfos();

			foreach (var posterInfo in posterInfos)
			{
				var posterId = posterInfo.PosterId;

				var colorBandSets = projectAdapter.GetColorBandSetsForProject(posterId);
				var jobs = projectAdapter.GetAllJobsForPoster(posterId, colorBandSets);

				var displaySize = posterInfo.Size;

				numberOfRecordsInserted += CreateMissingJobMapSectionRecords(posterId, posterInfo.Name, jobs, jobOwnerTypeForThisRun, displaySize, mapSectionAdapter, mapJobHelper);
			}

			return $"For JobOwnerType: Poster: {numberOfRecordsInserted} new JobMapSection records were created.";
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

						var inserted = mapSectionAdapter.InsertIfNotFoundJobMapSection(mapSectionId.Value, subdivisionId, job.Id, jobOwnerTypeForThisRun, mapSectionRequest.IsInverted, refIsHard: false, out var jobMapSectionId);

						numberOfRecordsInserted += inserted ? 1 : 0;
					}
				}
			}

			return numberOfRecordsInserted;
		}

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

		#region Check for Missing Job and MapSections

		/// <summary>
		/// For each job
		///	1. Compare the SubdivisionId with MapAreaInfo2Record.SubdivisionRecord.Id
		///
		///	2. Retrieve the Subdivision Record from the repo and compare
		///		a.	SamplePointDelta.RSizeDto.Width to MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Y1	
		///		b.	SamplePointDelta.RSizeDto.Height to MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Y2
		///		c.	SamplePointDelta.RSizeDto.Exponent to MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Exponent
		///
		///		d.	SamplePointDelta.RSizeDto.Width to MapAreaInfo2Record.SubdivisionRecord.SamplePointDelta.RSizeDto.Width
		///		e.	SamplePointDelta.RSizeDto.Height to MapAreaInfo2Record.SubdivisionRecord.SamplePointDelta.RSizeDto.Height
		///		f.	SamplePointDelta.RSizeDto.Exponent to MapAreaInfo2Record.SubdivisionRecord.SamplePointDelta.RSizeDto.Exponent
		///
		///
		///		g.	BaseMapPosition.BigVectorDto.X to MapAreaInfo2Record.SubdivisionRecord.BaseMapPosition.BigVectorDto.X
		///		h.	BaseMapPosition.BigVectorDto.Y to MapAreaInfo2Record.SubdivisionRecord.BaseMapPosition.BigVectorDto.Y
		/// 
		/// 
		/// </summary>
		/// <param name="mapSectionAdapter"></param>
		/// <param name="jobMapSectionIdsWithMissingMapSection"></param>
		/// <param name="subdivisionIdsForMissingMapSections"></param>
		/// <returns></returns>
		public static string CheckJobRefsAndSubdivisions(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, out List<ObjectId> jobMapSectionIdsWithMissingJobRecord, out List<ObjectId> subdivisionIdsForMissingJobs)
		{
			jobMapSectionIdsWithMissingJobRecord = new List<ObjectId>();
			subdivisionIdsForMissingJobs = new List<ObjectId>();

			var sb = new StringBuilder();
			sb.AppendLine("List of All JobMapSection records having a SubdivisionId different from its Job's SubdivisionId.");
			sb.AppendLine($"JobMapSectionId\tJobId\tSubdivisionId-JobMapSection\tSubdivisionId-JobRecord\tSubdivisionId-JobRecordMapAreaInfo");

			var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobMapSections();

			foreach (var (jobMapSectionId, jobId, subdivisionIdFromJobMapSection) in listOfJobMapIdAndSubdivisionId)
			{
				var subAndMap = projectAdapter.GetSubdivisionId(jobId);

				if (subAndMap.HasValue)
				{
					var (subdivisionIdFromJobRecord, mapAreaInfo2) = subAndMap.Value;

					var subdivisionIdFromJobRecordMapInfo = mapAreaInfo2.Subdivision.Id;

					if (subdivisionIdFromJobRecord != subdivisionIdFromJobMapSection | subdivisionIdFromJobRecordMapInfo != subdivisionIdFromJobMapSection)
					{
						sb.AppendLine($"{jobMapSectionId}\t{jobId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromJobRecord}\t{subdivisionIdFromJobRecordMapInfo}");
					}

					// TODO: Fetch the SubdivisionRecord and compare the details of the mapAreaInfo value. 
				}
				else
				{
					jobMapSectionIdsWithMissingJobRecord.Add(jobMapSectionId);

					if (!subdivisionIdsForMissingJobs.Contains(subdivisionIdFromJobMapSection))
					{
						subdivisionIdsForMissingJobs.Add(subdivisionIdFromJobMapSection);
					}
				}
			}

			return sb.ToString() + $"\n{jobMapSectionIdsWithMissingJobRecord.Count} JobMapSections records reference a non extant Job Record";
		}

		public static string CheckMapRefsAndSubdivisions(IMapSectionAdapter mapSectionAdapter, out List<ObjectId> jobMapSectionIdsWithMissingMapSection, out List<ObjectId> subdivisionIdsForMissingMapSections)
		{
			// For each JobMapSection, retrieve the MapSection Record from the repo
			// and compare the SubdivisionId to the SubdivisionId

			jobMapSectionIdsWithMissingMapSection = new List<ObjectId>();
			subdivisionIdsForMissingMapSections = new List<ObjectId>();

			var sb = new StringBuilder();
			sb.AppendLine("List of All JobMapSection records having a SubdivisionId different from its MapSection's SubdivisionId.");
			sb.AppendLine($"JobMapSectionId\tMapSectionId\tSubdivisionId-JobMapSection\tSubdivisionId-MapSection");

			var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetMapSectionAndSubdivisionIdsForAllJobMapSections();

			foreach(var (jobMapSectionId, mapSectionId, subdivisionIdFromJobMapSection) in listOfJobMapIdAndSubdivisionId)
			{
				var subdivisionIdFromMapSection = mapSectionAdapter.GetSubdivisionId(mapSectionId);

				if (subdivisionIdFromMapSection.HasValue)
				{
					if (subdivisionIdFromMapSection != subdivisionIdFromJobMapSection)
					{
						sb.AppendLine($"{jobMapSectionId}\t{mapSectionId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromMapSection}");
					}
				}
				else
				{
					jobMapSectionIdsWithMissingMapSection.Add(jobMapSectionId);

					if (!subdivisionIdsForMissingMapSections.Contains(subdivisionIdFromJobMapSection))
					{
						subdivisionIdsForMissingMapSections.Add(subdivisionIdFromJobMapSection);
					}
				}
			}

			return sb.ToString() + $"\n{jobMapSectionIdsWithMissingMapSection.Count} JobMapSections records reference a non extant MapSection";
		}

		#endregion

		#region Check for Orphan Jobs and MapSections

		public static string FindOrphanMapSections(IMapSectionAdapter mapSectionAdapter, out List<ObjectId> mapSectionIdsWithNoJob)
		{
			var mapSectionIds = mapSectionAdapter.GetAllMapSectionIds();

			var jobMapSectionIds = mapSectionAdapter.GetJobMapSectionIds(mapSectionIds).ToList();

			var stopWatch = Stopwatch.StartNew();

			var mapSectionsNotReferenced = mapSectionIds.Where(x => !jobMapSectionIds.Contains(x)).ToList();

			stopWatch.Stop();
			Debug.WriteLine($"Find missing MapSections took: {stopWatch.ElapsedMilliseconds}ms.");

			mapSectionIdsWithNoJob = mapSectionsNotReferenced;

			var result = string.Join("\n", mapSectionsNotReferenced);

			return result;
		}

		#endregion
	}
}

using MongoDB.Bson;
using MongoDB.Driver;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using ProjectRepo.Entities;
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

				numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds, JobOwnerType.Project) ?? 0;
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

				numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds, JobOwnerType.Poster) ?? 0;
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
			var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds, JobOwnerType.Project);

			foreach(var job in jobs.Where(x => x.OnFile))
			{
				if (!projectAdapter.DeleteJob(job.Id))
				{
					throw new InvalidOperationException("Cannot delete existing job record.");
				}
			}

			return numberOfMapSectionsDeleted ?? 0;
		}

		public static long DeleteJobsOnFile(IEnumerable<ObjectId> jobIds, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds, JobOwnerType.Project);

			foreach (var jobId in jobIds)
			{
				if (!projectAdapter.DeleteJob(jobId))
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
					var originalSourceSubdivisionId = new ObjectId(mapSectionRequest.OriginalSourceSubdivisionId);
					var blockPosition = mapSectionRequest.BlockPosition;

					var mapSectionId = mapSectionAdapter.GetMapSectionId(subdivisionId, blockPosition);

					if (mapSectionId != null)
					{
						//var subdivision = subdivisonProvider.GetSubdivision(mapSectionRequest.SamplePointDelta, mapSectionRequest.MapBlockOffset, out var localMapBlockOffset);

						var inserted = mapSectionAdapter.InsertIfNotFoundJobMapSection(mapSectionId.Value, subdivisionId, originalSourceSubdivisionId, job.Id, jobOwnerTypeForThisRun, 
							mapSectionRequest.IsInverted, refIsHard: false, out var jobMapSectionId);

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

		#region Find JobMapSections Not Referenced by any Job / by any MapSection

		//public static string CheckJobRefsAndSubdivisions_OLD(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, out List<ObjectId> jobMapSectionIdsWithMissingJobRecord, out List<ObjectId> subdivisionIdsForMissingJobs)
		//{
		//	jobMapSectionIdsWithMissingJobRecord = new List<ObjectId>();
		//	subdivisionIdsForMissingJobs = new List<ObjectId>();

		//	var sb = new StringBuilder();
		//	sb.AppendLine("List of All JobMapSection records having a SubdivisionId different from its Job's SubdivisionId.");

		//	//sb.AppendLine($"JobMapSectionId\tJobId\tSubdivisionId-JobMapSection\tSubdivisionId-JobRecord\tSubdivisionId-JobRecordMapAreaInfo\tSubdivisionId-JobMapSection-Original");
		//	sb.AppendLine($"JobMapSectionId\tJobId\tSubdivisionId-JobMapSection\tSubdivisionId-JobRecord\tSubdivisionId-JobMapSection-Original");

		//	var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobMapSections();

		//	var jobMapSectionCounter = 0;

		//	foreach (var (jobMapSectionId, jobId, subdivisionIdFromJobMapSection, originalSourceSubdivisionId) in listOfJobMapIdAndSubdivisionId)
		//	{
		//		jobMapSectionCounter++;

		//		//var subAndMap = projectAdapter.GetSubdivisionId(jobId);

		//		//if (subAndMap.HasValue)
		//		//{
		//		//	var (subdivisionIdFromJobRecord, mapAreaInfo2) = subAndMap.Value;

		//		//	var subdivisionIdFromJobRecordMapInfo = mapAreaInfo2.Subdivision.Id;

		//		//	//if (subdivisionIdFromJobRecord != subdivisionIdFromJobMapSection | subdivisionIdFromJobRecordMapInfo != subdivisionIdFromJobMapSection)
		//		//	//{
		//		//	//	sb.AppendLine($"{jobMapSectionId}\t{jobId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromJobRecord}\t{subdivisionIdFromJobRecordMapInfo}\t{originalSourceSubdivisionId}");
		//		//	//}

		//		//	if (originalSourceSubdivisionId != ObjectId.Empty && subdivisionIdFromJobRecord != originalSourceSubdivisionId)
		//		//	{
		//		//		sb.AppendLine($"{jobMapSectionId}\t{jobId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromJobRecord}\t{subdivisionIdFromJobRecordMapInfo}\t{originalSourceSubdivisionId}");
		//		//	}

		//		//	// TODO: Fetch the SubdivisionRecord and compare the details of the mapAreaInfo value. 
		//		//}
		//		//else
		//		//{
		//		//	jobMapSectionIdsWithMissingJobRecord.Add(jobMapSectionId);

		//		//	if (!subdivisionIdsForMissingJobs.Contains(subdivisionIdFromJobMapSection))
		//		//	{
		//		//		subdivisionIdsForMissingJobs.Add(subdivisionIdFromJobMapSection);
		//		//	}
		//		//}

		//		var subdivisionIdFromJobRecord = projectAdapter.GetSubdivisionId(jobId);

		//		if (subdivisionIdFromJobRecord.HasValue)
		//		{
		//			if (subdivisionIdFromJobRecord.Value != subdivisionIdFromJobMapSection)
		//			{
		//				sb.AppendLine($"{jobMapSectionId}\t{jobId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromJobRecord}\t{originalSourceSubdivisionId}");
		//			}
		//		}
		//		else
		//		{
		//			jobMapSectionIdsWithMissingJobRecord.Add(jobMapSectionId);

		//			if (!subdivisionIdsForMissingJobs.Contains(subdivisionIdFromJobMapSection))
		//			{
		//				subdivisionIdsForMissingJobs.Add(subdivisionIdFromJobMapSection);
		//			}
		//		}

		//		// TODO: Fetch the SubdivisionRecord and compare the details of the mapAreaInfo value. (This only comparing the Ids.

		//		if (jobMapSectionCounter % 100 == 0)
		//		{
		//			Debug.WriteLine($"{nameof(CheckMapRefsAndSubdivisions)} has processed {jobMapSectionCounter} records.");
		//		}

		//	}

		//	return sb.ToString() + $"\n{jobMapSectionIdsWithMissingJobRecord.Count} JobMapSections records reference a non extant Job Record";
		//}


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

			//sb.AppendLine($"JobMapSectionId\tJobId\tSubdivisionId-JobMapSection\tSubdivisionId-JobRecord\tSubdivisionId-JobRecordMapAreaInfo\tSubdivisionId-JobMapSection-Original");
			sb.AppendLine($"JobMapSectionId\tJobId\tSubdivisionId-JobMapSection\tSubdivisionId-JobRecord\tSubdivisionId-JobMapSection-Original");

			var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobMapSections();

			var jobMapSectionCounter = 0;

			foreach (var (jobMapSectionId, jobId, subdivisionIdFromJobMapSection, originalSourceSubdivisionId) in listOfJobMapIdAndSubdivisionId)
			{
				jobMapSectionCounter++;

				var subdivisionIdFromJobRecord = projectAdapter.GetSubdivisionId(jobId);

				if (subdivisionIdFromJobRecord.HasValue)
				{
					if (subdivisionIdFromJobRecord.Value != subdivisionIdFromJobMapSection)
					{
						sb.AppendLine($"{jobMapSectionId}\t{jobId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromJobRecord}\t{originalSourceSubdivisionId}");
					}
				}
				else
				{
					jobMapSectionIdsWithMissingJobRecord.Add(jobMapSectionId);

					if (!subdivisionIdsForMissingJobs.Contains(subdivisionIdFromJobMapSection))
					{
						subdivisionIdsForMissingJobs.Add(subdivisionIdFromJobMapSection);
					}
				}

				// TODO: Fetch the SubdivisionRecord and compare the details of the mapAreaInfo value. (This only comparing the Ids.

				if (jobMapSectionCounter % 100 == 0)
				{
					Debug.WriteLine($"{nameof(CheckMapRefsAndSubdivisions)} has processed {jobMapSectionCounter} records.");
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
			sb.AppendLine($"JobMapSectionId\tMapSectionId\tSubdivisionId-JobMapSection\tSubdivisionId-MapSection\tSubdivisionId-JobMapSection-Original");

			var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetMapSectionAndSubdivisionIdsForAllJobMapSections();

			var jobMapSectionCounter = 0;

			foreach(var (jobMapSectionId, mapSectionId, subdivisionIdFromJobMapSection, originalSourceSubdivisionId) in listOfJobMapIdAndSubdivisionId)
			{
				jobMapSectionCounter++;
				var subdivisionIdFromMapSection = mapSectionAdapter.GetSubdivisionId(mapSectionId);

				if (subdivisionIdFromMapSection.HasValue)
				{
					if (subdivisionIdFromMapSection.Value != subdivisionIdFromJobMapSection)
					{
						sb.AppendLine($"{jobMapSectionId}\t{mapSectionId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromMapSection}\t{originalSourceSubdivisionId}");
					}

					if (originalSourceSubdivisionId != ObjectId.Empty && subdivisionIdFromMapSection.Value != originalSourceSubdivisionId)
					{
						sb.AppendLine($"{jobMapSectionId}\t{mapSectionId}\t{subdivisionIdFromJobMapSection}\t{subdivisionIdFromMapSection}\t{originalSourceSubdivisionId}");
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

				if (jobMapSectionCounter % 100 == 0)
				{
					Debug.WriteLine($"{nameof(CheckMapRefsAndSubdivisions)} has processed {jobMapSectionCounter} records.");
				}
			}

			return sb.ToString() + $"\n{jobMapSectionIdsWithMissingMapSection.Count} JobMapSections records reference a non extant MapSection";
		}

		#endregion

		#region Check for Orphan Jobs, MapSections and Subdivisions

		public static string FindOrphanJobs(IProjectAdapter projectAdapter, JobOwnerType jobOwnerType, out List<ObjectId> jobIdsWithNoOwner, out List<ObjectId> jobIdsWithOwnerOfWrongType)
		{
			jobIdsWithNoOwner = new List<ObjectId>();
			jobIdsWithOwnerOfWrongType = new List<ObjectId>();

			List<ObjectId> ownerIds;
			List<ObjectId> ownerIdsOfOtherType;

			if (jobOwnerType == JobOwnerType.Project)
			{
				ownerIds = projectAdapter.GetAllProjectIds().ToList();
				ownerIdsOfOtherType = projectAdapter.GetAllPosterIds().ToList();
			}
			else
			{
				ownerIds = projectAdapter.GetAllPosterIds().ToList();
				ownerIdsOfOtherType = projectAdapter.GetAllProjectIds().ToList();
			}

			if (!ListsAreUnique(ownerIds, ownerIdsOfOtherType))
			{
				throw new InvalidCastException("The repository has one or more Projects that have the same Id as a Poster.");
			}

			var jobAndOwnerIds = projectAdapter.GetJobAndOwnerIdsByJobOwnerType(jobOwnerType);

			foreach (var (jobId, ownerId) in jobAndOwnerIds)
			{
				if (ownerIdsOfOtherType.Exists(x => x == ownerId))
				{
					jobIdsWithOwnerOfWrongType.Add(jobId);
				}
				else
				{
					if (!ownerIds.Exists(x => x == ownerId))
					{
						jobIdsWithNoOwner.Add(jobId);
					}
				}
			}

			var sb = new StringBuilder();

			sb.AppendLine();
			sb.AppendLine($"There are {jobIdsWithNoOwner.Count} Jobs with no owner.");
			sb.AppendLine($"There are {jobIdsWithOwnerOfWrongType.Count} {jobOwnerType} jobs that have an owner of the wrong type.");

			if (jobIdsWithNoOwner.Count > 0)
			{
				sb.AppendLine($"{jobOwnerType} Jobs with no owner:");
				sb.AppendLine(string.Join("\n", jobIdsWithNoOwner));
			}

			if (jobIdsWithOwnerOfWrongType.Count > 0)
			{
				sb.AppendLine($"{jobOwnerType} Jobs with with the wrong job type:");
				sb.AppendLine(string.Join("\n", jobIdsWithOwnerOfWrongType));
			}

			return sb.ToString();
		}

		private static bool ListsAreUnique(List<ObjectId> listA, List<ObjectId> listB)
		{
			var commonItems = GetItemsCommonToBothLists(listA, listB);

			//Debug.Assert(commonItems.Count == 0, $"These ids are used for both a project and a poster. {string.Join("\n", commonItems)}.");
			var result = commonItems.Count == 0;

			return result;
		}

		private static List<ObjectId> GetItemsCommonToBothLists(List<ObjectId> listA, List<ObjectId> listB)
		{
			var result = new List<ObjectId>();

			foreach(ObjectId item in listA)
			{
				if (listB.Contains(item))
				{
					result.Add(item);
				}
			}

			return result;
		}

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

		public static string FindOrphanSubdivisions(IMapSectionAdapter mapSectionAdapter, out List<ObjectId> subdivisionIdsWithNoOwner)
		{
			//objListOrder.Sort((x, y) => x.OrderDate.CompareTo(y.OrderDate));
			//var listOfJobAndSubIds = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobs();

			// Get a list of the Subdivision Records
			var subdivisionRecs = mapSectionAdapter.GetAllSubdivisions();
			var subdivisionIds = subdivisionRecs.Select(x => x.Id);

			// Find those subdivisions not referenced by a Job
			var subdivisionIdsFromJobs = mapSectionAdapter.GetSubdivisionIdsForAllJobs().Distinct().ToList();
			var subdivisionIdsUsedByNoJob = subdivisionIds.Where(x => !subdivisionIdsFromJobs.Contains(x)).ToList();

			// Get a list of unique Subdivision Ids from the JobMapSections collection
			var subdivisionIdsFromJobMapSections = new List<ObjectId>();
			var origSourceSubdivisionIdsFromJobMapSections = new List<ObjectId>();

			var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobMapSections();

			var jobMapSectionCounter = 0;

			foreach (var (jobMapSectionId, jobId, subdivisionIdFromJobMapSection, originalSourceSubdivisionId) in listOfJobMapIdAndSubdivisionId)
			{
				jobMapSectionCounter++;

				if (!subdivisionIdsFromJobMapSections.Contains(subdivisionIdFromJobMapSection))
				{
					subdivisionIdsFromJobMapSections.Add(subdivisionIdFromJobMapSection);
				}

				if (originalSourceSubdivisionId != subdivisionIdFromJobMapSection)
				{
					if (!origSourceSubdivisionIdsFromJobMapSections.Contains(originalSourceSubdivisionId))
					{
						origSourceSubdivisionIdsFromJobMapSections.Add(originalSourceSubdivisionId);
					}
				}

				if (jobMapSectionCounter % 100 == 0)
				{
					Debug.WriteLine($"{nameof(CheckMapRefsAndSubdivisions)} has processed {jobMapSectionCounter} records.");
				}
			}

			var foundInJobMapSections = subdivisionIdsUsedByNoJob.Where(x => subdivisionIdsFromJobMapSections.Contains(x)).ToList();

			// Get just the ones not found at first that aren't include the ones found in JobMapSections.
			var notRefed2 = subdivisionIdsUsedByNoJob.Where(x => !foundInJobMapSections.Contains(x)).ToList();

			var foundInOrigSourceJobMapSections = notRefed2.Where(x => origSourceSubdivisionIdsFromJobMapSections.Contains(x)).ToList();

			// Get just the ones not found at so far that aren't included the ones just found in JobMapSections.
			var notRefed3 = notRefed2.Where(x => !foundInOrigSourceJobMapSections.Contains(x)).ToList();

			// Get a list of distinct Subdivision Ids from the MapSections collection
			var mapSectionSubdivisionIds = mapSectionAdapter.GetSubdivisionIdsForAllMapSections().ToList().Distinct().ToList();

			var foundInMapSections = notRefed3.Where(x => mapSectionSubdivisionIds.Contains(x)).ToList();

			// Get just the ones not found at so far that aren't included the ones just found in JobMapSections.
			var notRefed4 = notRefed3.Where(x => !foundInMapSections.Contains(x)).ToList();

			Debug.WriteLine($"There are {subdivisionIdsUsedByNoJob.Count} used by no Job.");
			Debug.WriteLine($"There are {foundInJobMapSections.Count} used by a JobMapSection but not by any Job.");
			Debug.WriteLine($"There are {foundInOrigSourceJobMapSections.Count} used by an OriginalSourceSubdivisionId from a JobMapSection but not by any Job or JobMapSection.");
			Debug.WriteLine($"There are {foundInMapSections.Count} used by a MapSection but not by any other source.");

			subdivisionIdsWithNoOwner = notRefed4;
			var cntNotRefed = subdivisionIdsWithNoOwner.Count;

			Debug.WriteLine($"There are {cntNotRefed} not referenced by any source.");

			var sb = new StringBuilder();

			if (subdivisionIdsWithNoOwner.Count > 0)
			{
				sb.AppendLine($"There are {cntNotRefed} Subdivision records that are not referenced by any Job, MapSection or JobMapSection.");
				sb.AppendLine(string.Join("\n", subdivisionIdsWithNoOwner));

				sb.AppendLine().AppendLine();

				sb.AppendLine("Found\tBaseMapPos\tSpdWidth\tSpdExp");
				foreach(var subdivisionRecord in subdivisionRecs)
				{
					var recId = subdivisionRecord.Id;

					if (subdivisionIdsWithNoOwner.Contains(recId))
					{
						sb.Append("X\t");
					}
					else
					{
						sb.Append("\t");
					}

					sb.AppendLine($"{subdivisionRecord.BaseMapPosition}\t{subdivisionRecord.SamplePointDelta.WidthNumerator}\t{subdivisionRecord.SamplePointDelta.Exponent}");
				}
			}

			return sb.ToString();
		}


		public static string FindOrphanSubdivisions_OLD(IMapSectionAdapter mapSectionAdapter, out List<ObjectId> subdivisionIdsWithNoOwner)
		{
			//objListOrder.Sort((x, y) => x.OrderDate.CompareTo(y.OrderDate));
			//var listOfJobAndSubIds = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobs();

			// Get a list of the Subdivision Records
			var subdivisionRecs = mapSectionAdapter.GetAllSubdivisions();
			var subdivisionIds = subdivisionRecs.Select(x => x.Id);

			// Find those subdivisions not referenced by a Job
			var subdivisionIdsFromJobs = mapSectionAdapter.GetSubdivisionIdsForAllJobs().Distinct().ToList();
			var subdivisionIdsUsedByNoJob = subdivisionIds.Where(x => !subdivisionIdsFromJobs.Contains(x)).ToList();

			// Get a list of unique Subdivision Ids from the JobMapSections collection
			var subdivisionIdsFromJobMapSections = new List<ObjectId>();
			var origSourceSubdivisionIdsFromJobMapSections = new List<ObjectId>();

			var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobMapSections();

			var jobMapSectionCounter = 0;

			foreach (var (jobMapSectionId, jobId, subdivisionIdFromJobMapSection, originalSourceSubdivisionId) in listOfJobMapIdAndSubdivisionId)
			{
				jobMapSectionCounter++;

				if (!subdivisionIdsFromJobMapSections.Contains(subdivisionIdFromJobMapSection))
				{
					subdivisionIdsFromJobMapSections.Add(subdivisionIdFromJobMapSection);
				}

				if (originalSourceSubdivisionId != subdivisionIdFromJobMapSection)
				{
					if (!origSourceSubdivisionIdsFromJobMapSections.Contains(originalSourceSubdivisionId))
					{
						origSourceSubdivisionIdsFromJobMapSections.Add(originalSourceSubdivisionId);
					}
				}

				if (jobMapSectionCounter % 100 == 0)
				{
					Debug.WriteLine($"{nameof(CheckMapRefsAndSubdivisions)} has processed {jobMapSectionCounter} records.");
				}
			}

			var foundInJobMapSections = subdivisionIdsUsedByNoJob.Where(x => subdivisionIdsFromJobMapSections.Contains(x)).ToList();

			// Get just the ones not found at first that aren't include the ones found in JobMapSections.
			var notRefed2 = subdivisionIdsUsedByNoJob.Where(x => !foundInJobMapSections.Contains(x)).ToList();

			var foundInOrigSourceJobMapSections = notRefed2.Where(x => origSourceSubdivisionIdsFromJobMapSections.Contains(x)).ToList();

			// Get just the ones not found at so far that aren't included the ones just found in JobMapSections.
			var notRefed3 = notRefed2.Where(x => !foundInOrigSourceJobMapSections.Contains(x)).ToList();


			//// Find those subdivisions not referenced by a JobMapSection
			//var subdivisionIdsUsedByNoJobMapSection = subdivisionIds.Where(x => !subdivisionIdsFromJobMapSections.Contains(x)).ToList();
			//var origSourceSubdivisionIdsUsedByNoJobMapSection = subdivisionIds.Where(x => !origSourceSubdivisionIdsFromJobMapSections.Contains(x)).ToList();

			//// Initialize the result set.
			//subdivisionIdsWithNoOwner = new List<ObjectId>(subdivisionIdsUsedByNoJob);

			//foreach (var subdivisionId in subdivisionIdsUsedByNoJobMapSection)
			//{
			//	if (subdivisionIdsWithNoOwner.Contains(subdivisionId))
			//	{
			//		subdivisionIdsWithNoOwner.Add(subdivisionId);
			//	}
			//}

			//foreach (var subdivisionId in origSourceSubdivisionIdsUsedByNoJobMapSection)
			//{
			//	if (subdivisionIdsWithNoOwner.Contains(subdivisionId))
			//	{
			//		subdivisionIdsWithNoOwner.Add(subdivisionId);
			//	}
			//}

			// Get a list of distinct Subdivision Ids from the MapSections collection
			var mapSectionSubdivisionIds = mapSectionAdapter.GetSubdivisionIdsForAllMapSections().ToList().Distinct().ToList();

			var foundInMapSections = notRefed3.Where(x => mapSectionSubdivisionIds.Contains(x)).ToList();

			// Get just the ones not found at so far that aren't included the ones just found in JobMapSections.
			var notRefed4 = notRefed3.Where(x => !foundInMapSections.Contains(x)).ToList();



			//// Find those subdivisions referenced by a MapSection
			//var subdivisionIdsUsedByNoMapSection = subdivisionIds.Where(x => !mapSectionSubdivisionIds.Contains(x)).ToList();

			//foreach (var subdivisionId in subdivisionIdsUsedByNoMapSection)
			//{
			//	if (subdivisionIdsWithNoOwner.Contains(subdivisionId))
			//	{
			//		subdivisionIdsWithNoOwner.Add(subdivisionId);
			//	}
			//}

			Debug.WriteLine($"There are {subdivisionIdsUsedByNoJob.Count} used by no Job.");
			Debug.WriteLine($"There are {foundInJobMapSections.Count} used by a JobMapSection but not by any Job.");
			Debug.WriteLine($"There are {foundInOrigSourceJobMapSections.Count} used by an OriginalSourceSubdivisionId from a JobMapSection but not by any Job or JobMapSection.");
			Debug.WriteLine($"There are {foundInMapSections.Count} used by a MapSection but not by any other source.");

			subdivisionIdsWithNoOwner = notRefed4;
			var cntNotRefed = subdivisionIdsWithNoOwner.Count;

			Debug.WriteLine($"There are {cntNotRefed} not referenced by any source.");

			var sb = new StringBuilder();

			if (subdivisionIdsWithNoOwner.Count > 0)
			{
				sb.AppendLine($"There are {cntNotRefed} Subdivision records that are not referenced by any Job, MapSection or JobMapSection.");
				sb.AppendLine(string.Join("\n", subdivisionIdsWithNoOwner));
			}

			return sb.ToString();
		}


		#endregion
	}
}

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
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Text.RegularExpressions;
using ZstdSharp;

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

				numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds) ?? 0;
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

				numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds) ?? 0;
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
			var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds);

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
			var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds);

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
			var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForJob(job.Id);
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

			IEnumerable<IProjectInfo> projectInfos = projectAdapter.GetAllProjectInfos();

			foreach (var projectInfo in projectInfos)
			{
				var projectId = projectInfo.ProjectId;

				var colorBandSets = projectAdapter.GetColorBandSetsForProject(projectId);
				var jobs = projectAdapter.GetAllJobsForProject(projectId, colorBandSets);

				var displaySize = new SizeDbl(1024);

				numberOfRecordsInserted += CreateMissingJobMapSectionRecords(JobType.FullScale, projectId, projectInfo.Name, jobs, OwnerType.Project, displaySize, mapSectionAdapter, mapJobHelper);
			}

			return $"For JobOwnerType: Project: {numberOfRecordsInserted} new JobMapSection records were created.";
		}

		public static string PopulateJobMapSectionsForAllPosters(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			var numberOfRecordsInserted = 0L;

			IEnumerable<IPosterInfo> posterInfos = projectAdapter.GetAllPosterInfos();

			foreach (var posterInfo in posterInfos)
			{
				var posterId = posterInfo.PosterId;

				var colorBandSets = projectAdapter.GetColorBandSetsForProject(posterId);
				var jobs = projectAdapter.GetAllJobsForPoster(posterId, colorBandSets);

				var displaySize = posterInfo.Size;

				numberOfRecordsInserted += CreateMissingJobMapSectionRecords(JobType.FullScale, posterId, posterInfo.Name, jobs, OwnerType.Poster, displaySize, mapSectionAdapter, mapJobHelper);
			}

			return $"For JobOwnerType: Poster: {numberOfRecordsInserted} new JobMapSection records were created.";
		}

		public static long UpdateJobMapSectionSubdivisionIds(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			var ma = mapSectionAdapter as MapSectionAdapter;

			if (ma == null) return 0;

			var jobMapSectionRecords = ma.GetAllJobMapSections();

			var cnt = 0L;
			foreach (var jobMapSectionRecord in jobMapSectionRecords)
			{
				var jobMapSectionId = jobMapSectionRecord.Id;
				var mapSectionSubdivionId = jobMapSectionRecord.MapSectionSubdivisionId;

				var jobSubdivisionId = projectAdapter.GetSubdivisionId(jobMapSectionRecord.JobId);

				ma.UpdateJobMapSectionSubdivisionIds(jobMapSectionId, mapSectionSubdivionId, jobMapSectionId);
				cnt++;
			}

			return cnt;
		}

		private static long CreateMissingJobMapSectionRecords(JobType jobType, ObjectId ownerId, string ownerName, List<Job> jobs, OwnerType ownerType, SizeDbl displaySize, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			var mapSectionBuilder = new MapSectionBuilder();

			var numberOfRecordsInserted = 0L;

			foreach (var job in jobs)
			{
				if (job.JobOwnerType != ownerType)
				{
					Debug.WriteLine($"WARNING: Found a OwnerType mismatch: Expecting {ownerType} but found {job.JobOwnerType}. OwnerId: {ownerId} - Name: {ownerName}.");
				}

				var mapSectionRequests = GetMapSectionRequests(jobType, job, ownerType, displaySize, mapJobHelper, mapSectionBuilder);

				for (var i = 0; i < mapSectionRequests.Count; i++)
				{
					var mapSectionRequest = mapSectionRequests[i];
					var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
					var originalSourceSubdivisionId = new ObjectId(mapSectionRequest.OriginalSourceSubdivisionId);

					if (originalSourceSubdivisionId != subdivisionId)
					{
						Debug.WriteLine($"WARNING: The SubdivisionId from the MapSection is not the same as the SubdivisionId from the Job for the MapSectionRequest: {mapSectionRequest}");
						// and MapSection for {subdivisionId} and {blockPosition} is not on file.
					}
					else
					{
						var blockPosition = mapSectionRequest.BlockPosition;
						var mapSectionId = mapSectionAdapter.GetMapSectionId(subdivisionId, blockPosition);

						if (mapSectionId != null)
						{
							Debug.WriteLine($"");
							//var subdivision = subdivisonProvider.GetSubdivision(mapSectionRequest.SamplePointDelta, mapSectionRequest.MapBlockOffset, out var localMapBlockOffset);

							var blockIndex = new SizeInt(mapSectionRequest.ScreenPositionReleativeToCenter);

							var inserted = mapSectionAdapter.InsertIfNotFoundJobMapSection(JobType.FullScale, job.Id, mapSectionId.Value, blockIndex, mapSectionRequest.IsInverted, subdivisionId,
								originalSourceSubdivisionId, ownerType, out var jobMapSectionId);

							numberOfRecordsInserted += inserted ? 1 : 0;

							//numberOfRecordsInserted++;
						}
						else
						{
							Debug.WriteLine($"WARNING: MapSection for {subdivisionId} and {blockPosition} is not on file.");
						}
					}
				}
			}

			return numberOfRecordsInserted;
		}

		private static List<MapSectionRequest> GetMapSectionRequests(JobType jobType, Job job, OwnerType jobOwnerType, SizeDbl displaySize, MapJobHelper mapJobHelper, MapSectionBuilder mapSectionBuilder)
		{
			var jobId = job.Id;
			var mapAreaInfo = job.MapAreaInfo;
			var mapCalcSettings = job.MapCalcSettings;

			var mapAreaInfoV1 = mapJobHelper.GetMapAreaWithSizeFat(mapAreaInfo, displaySize);
			var emptyMapSections = mapSectionBuilder.CreateEmptyMapSections(mapAreaInfoV1, mapCalcSettings);
			var mapSectionRequests = mapSectionBuilder.CreateSectionRequestsFromMapSections(jobType, jobId.ToString(), jobOwnerType, mapAreaInfoV1, mapCalcSettings, emptyMapSections);

			return mapSectionRequests;
		}

		#endregion

		#region Find JobMapSections Not Referenced by any Job / by any MapSection

		public static string CheckJobRefsAndSubdivisions(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, out List<ObjectId> jobMapSectionIdsWithMissingJobRecord, out List<ObjectId> subdivisionIdsForMissingJobs)
		{
			var sb = new StringBuilder();
			sb.AppendLine("List of All JobMapSection records having a SubdivisionId different from its Job's SubdivisionId.");
			sb.AppendLine($"JobMapSectionId\tJobId\tSubdivisionId-JobMapSection\tSubdivisionId-JobRecord\tSubdivisionId-JobMapSection-Original");

			var listOfJobMapIdAndSubdivisionId = mapSectionAdapter.GetJobAndSubdivisionIdsForAllJobMapSections();

			var jobMapSectionCounter = 0;
			jobMapSectionIdsWithMissingJobRecord = new List<ObjectId>();
			subdivisionIdsForMissingJobs = new List<ObjectId>();

			foreach (var (jobMapSectionId, jobId, mapSectionSubdivisionId, jobSubdivisionId) in listOfJobMapIdAndSubdivisionId)
			{
				jobMapSectionCounter++;

				//Use the JobId to fetch the Job's SubdivisionId.
				var subdivisionIdFromJobRecord = projectAdapter.GetSubdivisionId(jobId);

				if (subdivisionIdFromJobRecord.HasValue)
				{
					// If the SubdivisionIds don't match include details in the report result.
					if (subdivisionIdFromJobRecord.Value != mapSectionSubdivisionId)
					{
						sb.AppendLine($"{jobMapSectionId}\t{jobId}\t{mapSectionSubdivisionId}\t{subdivisionIdFromJobRecord}\t{jobSubdivisionId}");
					}
				}
				else
				{
					// The Job cannot be found, add it's Id to the list
					jobMapSectionIdsWithMissingJobRecord.Add(jobMapSectionId);
					
					// Also collect a list of the distinct SubdivisionIds 
					if (!subdivisionIdsForMissingJobs.Contains(mapSectionSubdivisionId))
					{
						subdivisionIdsForMissingJobs.Add(mapSectionSubdivisionId);
					}
				}

				// TODO: Fetch the SubdivisionRecord and compare the details of the mapAreaInfo value. (This only comparing the Ids.
				//	4. Not yet implemented --- Retrieve the Subdivision Record from the repo and compare
				//		a.	SamplePointDelta.RSizeDto.Width to MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Y1	
				//		b.	SamplePointDelta.RSizeDto.Height to MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Y2
				//		c.	SamplePointDelta.RSizeDto.Exponent to MapAreaInfo2Record.RPointAndDeltaRecord.RPointAndDeltaDto.Exponent
				//
				//		d.	SamplePointDelta.RSizeDto.Width to MapAreaInfo2Record.SubdivisionRecord.SamplePointDelta.RSizeDto.Width
				//		e.	SamplePointDelta.RSizeDto.Height to MapAreaInfo2Record.SubdivisionRecord.SamplePointDelta.RSizeDto.Height
				//		f.	SamplePointDelta.RSizeDto.Exponent to MapAreaInfo2Record.SubdivisionRecord.SamplePointDelta.RSizeDto.Exponent
				//
				//
				//		g.	BaseMapPosition.BigVectorDto.X to MapAreaInfo2Record.SubdivisionRecord.BaseMapPosition.BigVectorDto.X
				//		h.	BaseMapPosition.BigVectorDto.Y to MapAreaInfo2Record.SubdivisionRecord.BaseMapPosition.BigVectorDto.Y

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

			foreach (var (jobMapSectionId, mapSectionId, mapSectionSubdivisionId, jobSubdivisionId) in listOfJobMapIdAndSubdivisionId)
			{
				jobMapSectionCounter++;
				var subdivisionIdFromMapSection = mapSectionAdapter.GetSubdivisionId(mapSectionId);

				if (subdivisionIdFromMapSection.HasValue)
				{
					if (subdivisionIdFromMapSection.Value != mapSectionSubdivisionId)
					{
						sb.AppendLine($"{jobMapSectionId}\t{mapSectionId}\t{mapSectionSubdivisionId}\t{subdivisionIdFromMapSection}\t{jobSubdivisionId}");
					}

					if (jobSubdivisionId != ObjectId.Empty && subdivisionIdFromMapSection.Value != jobSubdivisionId)
					{
						sb.AppendLine($"{jobMapSectionId}\t{mapSectionId}\t{mapSectionSubdivisionId}\t{subdivisionIdFromMapSection}\t{jobSubdivisionId}");
					}
				}
				else
				{
					jobMapSectionIdsWithMissingMapSection.Add(jobMapSectionId);

					if (!subdivisionIdsForMissingMapSections.Contains(mapSectionSubdivisionId))
					{
						subdivisionIdsForMissingMapSections.Add(mapSectionSubdivisionId);
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

		public static string FindOrphanJobs(IProjectAdapter projectAdapter, out List<ObjectId> jobIdsWithNoOwner, out List<ObjectId> jobIdsToBeAssignedJOTofPoster, out List<ObjectId> jobIdsToBeAssignedJOTofProject)
		{
			jobIdsWithNoOwner = new List<ObjectId>();
			jobIdsToBeAssignedJOTofPoster = new List<ObjectId>();
			jobIdsToBeAssignedJOTofProject = new List<ObjectId>();

			var jobIdsToBeAssignedJOTofUndetermined = new List<ObjectId>();

			var	projectIds = projectAdapter.GetAllProjectIds().ToList();
			var	posterIds = projectAdapter.GetAllPosterIds().ToList();

			if (!ListsAreUnique(projectIds, posterIds))
			{
				throw new InvalidCastException("The repository has one or more Projects that have the same Id as a Poster.");
			}

			var jobAndOwnerIds = projectAdapter.GetJobAndOwnerIdsWithJobOwnerType();

			foreach (var (jobId, ownerId, jobOwnerType) in jobAndOwnerIds)
			{
				if (jobOwnerType == OwnerType.Project)
				{
					if (posterIds.Exists(x => x == ownerId))
					{
						jobIdsToBeAssignedJOTofPoster.Add(jobId);
					}
					else
					{
						if (!projectIds.Exists(x => x == ownerId))
						{
							jobIdsWithNoOwner.Add(jobId);
						}
					}
				}
				else if (jobOwnerType == OwnerType.Poster)
				{
					if (projectIds.Exists(x => x == ownerId))
					{
						jobIdsToBeAssignedJOTofProject.Add(jobId);
					}
					else
					{
						if (!posterIds.Exists(x => x == ownerId))
						{
							jobIdsWithNoOwner.Add(jobId);
						}
					}
				}
				else
				{
					jobIdsToBeAssignedJOTofUndetermined.Add(jobId);
				}
			}

			// TODO: Create a new enum: JobRequestType = Regular, Display, Image, EditorPreview
			// TODO: Add a new property on MapSectionRequest and MapSectionResponse of type JobRequestType
			if (jobIdsToBeAssignedJOTofUndetermined.Count > 0)
			{
				//Debug.WriteLine("BREAK HERE");
				throw new InvalidOperationException("There are Job Records having a JobOwnerType other than Project or Poster.");
			}

			var sb = new StringBuilder();

			sb.AppendLine();
			sb.AppendLine($"There are {jobIdsWithNoOwner.Count} Jobs with no owner.");
			sb.AppendLine($"There are {jobIdsToBeAssignedJOTofPoster.Count} Jobs that belong to a Poster that have a JobOwnerType of Project.");
			sb.AppendLine($"There are {jobIdsToBeAssignedJOTofProject.Count} Jobs that belong to a Project that have a JobOwnerType of Poster.");

			if (jobIdsWithNoOwner.Count > 0)
			{
				sb.AppendLine($"Jobs with no owner:");
				sb.AppendLine(string.Join("\n", jobIdsWithNoOwner));
			}

			if (jobIdsToBeAssignedJOTofPoster.Count > 0)
			{
				sb.AppendLine($"Jobs having JobOwnerType = Project that belong to a Poster:");
				sb.AppendLine(string.Join("\n", jobIdsToBeAssignedJOTofPoster));
			}

			if (jobIdsToBeAssignedJOTofProject.Count > 0)
			{
				sb.AppendLine($"Jobs having JobOwnerType = Poster that belong to a Project:");
				sb.AppendLine(string.Join("\n", jobIdsToBeAssignedJOTofProject));
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

using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ZstdSharp.Unsafe;

namespace MSetRepo.Storage
{
    public class StorageModel
    {
		//private readonly Dictionary<ObjectId, (long, long)> _mapSectionRefCounts;

		#region Constructor

		public StorageModel(IJobOwnerInfo jobOwnerInfo, List<Job> jobs) :
            this(new JobOwner(jobOwnerInfo, jobs), new List<JobSection>(), new List<Section>(), new List<JobOwner>())
		{ }

		public StorageModel(IJobOwnerInfo jobOwnerInfo, List<Job> jobs, List<JobSection> jobSections, List<Section> sections) :
			this(new JobOwner(jobOwnerInfo, jobs), jobSections, sections, new List<JobOwner>())
		{ }

		public StorageModel(JobOwner jobOwner) : this(jobOwner, new List<JobSection>(), new List<Section>(), new List<JobOwner>())
		{ }

		public StorageModel(JobOwner jobOwner, List<JobSection> jobSections, List<Section> sections) : this(jobOwner, jobSections, sections, new List<JobOwner>())
		{ }

		public StorageModel(JobOwner jobOwner, List<JobSection> jobSections, List<Section> sections, List<JobOwner> otherOwners)
		{
			Owner = jobOwner;
			Sections = sections;
			JobSections = jobSections;
			OtherOwners = otherOwners;

			CriticalSectionIdRefs = new Dictionary<ObjectId, long>();
			NonCriticalSectionIdRefs = new Dictionary<ObjectId, long>();

			BuildCriticalSectionIdsRefs(jobOwner.Jobs, CriticalSectionIdRefs);
			BuildNonCriticalSectionIdsRefs(jobOwner.Jobs, NonCriticalSectionIdRefs);
		}

		#endregion

		#region Public Properties

		public JobOwner Owner { get; init; }

        public List<Section> Sections { get; init; }

        public List<JobSection> JobSections { get; init; }

		public Dictionary<ObjectId, long> CriticalSectionIdRefs { get; init; }
		public Dictionary<ObjectId, long> NonCriticalSectionIdRefs { get; init; }

		public List<JobOwner> OtherOwners { get; init; }

        #endregion

        #region Public Methods

		public void UpdateStats()
		{
			//UpdateMapSectionRefs();

			foreach (var job in Owner.Jobs)
			{
				var byJobIdFilter = JobSections.Where(x => x.JobId == job.JobId);

				//job.NumberOfFullScale = byJobIdFilter.Count(x => x.JobType == JobType.FullScale);
				//job.NumberOfReducedScale = byJobIdFilter.Count(x => x.JobType == JobType.ReducedScale);
				//job.NumberOfImage = byJobIdFilter.Count(x => x.JobType == JobType.Image);
				//job.NumberOfSizeEditorPreview = byJobIdFilter.Count(x => x.JobType == JobType.SizeEditorPreview);

				////job.NumberOfMapSections = job.NumberOfFullScale + job.NumberOfReducedScale + job.NumberOfImage + job.NumberOfSizeEditorPreview;
				//job.NumberOfMapSections = byJobIdFilter.Select(x => x.SectionId).Distinct().Count();
			}
		}

		public int GetNumberOfSharedSectionIds(List<ObjectId> sectionIds, Dictionary<ObjectId, long> sectionIdRefs)
		{
			var result = 0;

			foreach(var sectionId in sectionIds)
			{
				if (sectionIdRefs.TryGetValue(sectionId, out var sectionIdRef))
				{
					if (sectionIdRef > 1)
					{
						result++;
					}
				}
				else
				{
					Debug.WriteLine($"WARNING: Could not find the SectionId: {sectionId}.");
				}
			}

			return result;
		}

		#endregion

		#region Private Methods

		private void BuildCriticalSectionIdsRefs(List<Job> jobs, Dictionary<ObjectId, long> dict)
		{
			dict.Clear();

			if (jobs.Count < 1)
			{
				return;
			}

			var cSectionIds = jobs[0].DistinctCriticalSectionIds;

			foreach(var sectionId in cSectionIds)
			{
				dict.Add(sectionId, 1);
			}

			for (var i = 1; i < jobs.Count; i++)
			{
				cSectionIds = jobs[i].DistinctCriticalSectionIds;

				for (var j = 0; j < cSectionIds.Count; j++)
				{
					var testId = cSectionIds[j];

					if (dict.TryGetValue(testId, out var refCnt))
					{
						dict[testId] = 1 + refCnt;
					}
					else
					{
						dict.Add(testId, 1);
					}
				}
			}
		}

		private void BuildNonCriticalSectionIdsRefs(List<Job> jobs, Dictionary<ObjectId, long> dict)
		{
			dict.Clear();

			if (jobs.Count < 1)
			{
				return;
			}

			var cSectionIds = jobs[0].DistinctCriticalSectionIds;

			foreach (var sectionId in cSectionIds)
			{
				dict.Add(sectionId, 1);
			}

			for (var i = 1; i < jobs.Count; i++)
			{
				cSectionIds = jobs[i].DistinctCriticalSectionIds;

				for (var j = 0; j < cSectionIds.Count; j++)
				{
					var testId = cSectionIds[j];

					if (dict.TryGetValue(testId, out var refCnt))
					{
						dict[testId] = 1 + refCnt;
					}
					else
					{
						dict.Add(testId, 1);
					}
				}
			}
		}

		//private void UpdateMapSectionRefs()
		//{
		//	_mapSectionRefCounts.Clear();

		//	for (var i = 0; i < JobSections.Count; i++)
		//	{
		//		var jobSection = JobSections[i];

		//		if (jobSection.JobType == JobType.FullScale | jobSection.JobType == JobType.Image)
		//		{
		//			if (_mapSectionRefCounts.TryGetValue(jobSection.SectionId, out (long cRef, long ncRef) t))
		//			{
		//				_mapSectionRefCounts[jobSection.SectionId] = (t.cRef + 1, t.ncRef);
		//			}
		//			else
		//			{
		//				_mapSectionRefCounts.Add(jobSection.SectionId, (1, 0));
		//			}
		//		}
		//		else
		//		{
		//			if (_mapSectionRefCounts.TryGetValue(jobSection.SectionId, out (long cRef, long ncRef) t))
		//			{
		//				_mapSectionRefCounts[jobSection.SectionId] = (t.cRef, t.ncRef + 1);
		//			}
		//			else
		//			{
		//				_mapSectionRefCounts.Add(jobSection.SectionId, (0, 1));
		//			}
		//		}
		//	}
		//}

		#endregion
	}

	public class JobOwner
	{
		public ObjectId JobOwnerId { get; init; }
		public OwnerType OwnerType { get; init; }
		public DateTime DateCreated { get; init; }

		public List<Job> Jobs { get; init; }
		public ObjectId CurrentJobId { get; set;}

		public JobOwner(IJobOwnerInfo jobOwnerInfo, List<Job> jobs) : this(jobOwnerInfo.OwnerId, jobOwnerInfo.OwnerType, jobOwnerInfo.DateCreatedUtc, jobs, jobOwnerInfo.CurrentJobId)
		{ }

		public JobOwner(ObjectId jobOwnerId, OwnerType ownerType, DateTime dateCreated, List<Job> jobs, ObjectId currentJobId)
		{
			JobOwnerId = jobOwnerId;
			OwnerType = ownerType;
			DateCreated = dateCreated;
			Jobs = jobs;
			CurrentJobId = currentJobId;
		}

		public int NumberOfMapSections => Jobs.Sum(x => x.NumberOfMapSections);
		public int NumberOfCriticalMapSections => Jobs.Sum(x => x.NumberOfCriticalMapSections);
		public int NumberOfNonCriticalMapSections => Jobs.Sum(x => x.NumberOfNonCriticalMapSections);

		//public int NumberOfFullScale => Jobs.Sum(x => x.NumberOfFullScale);
		//public int NumberOfReducedScale => Jobs.Sum(x => x.NumberOfReducedScale);
		//public int NumberOfImage => Jobs.Sum(x => x.NumberOfImage);
		//public int NumberOfSizeEditorPreview => Jobs.Sum(x => x.NumberOfSizeEditorPreview);

		public double PercentageMapSectionsSharedGlobally { get; set; }
	}

	public class Job
	{
		public ObjectId JobId { get; init; }
		public DateTime DateCreated { get; init; }

		public ObjectId SubdivisionId { get; set; }

		public Job(ObjectId jobId, DateTime dateCreated, ObjectId subdivisionId, List<ObjectId> distinctCriticalSectionIds, List<ObjectId> distinctNonCriticalSectionIds)
		{
			JobId = jobId;
			DateCreated = dateCreated;
			SubdivisionId = subdivisionId;

			DistinctCriticalSectionIds = distinctCriticalSectionIds;
			DistinctNonCriticalSectionIds = distinctNonCriticalSectionIds;
		}

		//public int NumberOfFullScale { get; set; }
		//public int NumberOfReducedScale { get; set; }
		//public int NumberOfImage { get; set; }
		//public int NumberOfSizeEditorPreview { get; set; }

		public double PercentageMapSectionsSharedGlobally { get; set; }
		public double PercentageMapSectionsSharedWithSameOwner { get; set; }

		public List<ObjectId> DistinctCriticalSectionIds { get; init; }
		public List<ObjectId> DistinctNonCriticalSectionIds { get; init; }

		public int NumberOfMapSections => NumberOfCriticalMapSections + NumberOfNonCriticalMapSections;
		public int NumberOfCriticalMapSections => DistinctCriticalSectionIds.Count;
		public int NumberOfNonCriticalMapSections => DistinctNonCriticalSectionIds.Count;
	}

	public class Section
	{
		public ObjectId SectionId { get; init; }
		public DateTime DateCreated { get; init; }

		public ObjectId SubdivisionId { get; set; }

		public Section(ObjectId sectionId, DateTime dateCreated, ObjectId subdivisionId)
		{
			SectionId = sectionId;
			DateCreated = dateCreated;
			SubdivisionId = subdivisionId;
		}
	}

	public class JobSection
	{
		public ObjectId JobSectionId { get; init; }
		public DateTime DateCreated { get; init; }

		public JobType JobType { get; init; }

		public ObjectId JobId { get; init; }
		public ObjectId SectionId { get; init; }
		SizeInt BlockIndex { get; init; }
		bool IsInverted { get; init; }

		public ObjectId MapSectionSubdivisionId { get; init; }
		public ObjectId JobSubdivisionId { get; set; }
		public OwnerType OwnerType { get; set; }

		public JobSection(ObjectId jobSectionId, DateTime dateCreated, JobType jobType, ObjectId jobId, ObjectId sectionId,
			SizeInt blockIndex, bool isInverted, ObjectId mapSectionSubdivisionId, ObjectId jobSubdivisionId, OwnerType ownerType)
		{
			JobSectionId = jobSectionId;
			DateCreated = dateCreated;
			JobType = jobType;
			JobId = jobId;
			SectionId = sectionId;
			BlockIndex = blockIndex;
			IsInverted = isInverted;
			MapSectionSubdivisionId = mapSectionSubdivisionId;
			JobSubdivisionId = jobSubdivisionId;
			OwnerType = ownerType;
		}
	}
}

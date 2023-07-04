using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MSetRepo.Storage
{
    public class StorageModel
    {
		private readonly Dictionary<ObjectId, long> _MapSectionRefCounts;

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

			_MapSectionRefCounts = new Dictionary<ObjectId, long>();
		}

		#endregion

		#region Public Properties

		public JobOwner Owner { get; init; }

        public List<Section> Sections { get; init; }

        public List<JobSection> JobSections { get; init; }

        public List<JobOwner> OtherOwners { get; init; }

        #endregion

        #region Public Methods

		public void UpdateStats()
		{
			foreach(var job in Owner.Jobs)
			{
				var byJobIdFilter = JobSections.Where(x => x.JobId == job.JobId);

				job.NumberOfFullScale = byJobIdFilter.Count(x => x.JobType == JobType.FullScale);
				job.NumberOfReducedScale = byJobIdFilter.Count(x => x.JobType == JobType.ReducedScale);
				job.NumberOfImage = byJobIdFilter.Count(x => x.JobType == JobType.Image);
				job.NumberOfSizeEditorPreview = byJobIdFilter.Count(x => x.JobType == JobType.SizeEditorPreview);

				job.NumberOfMapSections = job.NumberOfFullScale + job.NumberOfReducedScale + job.NumberOfImage + job.NumberOfSizeEditorPreview;


			}
		}

		#endregion
	}

	public class JobOwner
	{
		public ObjectId JobOwnerId { get; init; }
		public DateTime DateCreated { get; init; }

		public OwnerType OwnerType { get; init; }
		public List<Job> Jobs { get; init; }
		//public Job CurrentJob { get; set; }
		public ObjectId CurrentJobId { get; set;}

		public JobOwner(IJobOwnerInfo jobOwnerInfo, List<Job> jobs) : this(jobOwnerInfo.OwnerId, jobOwnerInfo.DateCreatedUtc, jobOwnerInfo.OwnerType, jobs, jobOwnerInfo.CurrentJobId)
		{ }

		public JobOwner(ObjectId jobOwnerId, DateTime dateCreated, OwnerType ownerType, List<Job> jobs, ObjectId currentJobId)
		{
			JobOwnerId = jobOwnerId;
			DateCreated = dateCreated;
			OwnerType = ownerType;
			Jobs = jobs;
			CurrentJobId = currentJobId;
		}

		public int NumberOfMapSections => Jobs.Sum(x => x.NumberOfMapSections);

		public int NumberOfFullScale => Jobs.Sum(x => x.NumberOfFullScale);
		public int NumberOfReducedScale => Jobs.Sum(x => x.NumberOfReducedScale);
		public int NumberOfImage => Jobs.Sum(x => x.NumberOfImage);
		public int NumberOfSizeEditorPreview => Jobs.Sum(x => x.NumberOfSizeEditorPreview);

		public double PercentageMapSectionsShared { get; set; }
	}

	public class Job
	{

		public ObjectId JobId { get; init; }
		public DateTime DateCreated { get; init; }

		public ObjectId SubdivisionId { get; set; }

		public Job(ObjectId jobId, DateTime dateCreated, ObjectId subdivisionId, List<ObjectId> uniqueMapSectionIds)
		{
			JobId = jobId;
			DateCreated = dateCreated;
			SubdivisionId = subdivisionId;

			UniqueMapSectionIds = uniqueMapSectionIds;
		}

		public int NumberOfMapSections { get; set; }

		public int NumberOfFullScale { get; set; }
		public int NumberOfReducedScale { get; set; }
		public int NumberOfImage { get; set; }
		public int NumberOfSizeEditorPreview { get; set; }

		public double PercentageMapSectionsShared { get; set; }
		public double PercentageMapSectionsSharedWithSameOwner { get; set; }

		public List<ObjectId> UniqueMapSectionIds { get; init; }

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

using MongoDB.Bson;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MSetRepo.Storage
{
    public class StorageModel
    {
        #region Constructor

        public StorageModel(ObjectId jobOwnerId, DateTime datecreated, OwnerType ownerType, List<Job> jobs, ObjectId currentJobId) :
            this(new JobOwner(jobOwnerId, datecreated, ownerType, jobs, currentJobId))
        { }

        public StorageModel(JobOwner jobOwner) : this(jobOwner, new List<Section>(), new List<JobSection>(), new List<JobOwner>())
        { }

		public StorageModel(JobOwner jobOwner, List<Section> sections, List<JobSection> jobSections) : this(jobOwner, sections, jobSections, new List<JobOwner>())
		{ }

		public StorageModel(JobOwner jobOwner, List<Section> sections, List<JobSection> jobSections, List<JobOwner> otherOwners)
		{
			Owner = jobOwner;
			Sections = sections;
			JobSections = jobSections;
			OtherOwners = otherOwners;
		}

		#endregion

		#region Public Properties

		public JobOwner Owner { get; init; }

        public List<Section> Sections { get; init; }

        public List<JobSection> JobSections { get; init; }

        public List<JobOwner> OtherOwners { get; init; }

        #endregion

        #region Public Methods


        #endregion

        #region Our Classes



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

		public JobOwner(ObjectId jobOwnerId, DateTime dateCreated, OwnerType ownerType, List<Job> jobs, ObjectId currentJobId)
		{
			JobOwnerId = jobOwnerId;
			DateCreated = dateCreated;
			OwnerType = ownerType;
			Jobs = jobs;
			CurrentJobId = currentJobId;

			//var test = jobs.FirstOrDefault(x => x.JobId == currentJobId);
			//if (test == null)
			//{
			//	throw new InvalidOperationException("The currentJobId cannot be found in the list of jobs.");
			//}

			//CurrentJob = test;
		}
	}

	public class Job
	{
		public ObjectId JobId { get; init; }
		public DateTime DateCreated { get; init; }

		public ObjectId SubdivisionId { get; set; }

		public Job(ObjectId jobId, DateTime dateCreated, ObjectId subdivisionId)
		{
			JobId = jobId;
			DateCreated = dateCreated;
			SubdivisionId = subdivisionId;
		}
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

using MongoDB.Bson;
using MSS.Types;
using System.Collections.Generic;

namespace MSetRepo
{
	public class StorageModel
	{
		#region Constructor

		public StorageModel(ObjectId jobOwnerId, List<Job> jobs, ObjectId currentJobId) : this(new JobOwner(jobOwnerId, jobs, currentJobId))
		{ }

		public StorageModel(JobOwner jobOwner)
		{
			OwnerBeingManaged = jobOwner;

			Sections = new List<Section>();
			JobSections = new List<JobSection>();

			OtherOwners = new List<JobOwner>();
		}

		#endregion

		#region Public Properties

		public JobOwner OwnerBeingManaged { get; init; }

		public List<Section> Sections { get; init; }

		public List<JobSection> JobSections { get; init; }

		public List<JobOwner> OtherOwners { get; init; }

		#endregion

		#region Public Methods


		#endregion

		#region Our Classes

		public class JobOwner
		{
			public ObjectId JobOwnerId { get; init; }

			public List<Job> Jobs { get; init; }
			public List<Section> Sections { get; init; }
			public List<JobSection> JobSections { get; init; }


			public ObjectId CurrentJobId { get; set; }

			public JobOwner(ObjectId jobOwnerId, List<Job> jobs, ObjectId currentJobId)
			{
				JobOwnerId = jobOwnerId;
				Jobs = jobs;
				CurrentJobId = currentJobId;

				Sections = new List<Section>();
				JobSections = new List<JobSection>();
			}
		}

		public class Job
		{
			public ObjectId JobId { get; init; }
			public ObjectId SubdivisionId { get; set; }

			public Job(ObjectId jobId, ObjectId subdivisionId)
			{
				JobId = jobId;
				SubdivisionId = subdivisionId;
			}
		}

		public class Section
		{
			public ObjectId SectionId { get; init; }
			public ObjectId SubdivisionId { get; set; }

			public Section(ObjectId sectionId, ObjectId subdivisionId)
			{
				SectionId = sectionId;
				SubdivisionId = subdivisionId;
			}
		}

		public class JobSection
		{
			public ObjectId JobSectionId { get; init; }

			public ObjectId JobId { get; init; }
			public ObjectId SectionId { get; init; }
			public ObjectId SubdivisionId { get; set; }
			public OwnerType JobOwnerType { get; set; }

			public ObjectId OriginalSourceSubdivisionId { get; set; }


			public JobSection(ObjectId jobSectionId, ObjectId jobId, ObjectId sectionId)
			{
				JobSectionId = jobSectionId;
				JobId = jobId;
				SectionId = sectionId;
			}
		}

		#endregion
	}


}

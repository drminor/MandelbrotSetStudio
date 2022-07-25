using MongoDB.Bson;
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
		public static long DeleteProject(string name, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			if (projectAdapter.TryGetProject(name, out var formerProject))
			{
				var formerProjectId = formerProject.Id;
				var formerProjectJobIds = projectAdapter.GetAllJobsIdsForProject(formerProjectId);

				var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForMany(formerProjectJobIds, JobOwnerType.Project) ?? 0;
				if (numberOfMapSectionsDeleted == 0)
				{
					Debug.WriteLine("WARNING: No MapSections were removed for the project being overwritten.");
				}

				if (!projectAdapter.DeleteProject(formerProjectId))
				{
					throw new InvalidOperationException("Cannot delete existing project record.");
				}

				return numberOfMapSectionsDeleted;
			}
			else
			{
				return -1;
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

			return numberOfMapSectionsDeleted ?? 0;
		}

	}
}

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

	}
}

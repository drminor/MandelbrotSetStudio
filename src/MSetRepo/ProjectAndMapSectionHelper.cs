using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSetRepo
{
	public static class ProjectAndMapSectionHelper
	{
		public static void DeleteProject(string name, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
		{
			if (projectAdapter.TryGetProject(name, out var formerProject))
			{
				var formerProjectId = formerProject.Id;
				var formerProjectJobIds = projectAdapter.GetAllJobsIdsForProject(formerProjectId);

				var numberOfMapSectionsDeleted = mapSectionAdapter.DeleteMapSectionsForMany(formerProjectJobIds, JobOwnerType.Project);
				if (numberOfMapSectionsDeleted == 0)
				{
					Debug.WriteLine("WARNING: No MapSections were removed for the project being overwritten.");
				}

				if (!projectAdapter.DeleteProject(formerProjectId))
				{
					throw new InvalidOperationException("Cannot delete existing project record.");
				}
			}
		}

	}
}

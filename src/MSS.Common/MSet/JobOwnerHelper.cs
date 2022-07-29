using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	public static class JobOwnerHelper
	{
		public static bool Save(IJobOwner project, IProjectAdapter projectAdapter)
		{
			if (!(project.IsCurrentJobIdChanged || project.IsDirty))
			{
				Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
				return false;
			}

			if (project.IsCurrentJobIdChanged)
			{
				projectAdapter.UpdateProjectCurrentJobId(project.Id, project.CurrentJobId);
			}

			if (project.IsDirty)
			{
				projectAdapter.UpdateProjectName(project.Id, project.Name);
				projectAdapter.UpdateProjectDescription(project.Id, project.Description);
				SaveColorBandSets(project, projectAdapter);
				SaveJobs(project, projectAdapter);

				project.MarkAsSaved();
			}

			return true;
		}

		public static Project CreateCopyOfProject(IJobOwner sourceProject, string name, string? description, IProjectAdapter projectAdapter, IMapSectionDuplicator mapSectionDuplicator)
		{
			// TODO: Update the JobTree with a new Clone or Copy method. 
			var jobPairs = sourceProject.GetJobs().Select(x => new Tuple<ObjectId, Job>(x.Id, x.CreateNewCopy())).ToArray();
			var jobs = jobPairs.Select(x => x.Item2).ToArray();

			foreach (var oldIdAndNewJob in jobPairs)
			{
				var formerJobId = oldIdAndNewJob.Item1;
				var newJobId = oldIdAndNewJob.Item2.Id;
				UpdateJobParents(formerJobId, newJobId, jobs);

				var numberJobMapSectionRefsCreated = mapSectionDuplicator.DuplicateJobMapSections(formerJobId, JobOwnerType.Project, newJobId);
				Debug.WriteLine($"{numberJobMapSectionRefsCreated} new JobMapSectionRecords were created as Job: {formerJobId} was duplicated.");
			}

			var colorBandSetPairs = sourceProject.GetColorBandSets().Select(x => new Tuple<ObjectId, ColorBandSet>(x.Id, x.CreateNewCopy())).ToArray();
			var colorBandSets = colorBandSetPairs.Select(x => x.Item2).ToArray();

			foreach (var oldIdAndNewCbs in colorBandSetPairs)
			{
				UpdateCbsParentIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, colorBandSets);
				UpdateJobCbsIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, jobs);
			}

			var project = projectAdapter.CreateProject(name, description, jobs, colorBandSets);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new project.");
			}

			var firstOldIdAndNewJob = jobPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentJobId);
			var newCurJob = firstOldIdAndNewJob?.Item2;
			project.CurrentJob = newCurJob ?? Job.Empty;

			var firstOldIdAndNewCbs = colorBandSetPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentColorBandSetId);
			var newCurCbs = firstOldIdAndNewCbs?.Item2;

			project.CurrentColorBandSet = newCurCbs ?? new ColorBandSet();

			return project;
		}

		public static Poster CreateCopyOfPoster(IJobOwner sourceProject, string name, string? description, IProjectAdapter projectAdapter, IMapSectionDuplicator mapSectionDuplicator)
		{
			// TODO: Update the JobTree with a new Clone or Copy method. 
			var jobPairs = sourceProject.GetJobs().Select(x => new Tuple<ObjectId, Job>(x.Id, x.CreateNewCopy())).ToArray();
			var jobs = jobPairs.Select(x => x.Item2).ToArray();

			foreach (var oldIdAndNewJob in jobPairs)
			{
				var formerJobId = oldIdAndNewJob.Item1;
				var newJobId = oldIdAndNewJob.Item2.Id;
				UpdateJobParents(formerJobId, newJobId, jobs);

				var numberJobMapSectionRefsCreated = mapSectionDuplicator.DuplicateJobMapSections(formerJobId, JobOwnerType.Project, newJobId);
				Debug.WriteLine($"{numberJobMapSectionRefsCreated} new JobMapSectionRecords were created as Job: {formerJobId} was duplicated.");
			}

			var colorBandSetPairs = sourceProject.GetColorBandSets().Select(x => new Tuple<ObjectId, ColorBandSet>(x.Id, x.CreateNewCopy())).ToArray();
			var colorBandSets = colorBandSetPairs.Select(x => x.Item2).ToArray();

			foreach (var oldIdAndNewCbs in colorBandSetPairs)
			{
				UpdateCbsParentIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, colorBandSets);
				UpdateJobCbsIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, jobs);
			}

			var poster = projectAdapter.CreatePoster(name, description, sourceProject.CurrentJobId, jobs, colorBandSets, sourceProject.CurrentJobId);

			if (poster is null)
			{
				throw new InvalidOperationException("Could not create the new project.");
			}

			var firstOldIdAndNewJob = jobPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentJobId);
			var newCurJob = firstOldIdAndNewJob?.Item2;
			poster.CurrentJob = newCurJob ?? Job.Empty;

			var firstOldIdAndNewCbs = colorBandSetPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentColorBandSetId);
			var newCurCbs = firstOldIdAndNewCbs?.Item2;

			poster.CurrentColorBandSet = newCurCbs ?? new ColorBandSet();

			return poster;
		}


		public static long DeleteMapSectionsForUnsavedJobs(IJobOwner project, IMapSectionDeleter mapSectionDeleter)
		{
			var result = 0L;

			var jobs = project.GetJobs().Where(x => !x.OnFile).ToList();

			foreach (var job in jobs)
			{
				var numberDeleted = mapSectionDeleter.DeleteMapSectionsForJob(job.Id, JobOwnerType.Project);
				if (numberDeleted.HasValue)
				{
					result += numberDeleted.Value;
				}
			}

			return result;
		}

		public static void SaveColorBandSets(IJobOwner project, IProjectAdapter projectAdapter)
		{
			var colorBandSets = project.GetColorBandSets();
			var unsavedColorBandSets = colorBandSets.Where(x => x.OnFile).ToList();

			foreach (var cbs in unsavedColorBandSets)
			{
				if (cbs.ProjectId != project.Id)
				{
					Debug.WriteLine($"WARNING: ColorBandSet has a different projectId than the current projects. ColorBandSetId: {cbs.ProjectId}, current Project: {project.Id}.");
					var newCbs = cbs.Clone();
					newCbs.ProjectId = project.Id;
					projectAdapter.InsertColorBandSet(newCbs);
				}
				else
				{
					projectAdapter.InsertColorBandSet(cbs);
				}
			}

			var dirtyColorBandSets = colorBandSets.Where(x => x.IsDirty).ToList();

			foreach (var cbs in dirtyColorBandSets)
			{
				projectAdapter.UpdateColorBandSetDetails(cbs);
			}
		}

		public static void SaveJobs(IJobOwner project, IProjectAdapter projectAdapter)
		{
			//_jobTree.SaveJobs(projectId, projectAdapter);

			var jobs = project.GetJobs();

			var unSavedJobs = jobs.Where(x => !x.OnFile).ToList();

			foreach (var job in unSavedJobs)
			{
				job.ProjectId = project.Id;
				projectAdapter.InsertJob(job);
			}

			var dirtyJobs = jobs.Where(x => x.IsDirty).ToList();

			foreach (var job in dirtyJobs)
			{
				projectAdapter.UpdateJobDetails(job);
			}
		}

		public static void UpdateJobParents(ObjectId oldParentId, ObjectId newParentId, Job[] jobs)
		{
			foreach (var job in jobs)
			{
				if (job.ParentJobId == oldParentId)
				{
					job.ParentJobId = newParentId;
				}
			}
		}

		public static void UpdateCbsParentIds(ObjectId oldParentId, ObjectId newParentId, ColorBandSet[] colorBandSets)
		{
			foreach (var cbs in colorBandSets)
			{
				if (cbs.ParentId == oldParentId)
				{
					cbs.ParentId = newParentId;
				}
			}
		}

		public static void UpdateJobCbsIds(ObjectId oldCbsId, ObjectId newCbsId, Job[] jobs)
		{
			foreach (var job in jobs)
			{
				if (job.ColorBandSetId == oldCbsId)
				{
					job.ColorBandSetId = newCbsId;
				}
			}
		}

	}
}

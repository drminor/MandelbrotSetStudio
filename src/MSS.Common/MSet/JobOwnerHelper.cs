using MongoDB.Bson;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MSS.Common
{
	public static class JobOwnerHelper
	{
		public static bool Save(IJobOwner jobOwner, IProjectAdapter projectAdapter)
		{
			if (!(jobOwner.IsCurrentJobIdChanged || jobOwner.IsDirty))
			{
				Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
				return false;
			}

			if (jobOwner.IsCurrentJobIdChanged)
			{
				if (jobOwner is Project)
				{
					projectAdapter.UpdateProjectCurrentJobId(jobOwner.Id, jobOwner.CurrentJob.Id);
				}
				else if (jobOwner is Poster poster) 
				{
					projectAdapter.UpdatePosterMapArea(poster);
					//projectAdapter.UpdateProjectCurrentJobId(jobOwner.Id, jobOwner.CurrentJob.Id);
				}
				else
				{
					throw new InvalidOperationException("The JobOwner is neither a Project or a Poster");
				}
			}

			if (jobOwner.IsDirty)
			{
				var numberColorBandSetsRemoved = DeleteUnReferencedColorBandSets(jobOwner, projectAdapter);
				Debug.WriteLine($"Removed {numberColorBandSetsRemoved} unused ColorBandSets.");

				//projectAdapter.UpdateProjectName(jobOwner.Id, jobOwner.Name);
				//projectAdapter.UpdateProjectDescription(jobOwner.Id, jobOwner.Description);

				SaveColorBandSets(jobOwner, projectAdapter);
				SaveJobs(jobOwner, projectAdapter);

				jobOwner.MarkAsSaved();
			}

			return true;
		}

		public static IJobOwner CreateCopy(IJobOwner sourceProject, string name, string? description, IProjectAdapter projectAdapter, IMapSectionDuplicator mapSectionDuplicator)
		{
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

			var project = CreateJobOwner(sourceProject, name, description, jobs, colorBandSets, projectAdapter);

			var firstOldIdAndNewJob = jobPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentJob.Id);
			var newCurJob = firstOldIdAndNewJob?.Item2;
			project.CurrentJob = newCurJob ?? Job.Empty;

			return project;
		}

		private static IJobOwner CreateJobOwner(IJobOwner sourceProject, string name, string? description, IList<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IProjectAdapter projectAdapter)
		{
			if (sourceProject is Poster p)
			{
				return CreatePoster(name, description, p.CurrentJob.Id, jobs, colorBandSets, projectAdapter);
			}
			else
			{
				return CreateProject(name, description, jobs, colorBandSets, projectAdapter);
			}
		}

		private static IJobOwner CreateProject(string name, string? description, IList<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IProjectAdapter projectAdapter)
		{
			var project = projectAdapter.CreateProject(name, description, jobs, colorBandSets);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new project.");
			}
			return project;
		}

		private static IJobOwner CreatePoster(string name, string? description, ObjectId sourceJobId, IList<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IProjectAdapter projectAdapter)
		{
			var project = projectAdapter.CreatePoster(name, description, sourceJobId, jobs, colorBandSets);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new poster.");
			}
			return project;
		}

		public static long DeleteMapSectionsForUnsavedJobs(IJobOwner jobOwner, IMapSectionDeleter mapSectionDeleter)
		{
			var result = 0L;

			var jobs = jobOwner.GetJobs().Where(x => !x.OnFile).ToList();

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

		public static int DeleteUnReferencedColorBandSets(IJobOwner jobOwner, IProjectAdapter projectAdapter)
		{
			var colorBandSets = jobOwner.GetColorBandSets();
			var referencedCbsIds = jobOwner.GetJobs().Select(x => x.ColorBandSetId).Distinct();

			var colorBandSetsToRemoved = new List<ColorBandSet>();

			foreach (var cbs in colorBandSets)
			{
				if (!referencedCbsIds.Contains(cbs.Id))
				{
					colorBandSetsToRemoved.Add(cbs);
				}
			}

			foreach (var cbs in colorBandSetsToRemoved)
			{
				_ = colorBandSets.Remove(cbs);
				if (cbs.OnFile)
				{
					_ = projectAdapter.DeleteColorBandSet(cbs.Id);
				}
			}

			return colorBandSetsToRemoved.Count;
		}

		public static void SaveColorBandSets(IJobOwner jobOwner, IProjectAdapter projectAdapter)
		{
			var colorBandSets = jobOwner.GetColorBandSets();
			var unsavedColorBandSets = colorBandSets.Where(x => !x.OnFile).ToList();

			foreach (var cbs in unsavedColorBandSets)
			{
				if (cbs.ProjectId != jobOwner.Id)
				{
					Debug.WriteLine($"WARNING: ColorBandSet has a different projectId than the current projects. ColorBandSetId: {cbs.ProjectId}, current Project: {jobOwner.Id}.");
					var newCbs = cbs.Clone();
					newCbs.ProjectId = jobOwner.Id;
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

		public static void SaveJobs(IJobOwner jobOwner, IProjectAdapter projectAdapter)
		{
			//_jobTree.SaveJobs(projectId, projectAdapter);

			var jobs = jobOwner.GetJobs();

			var unSavedJobs = jobs.Where(x => !x.OnFile).ToList();

			foreach (var job in unSavedJobs)
			{
				job.ProjectId = jobOwner.Id;
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

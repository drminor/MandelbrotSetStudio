using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace MSS.Common
{
	public static class JobOwnerHelper
	{
		#region Create new and Create copy

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

			var project = CreateJobOwner(sourceProject, name, description, jobs.ToList(), colorBandSets, projectAdapter);

			var firstOldIdAndNewJob = jobPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentJob.Id);
			var newCurJob = firstOldIdAndNewJob?.Item2;
			project.CurrentJob = newCurJob ?? Job.Empty;

			return project;
		}

		public static IJobOwner CreateJobOwner(IJobOwner sourceJobOwner, string name, string? description, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IProjectAdapter projectAdapter)
		{
			if (sourceJobOwner is Poster p)
			{
				return CreatePoster(name, description, p.PosterSize, p.CurrentJob.Id, jobs, colorBandSets, projectAdapter);
			}
			else
			{
				// TODO: Have Projects record the Id of the Project from which it was created -- just as Posters do.
				// Then replace the CreateJobOwner "thingy" with a delegate that can be used for either.
				return CreateProject(name, description, jobs, colorBandSets, projectAdapter);
			}
		}

		private static IJobOwner CreateProject(string name, string? description, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IProjectAdapter projectAdapter)
		{
			var project = projectAdapter.CreateProject(name, description, jobs, colorBandSets);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new project.");
			}
			return project;
		}

		private static IJobOwner CreatePoster(string name, string? description, SizeInt posterSize, ObjectId sourceJobId, List<Job> jobs, IEnumerable<ColorBandSet> colorBandSets, IProjectAdapter projectAdapter)
		{
			var project = projectAdapter.CreatePoster(name, description, posterSize, sourceJobId, jobs, colorBandSets);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new poster.");
			}
			return project;
		}

		#endregion

		#region Save

		public static bool SaveProject(Project project, IProjectAdapter projectAdapter)
		{
			if (!(project.IsCurrentJobIdChanged || project.IsDirty))
			{
				Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
				return true;
			}

			if (project.IsCurrentJobIdChanged)
			{
				projectAdapter.UpdateProjectCurrentJobId(project.Id, project.CurrentJob.Id);
			}

			if (project.IsDirty)
			{
				var numberColorBandSetsRemoved = DeleteUnReferencedColorBandSets(project, projectAdapter);
				Debug.WriteLine($"Removed {numberColorBandSetsRemoved} unused ColorBandSets.");

				//projectAdapter.UpdateProjectName(jobOwner.Id, jobOwner.Name);
				//projectAdapter.UpdateProjectDescription(jobOwner.Id, jobOwner.Description);

				SaveColorBandSets(project, projectAdapter);
				SaveJobs(project, projectAdapter);
			}

			return true;
		}

		public static bool SavePoster(Poster poster, IProjectAdapter projectAdapter)
		{
			if (!(poster.IsCurrentJobIdChanged || poster.IsDirty))
			{
				Debug.WriteLine($"WARNING: Not Saving, IsDirty and IsCurrentJobChanged are both reset.");
				return true; // Our caller interprets false as a critical error.
			}

			if (poster.IsCurrentJobIdChanged)
			{
				projectAdapter.UpdatePosterCurrentJobId(poster.Id, poster.CurrentJob.Id);
			}

			if (poster.IsDirty)
			{
				var numberColorBandSetsRemoved = DeleteUnReferencedColorBandSets(poster, projectAdapter);
				Debug.WriteLine($"Removed {numberColorBandSetsRemoved} unused ColorBandSets.");

				projectAdapter.UpdatePosterMapArea(poster);

				SaveColorBandSets(poster, projectAdapter);
				SaveJobs(poster, projectAdapter);
			}

			return true;
		}

		private static void SaveColorBandSets(IJobOwner jobOwner, IProjectAdapter projectAdapter)
		{
			var colorBandSets = jobOwner.GetColorBandSets();
			var unsavedColorBandSets = colorBandSets.Where(x => !x.OnFile).ToList();

			foreach (var cbs in unsavedColorBandSets)
			{
				if (cbs.ProjectId != jobOwner.Id)
				{
					Debug.WriteLine($"WARNING: ColorBandSet has a different projectId than the current projects. ColorBandSetId: {cbs.ProjectId}, current Project: {jobOwner.Id}.");
					var newCbs = cbs.CreateNewCopy();
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

		private static void SaveJobs(IJobOwner jobOwner, IProjectAdapter projectAdapter)
		{
			//_jobTree.SaveJobs(projectId, projectAdapter);

			var jobs = jobOwner.GetJobs();

			var unSavedJobs = jobs.Where(x => !x.OnFile).ToList();

			foreach (var job in unSavedJobs)
			{
				job.OwnerId = jobOwner.Id;
				projectAdapter.InsertJob(job);
			}

			var dirtyJobs = jobs.Where(x => x.IsDirty).ToList();

			foreach (var job in dirtyJobs)
			{
				projectAdapter.UpdateJobDetails(job);
			}
		}

		private static void UpdateJobParents(ObjectId oldParentId, ObjectId newParentId, Job[] jobs)
		{
			foreach (var job in jobs)
			{
				if (job.ParentJobId == oldParentId)
				{
					job.ParentJobId = newParentId;
				}
			}
		}

		private static void UpdateCbsParentIds(ObjectId oldParentId, ObjectId newParentId, ColorBandSet[] colorBandSets)
		{
			foreach (var cbs in colorBandSets)
			{
				if (cbs.ParentId == oldParentId)
				{
					cbs.ParentId = newParentId;
				}
			}
		}

		private static void UpdateJobCbsIds(ObjectId oldCbsId, ObjectId newCbsId, Job[] jobs)
		{
			foreach (var job in jobs)
			{
				if (job.ColorBandSetId == oldCbsId)
				{
					job.ColorBandSetId = newCbsId;
				}
			}
		}

		#endregion

		#region Delete MapSections and ColorBandSets

		public static long DeleteMapSectionsForUnsavedJobs(IJobOwner jobOwner, IMapSectionDeleter mapSectionDeleter)
		{
			var result = 0L;

			var jobs = jobOwner.GetJobs().Where(x => !x.OnFile).ToList();

			foreach (var job in jobs)
			{
				var numberDeleted = mapSectionDeleter.DeleteMapSectionsForJob(job.Id, GetJobOwnerType(jobOwner));
				if (numberDeleted.HasValue)
				{
					result += numberDeleted.Value;
				}
			}

			return result;
		}

		public static long DeleteMapSectionsForJobIds(IList<ObjectId> jobIds, JobOwnerType jobOwnerType, IMapSectionDeleter mapSectionDeleter)
		{
			var result = 0L;

			foreach (var jobId in jobIds)
			{
				var numberDeleted = mapSectionDeleter.DeleteMapSectionsForJob(jobId, jobOwnerType);
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

		#endregion

		public static JobOwnerType GetJobOwnerType(IJobOwner jobOwner)
		{
			if (jobOwner is Project)
			{
				return JobOwnerType.Project;
			}
			else if (jobOwner is Poster)
			{
				return JobOwnerType.Poster;
			}
			else if (jobOwner is IImageBuilder)
			{
				return JobOwnerType.ImageBuilder;
			}
			else if (jobOwner is IBitmapBuilder)
			{
				return JobOwnerType.BitmapBuilder;
			}
			else
			{
				return JobOwnerType.Undetermined;
			}
		}

	}
}

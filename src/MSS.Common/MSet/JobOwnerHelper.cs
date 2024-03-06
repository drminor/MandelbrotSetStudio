using MongoDB.Bson;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

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

				var numberJobMapSectionRefsCreated = mapSectionDuplicator.DuplicateJobMapSections(formerJobId, sourceProject.OwnerType, newJobId);
				Debug.WriteLine($"{numberJobMapSectionRefsCreated} new JobMapSectionRecords were created as Job: {formerJobId} was duplicated.");
			}

			var colorBandSetPairs = sourceProject.GetColorBandSets().Select(x => new Tuple<ObjectId, ColorBandSet>(x.Id, x.CreateNewCopy())).ToArray();
			var colorBandSets = colorBandSetPairs.Select(x => x.Item2).ToArray();

			var timcrs = sourceProject.GetTargetIterationColorMapRecords();
			
			foreach (var oldIdAndNewCbs in colorBandSetPairs)
			{
				UpdateCbsNames(sourceProject.Name, name, colorBandSets);
				UpdateCbsParentIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, colorBandSets);
				UpdateJobCbsIds(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id, jobs);
				UpdateTargetIterationColorMapRecords(oldIdAndNewCbs.Item1, oldIdAndNewCbs.Item2.Id/*, oldIdAndNewCbs.Item2.ColorBandsSerialNumber*/, timcrs);
			}

			var lookupColorMapByTargetIteration = CreateLookupColorMapByTargetIteration(timcrs);

			var project = CreateJobOwner(sourceProject, name, description, jobs.ToList(), colorBandSets.ToList(), lookupColorMapByTargetIteration, projectAdapter);

			var firstOldIdAndNewJob = jobPairs.FirstOrDefault(x => x.Item1 == sourceProject.CurrentJob.Id);
			var newCurJob = firstOldIdAndNewJob?.Item2;
			project.CurrentJob = newCurJob ?? Job.Empty;

			return project;
		}

		public static IJobOwner CreateJobOwner(IJobOwner sourceJobOwner, string name, string? description, List<Job> jobs, List<ColorBandSet> colorBandSets, 
			Dictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration, IProjectAdapter projectAdapter)
		{
			if (sourceJobOwner is Poster p)
			{
				return CreatePoster(name, description, p.PosterSize, p.CurrentJob.Id, jobs, colorBandSets, lookupColorMapByTargetIteration, projectAdapter);
			}
			else
			{
				return CreateProject(name, description, jobs, colorBandSets, lookupColorMapByTargetIteration, projectAdapter);
			}
		}

		private static IJobOwner CreateProject(string name, string? description, List<Job> jobs, List<ColorBandSet> colorBandSets, 
			Dictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration, IProjectAdapter projectAdapter)
		{
			var project = projectAdapter.CreateProject(name, description, jobs, colorBandSets, lookupColorMapByTargetIteration);

			if (project is null)
			{
				throw new InvalidOperationException("Could not create the new project.");
			}
			return project;
		}

		private static IJobOwner CreatePoster(string name, string? description, SizeDbl posterSize, ObjectId sourceJobId, List<Job> jobs, List<ColorBandSet> colorBandSets, 
			Dictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration, IProjectAdapter projectAdapter)
		{
			var project = projectAdapter.CreatePoster(name, description, posterSize, sourceJobId, jobs, colorBandSets, lookupColorMapByTargetIteration);

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

				projectAdapter.UpdateProjectTargetIterationMap(project.Id, project.LastAccessedUtc, project.GetTargetIterationColorMapRecords().ToArray());
			}

			return true;
		}

		public static bool SavePoster(Poster poster, IProjectAdapter projectAdapter)
		{
			if (poster.IsDirty)
			{
				var numberColorBandSetsRemoved = DeleteUnReferencedColorBandSets(poster, projectAdapter);
				Debug.WriteLine($"Removed {numberColorBandSetsRemoved} unused ColorBandSets.");

				projectAdapter.UpdatePosterMapArea(poster);

				SaveColorBandSets(poster, projectAdapter);
				SaveJobs(poster, projectAdapter);

				poster.MarkAsSaved();
			}
			else if (poster.IsCurrentJobIdChanged)
			{
				Debug.WriteLine($"WARNING: IsCurrentJobChanged but IsDirty is false. Not saving any jobs.");
				projectAdapter.UpdatePosterMapArea(poster); // Includes DisplayPosition and Display Zoom.

				poster.MarkAsSaved();
			}
			else
			{
				Debug.WriteLine($"WARNING: IsDirty and IsCurrentJobChanged are both reset. Only Saving the Poster's Display Position and Zoom.");
				projectAdapter.UpdatePosterDisplayPositionAndZoom(poster);
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
					Debug.WriteLine($"WARNING: ColorBandSet has a different projectId than the current projects. ColorBandSetId: {cbs.ProjectId}, current Project: {jobOwner.Id}. ColorBandSet Serial Number: {cbs.ColorBandsSerialNumber}.");
					var newCbs = cbs.CreateNewCopy();
					newCbs.ProjectId = jobOwner.Id;
					newCbs.AssignNewSerialNumber();
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

		private static void UpdateCbsNames(string oldName, string newName, ColorBandSet[] colorBandSets)
		{
			foreach (var cbs in colorBandSets)
			{
				if (cbs.Name == oldName)
				{
					cbs.Name = newName;
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

		private static void UpdateTargetIterationColorMapRecords(ObjectId oldCbsId, ObjectId newCbsId/*, Guid newSerialNumber*/, List<TargetIterationColorMapRecord> targetIterationColorMapRecords)
		{
			for (var i = 0; i < targetIterationColorMapRecords.Count; i++)
			{
				var ticmr = targetIterationColorMapRecords[i];

				if (ticmr.ColorBandSetId == oldCbsId)
				{
					var newTicmr = new TargetIterationColorMapRecord(ticmr.TargetIterations, newCbsId, ticmr.DateCreated);

					targetIterationColorMapRecords[i] = newTicmr;
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
				var numberDeleted = mapSectionDeleter.DeleteMapSectionsForJob(job.Id);
				if (numberDeleted.HasValue)
				{
					result += numberDeleted.Value;
				}
			}

			return result;
		}

		public static long DeleteMapSectionsForJobIds(IList<ObjectId> jobIds, OwnerType jobOwnerType, IMapSectionDeleter mapSectionDeleter)
		{
			var result = 0L;

			foreach (var jobId in jobIds)
			{
				var numberDeleted = mapSectionDeleter.DeleteMapSectionsForJob(jobId);
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

		#region Load ColorBandSet 

		public static ColorBandSet LoadColorBandSet(ColorBandSet? currentColorBandSet, int targetIterations, string operationDescription, List<ColorBandSet> colorBandSets, IDictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration)
		{
			//var targetIterations = job.MapCalcSettings.TargetIterations;

			ColorBandSet? result;

			if (lookupColorMapByTargetIteration.TryGetValue(targetIterations, out var targetIterationColorMap))
			{
				var foundId = targetIterationColorMap.ColorBandSetId;

				if (currentColorBandSet != null && currentColorBandSet.Id == foundId)
				{
					result = currentColorBandSet;
					Debug.WriteLine($"JobOwnerHelper found ColorBandSetId = {foundId} for Project Current Job having TargetIteration: {targetIterations}. The Current ColorBandSet is the default.");
				}
				else
				{
					result = colorBandSets.FirstOrDefault(x => x.Id == targetIterationColorMap.ColorBandSetId);
					Debug.WriteLine($"JobOwnerHelper found ColorBandSetId = {foundId} for Project Current Job having TargetIteration: {targetIterations}. Returning a ColorBandSet from the list of CBS on the Project.");
				}
			}
			else
			{
				result = null;
			}

			if (result == null)
			{
				throw new InvalidOperationException($"Was unable to LoadColorBandSet using the LookupColorMapByTargetIteration while {operationDescription}.");
			}

			return result;
		}

		public static ColorBandSet FindOrCreateColorBandSet(ObjectId colorBandSetId, int targetIterations, string operationDescription, List<ColorBandSet> colorBandSets, out bool wasUpdated, out bool wasCreated)
		{
			var result = GetColorBandSetById(colorBandSetId, colorBandSets);

			if (result == null || result.HighCutoff != targetIterations)
			{
				wasUpdated = true;

				string msg;
				if (result == null)
				{
					msg = $"WARNING: The ColorBandSetId {colorBandSetId} of the current job was not found {operationDescription}."; //as the project is being constructed
				}
				else
				{
					msg = $"WARNING: The Current Job's ColorBandSet {colorBandSetId} has a HighCutoff that is different than that Job's target iteration." +
						$"Loading the best matching ColorBandSet from the same project {operationDescription}.";
				}

				Debug.WriteLine(msg);

				result = FindOrCreateColorBandSetForTargetIterations(targetIterations, colorBandSets, out wasCreated);
				//job.ColorBandSetId = result.Id;
				//LastUpdatedUtc = DateTime.UtcNow;
			}
			else
			{
				wasUpdated = false;
				wasCreated = false;
			}

			return result;
		}

		private static ColorBandSet? GetColorBandSetById(ObjectId colorBandSetId, List<ColorBandSet> colorBandSets)
		{
			var result = colorBandSets.FirstOrDefault(x => x.Id == colorBandSetId);
			if (result == null)
			{
				Debug.WriteLine($"WARNING: The job's current ColorBandSet: {colorBandSetId} does not exist in the Project list of ColorBandSets.");
			}

			return result;
		}

		private static ColorBandSet FindOrCreateColorBandSetForTargetIterations(int targetIterations, List<ColorBandSet> colorBandSets, out bool wasCreated)
		{
			var colorBandSet = ColorBandSetHelper.GetBestMatchingColorBandSet(targetIterations, colorBandSets);

			if (colorBandSet.HighCutoff != targetIterations)
			{
				var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(colorBandSet, targetIterations);
				Debug.WriteLine($"WARNING: Creating new adjusted ColorBandSet: {adjustedColorBandSet.Id} to replace {colorBandSet.Id}.");

				colorBandSets.Add(adjustedColorBandSet);
				colorBandSet = adjustedColorBandSet;
				wasCreated = true;
			}
			else
			{
				wasCreated = false;
			}

			return colorBandSet;
		}

		public static bool CreateLookupColorMapByTargetIteration(List<Job> jobs, List<ColorBandSet> colorBandSets, Dictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration, string desc)
		{
			var updateWasMade = false;


			var ticmRecs = lookupColorMapByTargetIteration.Values.ToArray();

			for (var i = 0; i < ticmRecs.Length; i++)
			{
				var ticmRec = ticmRecs[i];

				if (!colorBandSets.Any(x => x.Id == ticmRec.ColorBandSetId))
				{
					lookupColorMapByTargetIteration.Remove(ticmRec.TargetIterations);
				}
			}

			foreach (var job in jobs)
			{
				var targetIterations = job.MapCalcSettings.TargetIterations;

				if (!lookupColorMapByTargetIteration.ContainsKey(targetIterations))
				{
					var match = FindOrCreateColorBandSet(job.ColorBandSetId, targetIterations, desc, colorBandSets, out var wasUpdated, out var wasCreated);
					var ticmRec = new TargetIterationColorMapRecord(targetIterations, match.Id, match.DateCreated);

					lookupColorMapByTargetIteration.Add(targetIterations, ticmRec);

					updateWasMade = true;
				}
			}

			return updateWasMade;
		}

		public static Dictionary<int, TargetIterationColorMapRecord> CreateLookupColorMapByTargetIteration(Job job, ColorBandSet colorBandSet)
		{
			Debug.Assert(job.MapCalcSettings.TargetIterations == colorBandSet.HighCutoff, "The Jobs's TargetIterations does not match the ColorBandSet's High Cutoff.");

			var result = new Dictionary<int, TargetIterationColorMapRecord>();

			result.Add(job.MapCalcSettings.TargetIterations, new TargetIterationColorMapRecord(job.MapCalcSettings.TargetIterations, colorBandSet.Id, colorBandSet.DateCreated));

			return result;
		}

		public static Dictionary<int, TargetIterationColorMapRecord> CreateLookupColorMapByTargetIteration(IEnumerable<TargetIterationColorMapRecord>? targetIterationColorMapRecords)
		{
			if (targetIterationColorMapRecords == null)
			{
				return new Dictionary<int, TargetIterationColorMapRecord>();
			}
			else
			{
				var result = new Dictionary<int, TargetIterationColorMapRecord>(targetIterationColorMapRecords.Select(x => new KeyValuePair<int, TargetIterationColorMapRecord>(x.TargetIterations, x)));
				return result;
			}
		}

		public static void AddIteratationColorMapRecord(ColorBandSet colorBandSet, IDictionary<int, TargetIterationColorMapRecord> lookupColorMapByTargetIteration, bool makeDefault)
		{
			if (makeDefault)
			{
				var ticmr = new TargetIterationColorMapRecord(colorBandSet.HighCutoff, colorBandSet.Id, colorBandSet.DateCreated);

				// Add or update
				if (lookupColorMapByTargetIteration.ContainsKey(colorBandSet.HighCutoff))
					lookupColorMapByTargetIteration[colorBandSet.HighCutoff] = ticmr;
				else
					lookupColorMapByTargetIteration.Add(colorBandSet.HighCutoff, ticmr);
			}
			else
			{
				// Add only if there is no entry for the targetIteration.
				if (!lookupColorMapByTargetIteration.ContainsKey(colorBandSet.HighCutoff))
				{
					var ticmr = new TargetIterationColorMapRecord(colorBandSet.HighCutoff, colorBandSet.Id, colorBandSet.DateCreated);
					lookupColorMapByTargetIteration.Add(colorBandSet.HighCutoff, ticmr);
				}
			}
		}

		#endregion

		public static OwnerType GetJobOwnerType(IJobOwner jobOwner)
		{
			if (jobOwner is Project)
			{
				return OwnerType.Project;
			}
			else if (jobOwner is Poster)
			{
				return OwnerType.Poster;
			}
			else
			{
				throw new NotImplementedException($"Cannot determined the JobOwnerType from the Project or Poster: {jobOwner}.");
			}
		}

	}
}

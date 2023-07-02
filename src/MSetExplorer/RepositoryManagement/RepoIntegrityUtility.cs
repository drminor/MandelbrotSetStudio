using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer.RepositoryManagement
{
	internal class RepoIntegrityUtility
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private readonly MapJobHelper _mapJobHelper;

		public RepoIntegrityUtility(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;
		}

		#region Populate JobMapSections

		public string PopulateJobMapSections(OwnerType jobOwnerType)
		{
			string report;

			if (jobOwnerType == OwnerType.Project)
			{
				report = ProjectAndMapSectionHelper.PopulateJobMapSectionsForAllProjects(_projectAdapter, _mapSectionAdapter, _mapJobHelper);
			}
			else if (jobOwnerType == OwnerType.Poster)
			{
				report = ProjectAndMapSectionHelper.PopulateJobMapSectionsForAllPosters(_projectAdapter, _mapSectionAdapter, _mapJobHelper);
			}
			else
			{
				throw new InvalidOperationException($"The Job Owner Type:{jobOwnerType} is not supported.");
			}

			return report;
		}

		public string CreateJobMapSectionsReferenceReport()
		{
			if (_mapSectionAdapter is MapSectionAdapter ma)
			{
				var report = ma.GetJobMapSectionsToMapSectionReferenceReport();
				return report;
			}
			else
			{
				return "Report not available.";
			}
		}

		#endregion

		#region Find And Delete Jobs Not Referenced by any Project or Poster

		public void FindAndDeleteOrphanJobs()
		{
			var report = FindOrphanJobs(out var jobIdsWithNoOwner, out var jobIdsToBeAssignedJOTofPoster, out var jobIdsToBeAssignedJOTofProject);
			Debug.WriteLine(report);

			var countJobsToUpdateToPoster = jobIdsToBeAssignedJOTofPoster.Count;

			if (countJobsToUpdateToPoster > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to update the job type for the {countJobsToUpdateToPoster} Jobs that belong to Posters but have a Project JobOwnerType?.", "Update Job Types?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberUpdated = UpdateJobOwnerTypeForMany(jobIdsToBeAssignedJOTofPoster, OwnerType.Poster);
					MessageBox.Show($"{numberUpdated} Job records were updated to have JobOwnerType = Poster.");
				}
			}

			var countJobsToUpdateToProject = jobIdsToBeAssignedJOTofProject.Count;

			if (countJobsToUpdateToProject > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to update the job type for the {countJobsToUpdateToProject} Jobs that belong to Projects but have a Poster JobOwnerType?.", "Update Job Types?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberUpdated = UpdateJobOwnerTypeForMany(jobIdsToBeAssignedJOTofPoster, OwnerType.Project);
					MessageBox.Show($"{numberUpdated} Job records were updated to have JobOwnerType = Project.");
				}
			}

			var countOrphanJobs = jobIdsWithNoOwner.Count;

			if (countOrphanJobs > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOrphanJobs} Jobs that are not referenced by any Project or Poster?", "Delete Job Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					//var numberDeleted = JobOwnerHelper.DeleteMapSectionsForJobIds _mapSectionAdapter.DeleteMapSectionsInList(mapSectionIdsWithNoJob);

					var numberMapSectionsDeleted = ProjectAndMapSectionHelper.DeleteJobsOnFile(jobIdsWithNoOwner, _projectAdapter, _mapSectionAdapter);
					MessageBox.Show($"{countOrphanJobs} Job Records and {numberMapSectionsDeleted} MapSections were deleted.");
				}
			}
		}

		private long UpdateJobOwnerTypeForMany(List<ObjectId> jobIds, OwnerType jobOwnerType)
		{
			foreach(var jobId in jobIds)
			{
				_projectAdapter.UpdateJobOwnerType(jobId, jobOwnerType);
			}

			return jobIds.Count;
		}

		public string FindOrphanJobs(out List<ObjectId> jobIdsWithNoOwner, out List<ObjectId> jobIdsToBeAssignedJOTofPoster, out List<ObjectId> jobIdsToBeAssignedJOTofProject)
		{
			var report = ProjectAndMapSectionHelper.FindOrphanJobs(_projectAdapter, out jobIdsWithNoOwner, out jobIdsToBeAssignedJOTofPoster, out jobIdsToBeAssignedJOTofProject);

			return report;
		}

		#endregion

		#region Find And Delete Map Sections Not Referenced by any Job

		public void FindAndDeleteOrphanMapSections()
		{
			var report = FindOrphanMapSections(out var mapSectionIdsWithNoJob);
			Debug.WriteLine(report);

			var countOrphanMapSections = mapSectionIdsWithNoJob.Count;

			if (countOrphanMapSections > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOrphanMapSections} MapSections that are not referenced by any job?", "Delete JobMapSection Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberDeleted = _mapSectionAdapter.DeleteMapSectionsInList(mapSectionIdsWithNoJob);
					MessageBox.Show($"{numberDeleted} JobMapSections were deleted.");
				}
			}
		}

		public string FindOrphanMapSections(out List<ObjectId> mapSectionIdsWithNoJob)
		{
			var report = ProjectAndMapSectionHelper.FindOrphanMapSections(_mapSectionAdapter, out mapSectionIdsWithNoJob);
			return report;
		}

		#endregion

		#region Check and Delete JobMapSections referencing a non-extant Job Record

		public void CheckAndDeleteJobRefsFromJobMapCollection()
		{
			var report = CheckJobRefsFromJobMapCollection(out var jobMapSectionIdsWithMissingJobRecord, out var subdivisionIdsForMissingJobs);
			Debug.WriteLine(report);

			var countOfRecordsWithMissingJob = jobMapSectionIdsWithMissingJobRecord.Count;

			if (countOfRecordsWithMissingJob > 0)
			{
				var formattedSubdivisionList = string.Join("\n", subdivisionIdsForMissingJobs);
				Debug.WriteLine($"SubdivisionIds for the records with a missing JobRecord:\n{formattedSubdivisionList}\n");

				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOfRecordsWithMissingJob} JobMapSection records referencing a Job record that does not exist?", "Delete JobMapSection Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberDeleted = _mapSectionAdapter.DeleteJobMapSectionsInList(jobMapSectionIdsWithMissingJobRecord);
					MessageBox.Show($"{numberDeleted} JobMapSections were deleted.");
				}
			}
		}

		public string CheckJobRefsFromJobMapCollection(out List<ObjectId> jobMapSectionIdsWithMissingJobRecord, out List<ObjectId> subdivisionIdsForMissingJobs)
		{
			var report = ProjectAndMapSectionHelper.CheckJobRefsAndSubdivisions(_projectAdapter, _mapSectionAdapter, out jobMapSectionIdsWithMissingJobRecord, out subdivisionIdsForMissingJobs);
			return report;
		}

		#endregion

		#region Check and Delete JobMapSections referencing a non-extant MapSection

		public void CheckAndDeleteMapRefsFromJobMapCollection()
		{
			var report = CheckMapRefsFromJobMapCollection(out var jobMapSectionIdsWithMissingMapSection, out var subdivisionIdsForMissingMapSections);
			Debug.WriteLine(report);

			var countOfRecordsWithMissingMapSection = jobMapSectionIdsWithMissingMapSection.Count;

			if (countOfRecordsWithMissingMapSection > 0)
			{
				var formattedSubdivisionList = string.Join("\n", subdivisionIdsForMissingMapSections);
				Debug.WriteLine($"SubdivisionIds for the records with a missing MapSection:\n{formattedSubdivisionList}\n");

				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOfRecordsWithMissingMapSection} JobMapSection records referencing a MapSection record that does not exist?", "Delete JobMapSection Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberDeleted = _mapSectionAdapter.DeleteJobMapSectionsInList(jobMapSectionIdsWithMissingMapSection);
					MessageBox.Show($"{numberDeleted} JobMapSections were deleted.");
				}
			}
		}

		public string CheckMapRefsFromJobMapCollection(out List<ObjectId> jobMapSectionIdsWithMissingMapSection, out List<ObjectId> subdivisionIdsForMissingMapSections)
		{
			var report = ProjectAndMapSectionHelper.CheckMapRefsAndSubdivisions(_mapSectionAdapter, out jobMapSectionIdsWithMissingMapSection, out subdivisionIdsForMissingMapSections);
			return report;
		}

		#endregion

		#region Find And Delete Subdivision Records Not Referenced by any Job, MapSection or JobMapSection

		public void FindAndDeleteOrphanSubdivisions()
		{
			var report = FindOrphanSubdivisions(out var subdivisionIdsWithNoOwner);
			Debug.WriteLine(report);

			var countOrphanSubdivisions = subdivisionIdsWithNoOwner.Count;

			if (countOrphanSubdivisions > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOrphanSubdivisions} Subdivisions that are not referenced by any Job, MapSection or JobMapSection?", "Delete Subdivision Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberDeleted = _mapSectionAdapter.DeleteSubdivisionsInList(subdivisionIdsWithNoOwner);
					MessageBox.Show($"{numberDeleted} Subdivision Records were deleted.");
				}
			}
		}

		public string FindOrphanSubdivisions(out List<ObjectId> subdivisionIdsWithNoOwner)
		{
			var report = ProjectAndMapSectionHelper.FindOrphanSubdivisions(_mapSectionAdapter, out subdivisionIdsWithNoOwner);
			return report;
		}

		#endregion


	}
}

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

		#region Cleanup Job Map Sections

		public string PopulateJobMapSections(JobOwnerType jobOwnerType)
		{
			string report;

			if (jobOwnerType == JobOwnerType.Project)
			{
				report = ProjectAndMapSectionHelper.PopulateJobMapSectionsForAllProjects(_projectAdapter, _mapSectionAdapter, _mapJobHelper);
			}
			else if (jobOwnerType == JobOwnerType.Poster)
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

		public void FindAndDeleteOrphanJobs(JobOwnerType jobOwnerType)
		{
			var report = FindOrphanJobs(jobOwnerType, out var jobIdsWithNoOwner, out var jobIdsWithOwnerOfWrongType);
			Debug.WriteLine(report);


			var countWrongTypeJobs = jobIdsWithOwnerOfWrongType.Count;

			if (countWrongTypeJobs > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to update the job type for the {countWrongTypeJobs} {jobOwnerType} Jobs that are listed as being of the other type.", "Update Job Types?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					var numberUpdated = UpdateJobOwnerTypeForMany(jobIdsWithOwnerOfWrongType, jobOwnerType);
					MessageBox.Show($"{numberUpdated} Job records were updated to have JobOwnerType = {jobOwnerType}.");
				}
			}

			var countOrphanJobs = jobIdsWithNoOwner.Count;

			if (countOrphanJobs > 0)
			{
				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOrphanJobs} Jobs that are not referenced by any {jobOwnerType}?", "Delete Job Records?",
					MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);

				if (msgBoxResult == MessageBoxResult.Yes)
				{
					//var numberDeleted = JobOwnerHelper.DeleteMapSectionsForJobIds _mapSectionAdapter.DeleteMapSectionsInList(mapSectionIdsWithNoJob);

					var numberMapSectionsDeleted = ProjectAndMapSectionHelper.DeleteJobsOnFile(jobIdsWithNoOwner, _projectAdapter, _mapSectionAdapter);
					MessageBox.Show($"{countOrphanJobs} Job Records and {numberMapSectionsDeleted} MapSections were deleted.");
				}
			}
		}

		private long UpdateJobOwnerTypeForMany(List<ObjectId> jobIds, JobOwnerType jobOwnerType)
		{
			foreach(var jobId in jobIds)
			{
				_projectAdapter.UpdateJobOwnerType(jobId, jobOwnerType);
			}

			return jobIds.Count;
		}

		public string FindOrphanJobs(JobOwnerType jobOwnerType, out List<ObjectId> jobIdsWithNoOwner, out List<ObjectId> jobIdsWithOwnerOfWrongType)
		{
			var report = ProjectAndMapSectionHelper.FindOrphanJobs(_projectAdapter, jobOwnerType, out jobIdsWithNoOwner, out jobIdsWithOwnerOfWrongType);

			return report;
		}

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

		public void CheckAndDeleteJobRefsFromJobMapCollection()
		{
			var report = CheckJobRefsFromJobMapCollection(out var jobMapSectionIdsWithMissingJobRecord, out var subdivisionIdsForMissingJobs);
			Debug.WriteLine(report);

			var countOfRecordsWithMissingMapSection = jobMapSectionIdsWithMissingJobRecord.Count;

			if (countOfRecordsWithMissingMapSection > 0)
			{
				var formattedSubdivisionList = string.Join("\n", subdivisionIdsForMissingJobs);
				Debug.WriteLine($"SubdivisionIds for the records with a missing JobRecord:\n{formattedSubdivisionList}\n");

				var msgBoxResult = MessageBox.Show($"Would you like to delete the {countOfRecordsWithMissingMapSection} JobMapSection records referencing a Job record that does not exist?", "Delete JobMapSection Records?",
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
	}
}

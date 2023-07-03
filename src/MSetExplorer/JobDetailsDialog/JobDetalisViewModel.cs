using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MSetExplorer
{
	public class JobDetailsViewModel : INotifyPropertyChanged
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly MapSectionAdapter _mapSectionAdapter;
		private readonly DeleteNonEssentialMapSectionsDelegate? _deleteNonEssentialMapSectionsFunction;
		//private long _mapSectionCollectionSize;

		private IJobInfo? _selectedJob;

		#endregion

		#region Constructor

		public JobDetailsViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, DeleteNonEssentialMapSectionsDelegate? deleteNonEssentialMapSectionsFunction, ObjectId ownerId, OwnerType ownerType, ObjectId? initialJobId)
		{
			_projectAdapter = projectAdapter;

			if (mapSectionAdapter is MapSectionAdapter ma)
			{
				_mapSectionAdapter = ma;
			}
			else
			{
				throw new InvalidOperationException("The mapSectionAdapter argument must be implemented by the MapSectionAdapter class.");
			}

			_deleteNonEssentialMapSectionsFunction = deleteNonEssentialMapSectionsFunction;

			OwnerId = ownerId;
			OwnerType = ownerType;

			//_mapSectionCollectionSize = _mapSectionAdapter.GetSizeOfCollectionInMB();
			//_mapSectionDocSize = _mapSectionAdapter.GetSizeOfDocZero();

			var jobInfos = _projectAdapter.GetJobInfosForOwner(ownerId);

			JobInfos = new ObservableCollection<IJobInfo>(jobInfos);

			SelectedJobInfo = JobInfos.FirstOrDefault(x => x.Id == initialJobId);

			var view = CollectionViewSource.GetDefaultView(JobInfos);
			_ = view.MoveCurrentTo(SelectedJobInfo);

			//PlayWithStorageModel(OwnerId);
		}

		#endregion

		#region Public Properties

		public ObjectId OwnerId { get; init; }

		public OwnerType OwnerType { get; init; }

		public ObservableCollection<IJobInfo> JobInfos { get; init; }

		public IJobInfo? SelectedJobInfo
		{
			get => _selectedJob;

			set
			{
				_selectedJob = value;
				OnPropertyChanged();
			}
		}

		#endregion

		#region Public Methods

		public bool DeleteSelected(out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			//var posterInfo = SelectedJobInfo;

			//if (posterInfo == null)
			//{
			//	return false;
			//}

			//bool result;
			//if (ProjectAndMapSectionHelper.DeletePoster(posterInfo.PosterId, _projectAdapter, _mapSectionAdapter, out numberOfMapSectionsDeleted))
			//{
			//	_ = PosterInfos.Remove(posterInfo);
			//	result = true;
			//}
			//else
			//{
			//	result = false;
			//}

			var result = false;
			return result;
		}

		//public long TrimSelected(bool agressive)
		//{
		//	if (SelectedJobInfo == null || _deleteNonEssentialMapSectionsFunction == null)
		//	{
		//		return -1;
		//	}

		//	var jobId = SelectedJobInfo.CurrentJobId;
		//	var job = _projectAdapter.GetJob(jobId);
		//	var posterSize = SelectedJobInfo.Size;

		//	var result = _deleteNonEssentialMapSectionsFunction(job, posterSize, agressive);

		//	return result;
		//}

		//public long TrimHeavySelected()
		//{
		//	return -1;
		//}

		#endregion

		#region Private Methods

		public void PlayWithStorageModel(ObjectId ownerId)
		{
			var jobAndSubIds = _projectAdapter.GetJobAndSubdivisionIdsForOwner(ownerId);

			var jobs = new List<StorageModel.Job>();

			foreach (var (jobId, subId) in jobAndSubIds)
			{
				jobs.Add(new StorageModel.Job(jobId, subId));
			}

			var currentJobId = jobs[0].JobId;

			var storageModel = new StorageModel(ownerId, jobs, currentJobId);

			var jobOwner = storageModel.OwnerBeingManaged;

			foreach (var smJob in jobOwner.Jobs)
			{
				var jobMapSectionRecords = _mapSectionAdapter.GetByJobId(smJob.JobId);

				foreach (var jobMapSectionRecord in jobMapSectionRecords)
				{
					var sectionId = jobMapSectionRecord.MapSectionId;

					var sectionSubId = _mapSectionAdapter.GetSubdivisionId(sectionId);

					if (!sectionSubId.HasValue)
					{
						throw new InvalidOperationException($"Cannot get the SubdivisionId from the MapSection with Id: {sectionId}.");
					}

					jobOwner.Sections.Add(new StorageModel.Section(sectionId, sectionSubId.Value));

					var js = new StorageModel.JobSection(jobMapSectionRecord.Id, jobMapSectionRecord.JobId, sectionId)
					{
						JobOwnerType = jobMapSectionRecord.OwnerType,
						SubdivisionId = jobMapSectionRecord.MapSectionSubdivisionId,
						OriginalSourceSubdivisionId = jobMapSectionRecord.JobSubdivisionId,
					};

					jobOwner.JobSections.Add(js);
				}
			}
		}



		#endregion

		#region INotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}

using MongoDB.Bson;
using MSetRepo;
using MSetRepo.Storage;
using MSS.Common;
using MSS.Types;
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
		//private long _mapSectionCollectionSize;

		private IJobInfo? _selectedJob;

		private StorageModel _storageModel;

		#endregion

		#region Constructor

		public JobDetailsViewModel(string ownerName, ObjectId ownerId, OwnerType ownerType, ObjectId currentJobId, DateTime ownerCreationDate, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
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

			OwnerName = ownerName;
			OwnerId = ownerId;
			OwnerType = ownerType;

			//_mapSectionCollectionSize = _mapSectionAdapter.GetSizeOfCollectionInMB();
			//_mapSectionDocSize = _mapSectionAdapter.GetSizeOfDocZero();

			var jobInfos = _projectAdapter.GetJobInfosForOwner(ownerId);

			//var cntr = 0;
			//foreach(var ji in jobInfos)
			//{
			//	Stat1 = cntr++;
			//	Stat2 = cntr += 10;
			//	Stat3 = cntr += 12;
			//}

			JobInfos = new ObservableCollection<IJobInfo>(jobInfos);

			SelectedJobInfo = JobInfos.FirstOrDefault(x => x.Id == currentJobId);

			var view = CollectionViewSource.GetDefaultView(JobInfos);
			_ = view.MoveCurrentTo(SelectedJobInfo);

			_storageModel = CreateStorageModel(OwnerId, ownerType, currentJobId, ownerCreationDate, JobInfos);

			_storageModel.UpdateStats();

			for(int i = 0; i < JobInfos.Count; i++)
			{
				var jobInfo = JobInfos[i];
				jobInfo.NumberOfMapSections = _storageModel.Owner.Jobs[i].NumberOfMapSections;
				
				//jobInfo.PercentageMapSectionsShared = 1.34;
				//jobInfo.PercentageMapSectionsSharedWithSameOwner = 28.3;
			}

			Stat1 = _storageModel.Owner.NumberOfMapSections;
			Stat2 = _storageModel.Owner.NumberOfReducedScale;
			Stat3 = _storageModel.Owner.NumberOfImage;
		}

		#endregion

		#region Public Properties

		public string OwnerName { get; init; }
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

		private int _stat1;
		private int _stat2;
		private int _stat3;

		public int Stat1
		{
			get => _stat1;
			set { _stat1 = value; OnPropertyChanged(); }
		}

		public int Stat2
		{
			get => _stat2;
			set { _stat2 = value; OnPropertyChanged(); }
		}

		public int Stat3
		{
			get => _stat3;
			set { _stat3 = value; OnPropertyChanged(); }
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

		public StorageModel CreateStorageModel(ObjectId ownerId, OwnerType ownerType, ObjectId currentJobId, DateTime ownerCreationDate, IEnumerable<IJobInfo> jobInfos)
		{
			var jobs = jobInfos.Select(x => new Job(x.Id, x.DateCreatedUtc, x.SubdivisionId)).ToList();

			var storageModel = new StorageModel(ownerId, ownerCreationDate, ownerType, jobs, currentJobId);

			foreach (var job in jobs)
			{
				var jobMapSectionRecords = _mapSectionAdapter.GetByJobId(job.JobId);

				var mapSectionIds = jobMapSectionRecords.Select(x => x.MapSectionId);
				var mapSectionCreationDatesAndSubIds = _mapSectionAdapter.GetMapSectionCreationDates(mapSectionIds);
				var sections = mapSectionCreationDatesAndSubIds.Select(x => new Section(x.Item1, x.Item2, x.Item3));
				storageModel.Sections.AddRange(sections);

				var jobSections = jobMapSectionRecords.Select(x => new JobSection(x.Id, x.DateCreatedUtc, x.JobType, x.JobId, x.MapSectionId, new SizeInt(x.BlockIndex.Width, x.BlockIndex.Height), x.IsInverted, x.MapSectionSubdivisionId, x.JobSubdivisionId, x.OwnerType));
				storageModel.JobSections.AddRange(jobSections);
			}

			return storageModel;
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

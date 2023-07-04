using MongoDB.Bson;
using MSetRepo;
using MSetRepo.Storage;
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

using Job = MSetRepo.Storage.Job;

namespace MSetExplorer
{
	public class JobDetailsViewModel : INotifyPropertyChanged
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly MapSectionAdapter _mapSectionAdapter;
		//private long _mapSectionCollectionSize;

		private readonly StorageModel _storageModel;

		private IJobInfo? _selectedJob;

		private int _stat1;
		private int _stat2;
		private int _stat3;

		#endregion

		#region Constructor

		public JobDetailsViewModel(IJobOwnerInfo jobOwnerInfo, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter)
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

			JobOwnerInfo = jobOwnerInfo;

			//_mapSectionCollectionSize = _mapSectionAdapter.GetSizeOfCollectionInMB();
			//_mapSectionDocSize = _mapSectionAdapter.GetSizeOfDocZero();

			var jobInfos = _projectAdapter.GetJobInfosForOwner(jobOwnerInfo.OwnerId);

			JobInfos = new ObservableCollection<IJobInfo>(jobInfos);
			SelectedJobInfo = JobInfos.FirstOrDefault(x => x.Id == jobOwnerInfo.CurrentJobId);

			var view = CollectionViewSource.GetDefaultView(JobInfos);
			_ = view.MoveCurrentTo(SelectedJobInfo);

			_storageModel = CreateStorageModel(jobOwnerInfo, JobInfos);
			UpdateStats();
		}

		#endregion

		#region Public Properties

		public IJobOwnerInfo JobOwnerInfo { get; init; }

		public string OwnerName => JobOwnerInfo.Name;
		public ObjectId CurrentJobId => JobOwnerInfo.CurrentJobId;

		//public ObjectId OwnerId { get; init; }
		//public OwnerType OwnerType { get; init; }

		//public ObjectId CurrentJobId { get; init; }

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

		public bool TheCurrentJobIsSelected => SelectedJobInfo == null ? false : SelectedJobInfo.Id == CurrentJobId;

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

		public bool DeleteSelectedJob(out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			if (SelectedJobInfo == null)
			{
				return false;
			}

			var selectedJobId = SelectedJobInfo.Id;

			if (SelectedJobInfo.Id == _storageModel.Owner.CurrentJobId)
			{
				return false;
			}

			numberOfMapSectionsDeleted = ProjectAndMapSectionHelper.DeleteJobOnFile(selectedJobId, _projectAdapter, _mapSectionAdapter);

			return true;
		}

		public long DeleteAllMapSectionsForSelectedJob()
		{
			if (SelectedJobInfo == null)
			{
				return -1;
			}

			var selectedJobId = SelectedJobInfo.Id;

			var result = _mapSectionAdapter.DeleteMapSectionsForJob(selectedJobId) ?? 0;

			return result;
		}

		public long TrimMapSectionsForSelectedJob()
		{
			if (SelectedJobInfo == null)
			{
				return -1;
			}

			var selectedJobId = SelectedJobInfo.Id;

			var nonEssentialJobTypes = new JobType[] { JobType.ReducedScale, JobType.SizeEditorPreview };
			var result = _mapSectionAdapter.DeleteMapSectionsForJobHavingJobTypes(selectedJobId, nonEssentialJobTypes) ?? 0;

			return result;
		}

		#endregion

		#region Private Methods

		//private StorageModel CreateStorageModel(ObjectId ownerId, OwnerType ownerType, ObjectId currentJobId, DateTime ownerCreationDate, IEnumerable<IJobInfo> jobInfos)
		//{

		//}


		private StorageModel CreateStorageModel(IJobOwnerInfo jobOwnerInfo, IEnumerable<IJobInfo> jobInfos)
		{
			//ObjectId ownerId, OwnerType ownerType, ObjectId currentJobId, DateTime ownerCreationDate
			//var jobs = jobInfos.Select(x => new Job(x.Id, x.DateCreatedUtc, x.SubdivisionId)).ToList();

			//var storageModel = new StorageModel(ownerId, ownerCreationDate, ownerType, jobs, currentJobId);

			var jobs = new List<Job>();

			var sectionsAcc = new List<Section>();
			var jobSectionsAcc = new List<JobSection>();

			foreach (var jobInfo in jobInfos)
			{
				var jobMapSectionRecords = _mapSectionAdapter.GetByJobId(jobInfo.Id);

				var mapSectionIds = jobMapSectionRecords.Select(x => x.MapSectionId);
				var mapSectionCreationDatesAndSubIds = _mapSectionAdapter.GetMapSectionCreationDates(mapSectionIds);
				var sections = mapSectionCreationDatesAndSubIds.Select(x => new Section(x.Item1, x.Item2, x.Item3));
				//storageModel.Sections.AddRange(sections);
				sectionsAcc.AddRange(sections);

				var jobSections = jobMapSectionRecords.Select(x => new JobSection(x.Id, x.DateCreatedUtc, x.JobType, x.JobId, x.MapSectionId, new SizeInt(x.BlockIndex.Width, x.BlockIndex.Height), x.IsInverted, x.MapSectionSubdivisionId, x.JobSubdivisionId, x.OwnerType));
				//storageModel.JobSections.AddRange(jobSections);
				jobSectionsAcc.AddRange(jobSections);

				var distinctMapSectionIds = mapSectionIds.Distinct().ToList();

				jobs.Add(new Job(jobInfo.Id, jobInfo.DateCreatedUtc, jobInfo.SubdivisionId, distinctMapSectionIds));
			}

			var jobOwner = new JobOwner(jobOwnerInfo, jobs);

			var storageModel = new StorageModel(jobOwner, sectionsAcc, jobSectionsAcc);

			return storageModel;
		}

		private void UpdateStats()
		{
			_storageModel.UpdateStats();

			for (int i = 0; i < JobInfos.Count; i++)
			{
				var jobInfo = JobInfos[i];
				jobInfo.NumberOfMapSections = _storageModel.Owner.Jobs[i].NumberOfMapSections;
				jobInfo.NumberOfFullScale = _storageModel.Owner.Jobs[i].NumberOfFullScale;
				jobInfo.NumberOfReducedScale = _storageModel.Owner.Jobs[i].NumberOfReducedScale;
				jobInfo.NumberOfImage = _storageModel.Owner.Jobs[i].NumberOfImage;
				jobInfo.NumberOfSizeEditorPreview = _storageModel.Owner.Jobs[i].NumberOfSizeEditorPreview;

				//jobInfo.PercentageMapSectionsShared = 1.34;
				//jobInfo.PercentageMapSectionsSharedWithSameOwner = 28.3;
			}

			Stat1 = _storageModel.Owner.NumberOfMapSections;
			Stat2 = _storageModel.Owner.NumberOfReducedScale;
			Stat3 = _storageModel.Owner.NumberOfImage;
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

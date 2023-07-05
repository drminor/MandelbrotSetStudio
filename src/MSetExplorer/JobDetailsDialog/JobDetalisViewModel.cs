using MongoDB.Bson;
using MSetRepo;
using MSetRepo.Storage;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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

		private long _mapSectionCollectionSize;

		private readonly StorageModel _storageModel;

		private IJobInfo? _selectedJob;

		private int _stat1;
		private int _stat2;
		private int _stat3;

		private double _stat4;
		private double _stat5;
		private double _stat6;

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

			_mapSectionCollectionSize = _mapSectionAdapter.GetSizeOfCollectionInMB();
			//_mapSectionDocSize = _mapSectionAdapter.GetSizeOfDocZero();

			JobOwnerInfo = jobOwnerInfo;
			var jobInfos = _projectAdapter.GetJobInfosForOwner(jobOwnerInfo.OwnerId, jobOwnerInfo.CurrentJobId);

			_storageModel = CreateStorageModel(jobOwnerInfo, jobInfos);

			_jobInfos = new ObservableCollection<IJobInfo>(jobInfos);
			UpdateStats(_jobInfos);

			OnPropertyChanged(nameof(JobInfos));

			SelectedJobInfo = JobInfos.FirstOrDefault(x => x.Id == jobOwnerInfo.CurrentJobId) ?? JobInfos.FirstOrDefault(x => x.IsCurrentOnOwner) ?? JobInfos.FirstOrDefault();

			var view = CollectionViewSource.GetDefaultView(JobInfos);
			_ = view.MoveCurrentTo(SelectedJobInfo);
		}

		#endregion

		#region Public Properties

		public long MapSectionCollectionSize
		{
			get => _mapSectionCollectionSize;

			set
			{
				_mapSectionCollectionSize = value;
				OnPropertyChanged();
			}
		}

		public IJobOwnerInfo JobOwnerInfo { get; init; }

		public string OwnerName => JobOwnerInfo.Name;
		public ObjectId CurrentJobId => JobOwnerInfo.CurrentJobId;

		private ObservableCollection<IJobInfo> _jobInfos;

		public ObservableCollection<IJobInfo> JobInfos
		{
			get => _jobInfos;
			set
			{
				if (value != _jobInfos)
				{
					_jobInfos = value;
					OnPropertyChanged();
				}
			}
		}

		public IJobInfo? SelectedJobInfo
		{
			get => _selectedJob;

			set
			{
				if (value != _selectedJob)
				{
					_selectedJob = value;
					UpdateSharedPercentages(_selectedJob);

					OnPropertyChanged();
				}
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

		public double Stat4
		{
			get => _stat4;
			set { _stat4 = value; OnPropertyChanged(); }
		}

		public double Stat5
		{
			get => _stat5;
			set { _stat5 = value; OnPropertyChanged(); }
		}

		public double Stat6
		{
			get => _stat6;
			set { _stat6 = value; OnPropertyChanged(); }
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

		private StorageModel CreateStorageModel(IJobOwnerInfo jobOwnerInfo, IEnumerable<IJobInfo> jobInfos)
		{
			var jobs = new List<Job>();

			var jobSectionsAcc = new List<JobSection>();
			var sectionsAcc = new List<Section>();

			foreach (var jobInfo in jobInfos)
			{
				var jobMapSectionRecords = _mapSectionAdapter.GetByJobId(jobInfo.Id);

				// Create a list of 'model' jobMapSections from the list of JobMapSectionRecords.
				var jobSections = jobMapSectionRecords.Select(x => new JobSection(x.Id, x.DateCreatedUtc, x.JobType, x.JobId, x.MapSectionId, new SizeInt(x.BlockIndex.Width, x.BlockIndex.Height), x.IsInverted, x.MapSectionSubdivisionId, x.JobSubdivisionId, x.OwnerType));
				jobSectionsAcc.AddRange(jobSections);

				// Get the list of critical distinct MapSectionIds from the list of JobMapSectionRecords
				var distinctCriticalMapSectionIds = jobMapSectionRecords.Where(x => x.JobType == JobType.FullScale || x.JobType == JobType.Image).Select(x => x.MapSectionId).Distinct();
				// For each distinct MapSectionId, retreive the DateCreated and SubdivisionId
				var mapSectionCreationDatesAndSubIds = _mapSectionAdapter.GetMapSectionCreationDatesAndSubIds(distinctCriticalMapSectionIds);
				var sections = mapSectionCreationDatesAndSubIds.Select(x => new Section(x.Item1, x.Item2, x.Item3));
				sectionsAcc.AddRange(sections);

				// Get the list of non critical distinct MapSectionIds from the list of JobMapSectionRecords.
				var distinctNonCriticalMapSectionIds = jobMapSectionRecords.Where(x => !(x.JobType == JobType.FullScale || x.JobType == JobType.Image)).Select(x => x.MapSectionId).Distinct();
				// For each distinct MapSectionId, retreive the DateCreated and SubdivisionId
				mapSectionCreationDatesAndSubIds = _mapSectionAdapter.GetMapSectionCreationDatesAndSubIds(distinctCriticalMapSectionIds);
				sections = mapSectionCreationDatesAndSubIds.Select(x => new Section(x.Item1, x.Item2, x.Item3));
				sectionsAcc.AddRange(sections);

				jobs.Add(new Job(jobInfo.Id, jobInfo.DateCreatedUtc, jobInfo.SubdivisionId, distinctCriticalMapSectionIds.ToList(), distinctNonCriticalMapSectionIds.ToList()));
			}

			var storageModel = new StorageModel(jobOwnerInfo, jobs, jobSectionsAcc, sectionsAcc);

			return storageModel;
		}

		private void UpdateStats(ObservableCollection<IJobInfo> jobInfos)
		{
			_storageModel.UpdateStats();

			foreach(var jobInfo in jobInfos)
			{
				var smJob =  _storageModel.Owner.Jobs.FirstOrDefault(x => x.JobId == jobInfo.Id);
				if (smJob == null)
				{
					Debug.WriteLine($"No job with id: {jobInfo.Id} could be found in the storage model.");
				}
				else
				{
					jobInfo.NumberOfMapSections = smJob.NumberOfMapSections;
					jobInfo.NumberOfCritical = smJob.NumberOfCriticalMapSections;
					jobInfo.NumberOfNonCritical = smJob.NumberOfNonCriticalMapSections;
				}

				//jobInfo.NumberOfMapSections = _storageModel.Owner.Jobs[i].NumberOfMapSections;
				//jobInfo.NumberOfCritical = _storageModel.Owner.Jobs[i].NumberOfCriticalMapSections;
				//jobInfo.NumberOfNonCritical = _storageModel.Owner.Jobs[i].NumberOfNonCriticalMapSections;

				//jobInfo.NumberOfFullScale = _storageModel.Owner.Jobs[i].NumberOfFullScale;
				//jobInfo.NumberOfReducedScale = _storageModel.Owner.Jobs[i].NumberOfReducedScale;
				//jobInfo.NumberOfImage = _storageModel.Owner.Jobs[i].NumberOfImage;
				//jobInfo.NumberOfSizeEditorPreview = _storageModel.Owner.Jobs[i].NumberOfSizeEditorPreview;

				//jobInfo.PercentageMapSectionsShared = 1.34;
				//jobInfo.PercentageMapSectionsSharedWithSameOwner = 28.3;
			}

			Stat1 = _storageModel.Owner.NumberOfCriticalMapSections;
			Stat2 = _storageModel.Owner.NumberOfNonCriticalMapSections;
			Stat3 = _storageModel.Owner.NumberOfMapSections;
		}

		private void UpdateSharedPercentages(IJobInfo? selectedJob)
		{
			if (selectedJob != null)
			{
				var jobId = selectedJob.Id;
				var smJob = _storageModel.Owner.Jobs.FirstOrDefault(x => x.JobId == jobId);

				var (numberOfSharedCritical, numberOfSharedNonCritical) = GetNumberOfShared(smJob);

				selectedJob.PercentageMapSectionsShared = numberOfSharedCritical;
				selectedJob.PercentageMapSectionsSharedWithSameOwner = numberOfSharedNonCritical;

				Stat4 = selectedJob.PercentageMapSectionsShared;
				Stat5 = selectedJob.PercentageMapSectionsSharedWithSameOwner;
				Stat6 = Stat4 + Stat5;
			}
			else
			{
				Stat4 = 0;
				Stat5 = 0;
				Stat6 = 0;
			}
		}

		// GetNumberOfShared
		private (int numberOfSharedCritical, int numberOfSharedNonCritical) GetNumberOfShared(Job? selectedJob)
		{
			if (selectedJob == null)
			{
				return (0, 0);
			}

			var numberOfSharedCritical = _storageModel.GetNumberOfSharedSectionIds(selectedJob.DistinctCriticalSectionIds, _storageModel.CriticalSectionIdRefs);
			var numberOfSharedNonCritical = _storageModel.GetNumberOfSharedSectionIds(selectedJob.DistinctNonCriticalSectionIds, _storageModel.NonCriticalSectionIdRefs);

			return (numberOfSharedCritical, numberOfSharedNonCritical);
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

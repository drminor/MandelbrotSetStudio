using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ProjectOpenSaveViewModel : IProjectOpenSaveViewModel
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly ObservableCollection<IProjectInfo> _projectInfos;
		private ListCollectionView _projectInfosView;
		private IProjectInfo? _selectedProject;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		private string _sortByFieldName;
		private bool _isSortedDescending;

		private DateTime _lastAccessedAfterDate;
		private int _numberOfRecordsToInclude;

		private bool _allProjectInfosHaveBeenFecthed;


		#endregion

		#region Constructor

		public ProjectOpenSaveViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, ViewModelFactory viewModelFactory, string? initialName, DialogType dialogType)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			ViewModelFactory = viewModelFactory;

			DialogType = dialogType;

			_numberOfRecordsToInclude = 10;

			_sortByFieldName = "LastAccessedUtc";
			_isSortedDescending = true;

			_projectInfos =	new ObservableCollection<IProjectInfo>(GetProjectInfos(_numberOfRecordsToInclude));

			if (_numberOfRecordsToInclude != int.MaxValue)
			{
				var totalNumberOfProjectsOnFile = _projectAdapter.GetTotalNumberOfProjectsOnFile();

				_allProjectInfosHaveBeenFecthed = totalNumberOfProjectsOnFile <= _numberOfRecordsToInclude;
				_lastAccessedAfterDate = _projectInfos.Min(x => x.LastAccessedUtc);
			}
			else
			{
				_allProjectInfosHaveBeenFecthed = true;
				_lastAccessedAfterDate = _projectInfos.OrderBy(x => x.LastAccessedUtc).Take(_numberOfRecordsToInclude).Min(x => x.LastAccessedUtc);
			}

			_projectInfosView = BuildProjectInfosView(_projectInfos, _sortByFieldName, _isSortedDescending, initialName);
			_projectInfosView.Filter = IncludeInView;

			_projectInfosView.CurrentChanged += View_CurrentChanged;
		}

		private void View_CurrentChanged(object? sender, EventArgs e)
		{
			Debug.WriteLine($"The current project is now: {_projectInfosView.CurrentItem}.");
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public string? SelectedName
		{
			get => _selectedName;
			set
			{
				_selectedName = value;
				OnPropertyChanged();
			}
		}

		public bool UserIsSettingTheName
		{
			get => _userIsSettingTheName;
			set { _userIsSettingTheName = value; OnPropertyChanged(); }
		}

		public string? SelectedDescription
		{
			get => _selectedDescription;
			set
			{
				_selectedDescription = value;

				if (SelectedProject != null && SelectedProject.ProjectId != ObjectId.Empty && SelectedProject.Description != value)
				{
					_projectAdapter.UpdateProjectDescription(SelectedProject.ProjectId, SelectedDescription);
					SelectedProject.Description = value;
				}

				OnPropertyChanged();
			}
		}

		public IProjectInfo? SelectedProject
		{
			get => _selectedProject;

			set
			{
				_selectedProject = value;
				if (value != null)
				{
					if (!_userIsSettingTheName)
					{
						SelectedName = _selectedProject?.Name;
					}

					SelectedDescription = _selectedProject?.Description;
				}
				else
				{
					SelectedName = null;
					SelectedDescription = null;
				}

				OnPropertyChanged();
			}
		}

		public ViewModelFactory ViewModelFactory { get; init; }

		public string SortByFieldName
		{
			get => _sortByFieldName;
			set
			{
				if (value != _sortByFieldName)
				{
					_sortByFieldName = value;
					UpdateSortBy(ProjectInfosView, _sortByFieldName, _isSortedDescending);
					OnPropertyChanged(nameof(IProjectOpenSaveViewModel.SortByFieldName));
				}
			}
		}

		public bool IsSortedDescending
		{
			get => _isSortedDescending;

			set
			{
				if (value !=_isSortedDescending)
				{
					_isSortedDescending = value;
					UpdateSortBy(ProjectInfosView, _sortByFieldName, _isSortedDescending);
					OnPropertyChanged(nameof(IProjectOpenSaveViewModel.IsSortedDescending));
				}
			}
		}

		public int NumberOfRecordsToInclude
		{
			get => _numberOfRecordsToInclude;
			set
			{
				if (value != _numberOfRecordsToInclude)
				{
					_numberOfRecordsToInclude = value;

					if (!_allProjectInfosHaveBeenFecthed)
					{
						UpdateProjectInfos(_projectInfos);
						_allProjectInfosHaveBeenFecthed = true;
					}

					OnPropertyChanged(nameof(IProjectOpenSaveViewModel.NumberOfRecordsToInclude));
				}
			}
		}

		public ListCollectionView ProjectInfosView
		{
			get => _projectInfosView;

			set
			{
				var valueIsNew = value != _projectInfosView;
				Debug.WriteLine($"The CbsHistogramViewModel is getting a new ColorBandsView. ValueIsNew is {valueIsNew}.");

				_projectInfosView.CurrentChanged += View_CurrentChanged;

				_projectInfosView = value;

				SelectedProject = _projectInfosView.CurrentItem as IProjectInfo;

				OnPropertyChanged(nameof(IProjectOpenSaveViewModel.ProjectInfosView));
				OnPropertyChanged(nameof(IProjectOpenSaveViewModel.SelectedProject));

				_projectInfosView.CurrentChanged += View_CurrentChanged;
			}
		}

		#endregion

		#region Private Methods

		private ListCollectionView BuildProjectInfosView(ObservableCollection<IProjectInfo> projectInfos, string sortByFieldName, bool sortDescending, string? initialName)
		{
			ListCollectionView result;
			if (projectInfos == null)
			{
				var newCollection = new ObservableCollection<IProjectInfo>();
				result = (ListCollectionView)CollectionViewSource.GetDefaultView(newCollection);
			}
			else
			{
				result = (ListCollectionView)CollectionViewSource.GetDefaultView(projectInfos);

				UpdateSortBy(result, sortByFieldName, sortDescending);

				if (initialName != null)
				{
					SelectedProject = projectInfos.FirstOrDefault(x => x.Name == initialName);
					_ = result.MoveCurrentTo(SelectedProject);
				}
			}

			return result;
		}

		private void UpdateSortBy(ListCollectionView collectionView, string sortByFieldName, bool sortDescending)
		{
			var x = collectionView.CanSort;
			collectionView.SortDescriptions.Clear();

			var sortDirection = sortDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
			collectionView.SortDescriptions.Add(new SortDescription(sortByFieldName, sortDirection));
		}

		private void UpdateProjectInfos(ObservableCollection<IProjectInfo> projectInfos)
		{
			var allProjectInfos = _projectAdapter.GetAllProjectInfos();

			projectInfos.Clear();

			foreach (var pi in allProjectInfos)
			{
				projectInfos.Add(pi);
			}
		}

		private IEnumerable<IProjectInfo> GetProjectInfos(int numberOfRecordsToInclude)
		{
			IEnumerable<IProjectInfo> result;

			if (numberOfRecordsToInclude == int.MaxValue)
			{
				result = _projectAdapter.GetAllProjectInfos();
			}
			else
			{
				result = _projectAdapter.GetProjectInfosByLastAccessed(_numberOfRecordsToInclude);
			}

			return result;
		}

		private bool IncludeInView(object item)
		{
			if (_numberOfRecordsToInclude == int.MaxValue)
			{
				return true;
			}

			if (item is IProjectInfo projectInfo)
			{
				var result = projectInfo.LastAccessedUtc >= _lastAccessedAfterDate;
				return result;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Public Methods

		public bool IsNameTaken(string? name)
		{
			var result = name != null && _projectAdapter.ProjectExists(name, out _);
			return result;
		}

		public bool DeleteSelected(out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			var projectInfo = SelectedProject;

			if (projectInfo == null)
			{
				return false;
			}

			bool result;
			if (ProjectAndMapSectionHelper.DeleteProject(projectInfo.ProjectId, _projectAdapter, _mapSectionAdapter, out numberOfMapSectionsDeleted))
			{
				ProjectInfosView.Remove(projectInfo);
				result = true;
			}
			else
			{
				result = false;
			}

			return result;
		}

		public long TrimSelected(bool agressive)
		{
			var projectInfo = SelectedProject;

			if (projectInfo == null)
			{
				return -1;
			}

			var currentJobId = projectInfo.CurrentJobId;

			var ownerId = projectInfo.ProjectId;

			var allJobIds = _projectAdapter.GetAllJobIdsForProject(ownerId);

			var allNonCurrentJobIds = allJobIds.Where(x => x != currentJobId);

			DeleteMapSectionsForManyJobs(allNonCurrentJobIds, out var numberOfMapSectionsDeleted);

			if (agressive)
			{
				// In addition to deleting all the MapSections for all of the jobs for this poster, except for the current job..
				// Delete all of the ReducedScale and Preview MapSections for the current job.

				TrimMapSectionsForSelectedJob(currentJobId);
			}

			return numberOfMapSectionsDeleted;
		}

		private bool DeleteMapSectionsForManyJobs(IEnumerable<ObjectId> jobIds, out long numberOfMapSectionsDeleted)
		{
			var numberDeleted = _mapSectionAdapter.DeleteMapSectionsForManyJobs(jobIds);

			if (numberDeleted.HasValue)
			{
				numberOfMapSectionsDeleted = numberDeleted.Value;
				return true;
			}
			else
			{
				numberOfMapSectionsDeleted = -1;
				return true;
			}
		}

		private long TrimMapSectionsForSelectedJob(ObjectId selectedJobId)
		{
			var nonEssentialJobTypes = new JobType[] { JobType.ReducedScale, JobType.SizeEditorPreview };
			var result = _mapSectionAdapter.DeleteMapSectionsForJobHavingJobTypes(selectedJobId, nonEssentialJobTypes) ?? 0;

			return result;
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

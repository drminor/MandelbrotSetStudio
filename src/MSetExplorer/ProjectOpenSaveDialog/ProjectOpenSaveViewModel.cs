using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
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
	public class ProjectOpenSaveViewModel : IProjectOpenSaveViewModel, INotifyPropertyChanged
	{
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		private IProjectInfo? _selectedProject;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		private string _sortByFieldName;
		private bool _sortDescending;

		private DateTime _lastAccessedAfterDate;

		#region Constructor

		public ProjectOpenSaveViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, ViewModelFactory viewModelFactory, string? initialName, DialogType dialogType)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			ViewModelFactory = viewModelFactory;

			DialogType = dialogType;

			//ProjectInfos = new ObservableCollection<IProjectInfo>(_projectAdapter.GetAllProjectInfos());
			//SelectedProject = ProjectInfos.FirstOrDefault(x => x.Name == initialName);

			_sortByFieldName = "LastAccessed";
			_sortDescending = true;

			//_lastAccessedAfterDate = DateTime.Now.AddMonths(-1);
			_lastAccessedAfterDate = DateTime.MinValue;

			ProjectInfos = GetNewListSource(_sortByFieldName, _sortDescending, _lastAccessedAfterDate, initialName);
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public ObservableCollection<IProjectInfo> ProjectInfos { get; private set; }

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

					var currentName = SelectedProject?.Name;
					ProjectInfos = GetNewListSource(_sortByFieldName, _sortDescending, _lastAccessedAfterDate, currentName);

					OnPropertyChanged(nameof(IProjectOpenSaveViewModel.SortByFieldName));
				}
			}
		}

		public bool SortDescending
		{
			get => _sortDescending;

			set
			{
				if (value !=_sortDescending)
				{
					_sortDescending = value;

					var currentName = SelectedProject?.Name;
					ProjectInfos = GetNewListSource(_sortByFieldName, _sortDescending, _lastAccessedAfterDate, currentName);

					OnPropertyChanged(nameof(IProjectOpenSaveViewModel.SortDescending));
				}
			}
		}

		public DateTime LastAccessedAfterDate
		{
			get => _lastAccessedAfterDate;
			set
			{
				if (value != _lastAccessedAfterDate)
				{
					_lastAccessedAfterDate = value;
					OnPropertyChanged(nameof(IProjectOpenSaveViewModel.LastAccessedAfterDate));
				}
			}
		}

		#endregion

		#region Private Methods

		private ObservableCollection<IProjectInfo> GetNewListSource(string sortByFieldName, bool sortDescending, DateTime lastAccessedAfterDate, string? initialName)
		{
			var result = OrderTheList(_projectAdapter.GetAllProjectInfos(lastAccessedAfterDate), sortByFieldName, sortDescending);

			if (initialName != null)
			{
				SelectedProject = result.FirstOrDefault(x => x.Name == initialName);

				var view = CollectionViewSource.GetDefaultView(result);
				_ = view.MoveCurrentTo(SelectedProject);
			}

			return result;
		}

		private ObservableCollection<IProjectInfo> OrderTheList(IEnumerable<IProjectInfo> theList, string sortByFieldName, bool sortDescending)
		{
			ObservableCollection<IProjectInfo> result;

			switch (sortByFieldName)
			{
				case "LastAccessed":
					{
						result = sortDescending 
							? new ObservableCollection<IProjectInfo>(theList.OrderByDescending(x => x.LastAccessedUtc)) 
							: new ObservableCollection<IProjectInfo>(theList.OrderBy(x => x.LastAccessedUtc));
						break;
					}

				case "DateCreated":
					{
						result = sortDescending 
							? new ObservableCollection<IProjectInfo>(theList.OrderByDescending(x => x.DateCreatedUtc)) 
							: new ObservableCollection<IProjectInfo>(theList.OrderBy(x => x.DateCreatedUtc));
						break;
					}

				case "Name":
					{
						result = sortDescending 
							? new ObservableCollection<IProjectInfo>(theList.OrderByDescending(x => x.Name)) 
							: new ObservableCollection<IProjectInfo>(theList.OrderBy(x => x.Name));
						break;
					}

				default:
					result = new ObservableCollection<IProjectInfo>();
					break;
			}

			return result;
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
				_ = ProjectInfos.Remove(projectInfo);
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

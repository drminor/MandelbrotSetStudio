using MongoDB.Bson;
using MSetRepo;
using MSetRepo.Storage;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media.Animation;

namespace MSetExplorer
{
	public class PosterOpenSaveViewModel : IPosterOpenSaveViewModel, INotifyPropertyChanged
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;
		

		//private readonly Func<Job, SizeDbl, bool, long>? _deleteNonEssentialMapSectionsFunction;

		private IPosterInfo? _selectedPoster;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		#endregion


		#region Constructor

		public PosterOpenSaveViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, ViewModelFactory viewModelFactory, DeleteNonEssentialMapSectionsDelegate? deleteNonEssentialMapSectionsFunction, string? initialName, DialogType dialogType)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			ViewModelFactory = viewModelFactory;

			DeleteNonEssentialMapSectionsFunction = deleteNonEssentialMapSectionsFunction;

			DialogType = dialogType;

			var posters = _projectAdapter.GetAllPosterInfos();
			PosterInfos = new ObservableCollection<IPosterInfo>(posters);
			SelectedPoster = PosterInfos.FirstOrDefault(x => x.Name == initialName);

			if (SelectedPoster == null)
			{
				SelectedName = initialName;
				_userIsSettingTheName = true;
			}

			var view = CollectionViewSource.GetDefaultView(PosterInfos);
			_ = view.MoveCurrentTo(SelectedPoster);
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public ObservableCollection<IPosterInfo> PosterInfos { get; init; }

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
			set
			{
				_userIsSettingTheName = value;
				OnPropertyChanged();
			}
		}


		public string? SelectedDescription
		{
			get => _selectedDescription;
			set
			{
				_selectedDescription = value;

				if (SelectedPoster != null && SelectedPoster.PosterId != ObjectId.Empty && SelectedPoster.Description != value)
				{
					_projectAdapter.UpdateProjectDescription(SelectedPoster.PosterId, SelectedDescription);
					SelectedPoster.Description = value;
				}

				OnPropertyChanged();
			}
		}

		public IPosterInfo? SelectedPoster
		{
			get => _selectedPoster;

			set
			{
				_selectedPoster = value;
				if (value != null)
				{
					if (!_userIsSettingTheName)
					{
						SelectedName = _selectedPoster?.Name;
					}

					SelectedDescription = _selectedPoster?.Description;
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

		public DeleteNonEssentialMapSectionsDelegate? DeleteNonEssentialMapSectionsFunction { get; }

		#endregion

		#region Public Methods

		public bool IsNameTaken(string? name)
		{
			var result = name != null && _projectAdapter.PosterExists(name, out _);
			return result;
		}

		public bool DeleteSelected(out long numberOfMapSectionsDeleted)
		{
			numberOfMapSectionsDeleted = 0;

			var posterInfo = SelectedPoster;

			if (posterInfo == null)
			{
				return false;
			}

			bool result;
			if (ProjectAndMapSectionHelper.DeletePoster(posterInfo.PosterId, _projectAdapter, _mapSectionAdapter, out numberOfMapSectionsDeleted))
			{
				_ = PosterInfos.Remove(posterInfo);
				result = true;
			}
			else
			{
				result = false;
			}

			return result;
		}

		// TODO: Move these methods to the ProjectAndMapSectionHelper (static) class
		public long TrimSelected(bool agressive)
		{
			var posterInfo = SelectedPoster;

			if (posterInfo == null)
			{
				return -1;
			}

			var currentJobId = posterInfo.CurrentJobId;

			var ownerId = posterInfo.PosterId;

			var allJobIds = _projectAdapter.GetAllJobIdsForPoster(ownerId);

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

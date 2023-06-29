using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MSetExplorer
{
	public class PosterOpenSaveViewModel : IPosterOpenSaveViewModel, INotifyPropertyChanged
	{
		#region Private Fieldas

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly Func<Job, SizeDbl, long>? _deleteNonEssentialMapSectionsFunction;

		private IPosterInfo? _selectedPoster;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		#endregion


		#region Constructor

		public PosterOpenSaveViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, Func<Job, SizeDbl, long>? deleteNonEssentialMapSectionsFunction, string? initialName, DialogType dialogType)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;

			_deleteNonEssentialMapSectionsFunction = deleteNonEssentialMapSectionsFunction;

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

		public long TrimSelected()
		{
			if (SelectedPoster == null || _deleteNonEssentialMapSectionsFunction == null)
			{
				return -1;
			}

			var jobId = SelectedPoster.CurrentJobId;
			var job = _projectAdapter.GetJob(jobId);
			var posterSize = SelectedPoster.Size;

			var result = _deleteNonEssentialMapSectionsFunction(job, posterSize);

			return result;
		}

		public long TrimHeavySelected()
		{
			return -1;
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

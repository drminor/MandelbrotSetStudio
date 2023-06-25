using ImageBuilder;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace MSetExplorer
{
	public class PosterOpenSaveViewModel : IPosterOpenSaveViewModel, INotifyPropertyChanged
	{
		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private bool _useEscapeVelocities;
		private IPosterInfo? _selectedPoster;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		#region Constructor

		public PosterOpenSaveViewModel(IMapLoaderManager mapLoaderManager, IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, string? initialName, bool useEscapeVelocitites, DialogType dialogType)
		{
			_mapLoaderManager = mapLoaderManager;
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;

			_useEscapeVelocities = useEscapeVelocitites;
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

		private Progress<double> _previewImageDataBuilderProgress = new Progress<double>();

		private void StatusCallBack(double value)
		{
			((IProgress<double>)_previewImageDataBuilderProgress).Report(value);
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

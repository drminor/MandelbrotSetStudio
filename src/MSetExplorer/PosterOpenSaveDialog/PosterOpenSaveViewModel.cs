using MongoDB.Bson;
using MSetRepo;
using MSS.Types;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

namespace MSetExplorer
{
	public class PosterOpenSaveViewModel : IPosterOpenSaveViewModel, INotifyPropertyChanged
	{
		private readonly ProjectAdapter _projectAdapter;
		private Poster? _selectedPoster;

		private string? _selectedName;
		private string? _selectedDescription;

		private bool _userIsSettingTheName;

		#region Constructor

		public PosterOpenSaveViewModel(ProjectAdapter projectAdapter, string? initialName, DialogType dialogType)
		{
			_projectAdapter = projectAdapter;
			DialogType = dialogType;

			Posters = new ObservableCollection<Poster>(_projectAdapter.GetAllPosters());
			SelectedPoster = Posters.FirstOrDefault(x => x.Name == initialName);

			if (SelectedPoster == null)
			{
				SelectedName = initialName;
				_userIsSettingTheName = true;
			}

			var view = CollectionViewSource.GetDefaultView(Posters);
			_ = view.MoveCurrentTo(SelectedPoster);
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public ObservableCollection<Poster> Posters { get; init; }

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

				if (SelectedPoster != null && SelectedPoster.Id != ObjectId.Empty && SelectedPoster.Description != value)
				{
					_projectAdapter.UpdateProjectDescription(SelectedPoster.Id, SelectedDescription);
					SelectedPoster.Description = value;
				}

				OnPropertyChanged();
			}
		}

		public Poster? SelectedPoster
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
			var result = name != null && _projectAdapter.PosterExists(name);
			return result;
		}

		public void DeleteSelected()
		{
			var poster = SelectedPoster;

			if (poster != null)
			{
				_projectAdapter.DeleteProject(poster.Id);
				_ = Posters.Remove(poster);
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

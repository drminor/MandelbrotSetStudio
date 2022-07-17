using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types.MSet;
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

		#region Constructor

		public ProjectOpenSaveViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, string? initialName, DialogType dialogType)
		{
			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			DialogType = dialogType;

			ProjectInfos = new ObservableCollection<IProjectInfo>(_projectAdapter.GetAllProjectInfos());
			SelectedProject = ProjectInfos.FirstOrDefault(x => x.Name == initialName);

			var view = CollectionViewSource.GetDefaultView(ProjectInfos);
			_ = view.MoveCurrentTo(SelectedProject);
		}

		#endregion

		#region Public Properties

		public DialogType DialogType { get; }

		public ObservableCollection<IProjectInfo> ProjectInfos { get; init; }

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

		#endregion

		#region Public Methods

		public bool IsNameTaken(string? name)
		{
			var result = name != null && _projectAdapter.ProjectExists(name);
			return result;
		}

		public void DeleteSelected()
		{
			var projectInfo = SelectedProject;

			if (projectInfo != null)
			{
				ProjectAndMapSectionHelper.DeleteProject(projectInfo.Name, _projectAdapter, _mapSectionAdapter);
				_ = ProjectInfos.Remove(projectInfo);
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

using MongoDB.Bson;
using MSetRepo;
using MSS.Types.MSet;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Data;

namespace MSetExplorer
{
	public class ProjectOpenSaveViewModel : ViewModelBase //, IProjectOpenSaveViewModel
	{
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		private readonly ProjectAdapter _projectAdapter;
		private IProjectInfo _selectedProject;

		private string _selectedName;
		private string _selectedDescription;

		private bool _userIsSettingTheName;

		#region Constructor

		public ProjectOpenSaveViewModel(string selectedName, bool isOpenDialog)
		{
			IsOpenDialog = isOpenDialog;
			_projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING, CreateProjectInfo);
			ProjectInfos = new ObservableCollection<IProjectInfo>(_projectAdapter.GetAllProjectInfos());

			var selectedProject = ProjectInfos.FirstOrDefault(x => x.Name == selectedName);
			var view = CollectionViewSource.GetDefaultView(ProjectInfos);
			_ = view.MoveCurrentTo(selectedProject);
		}

		#endregion

		#region Public Properties

		public bool IsOpenDialog { get; }
		public ObservableCollection<IProjectInfo> ProjectInfos { get; init; }

		public string SelectedName
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


		public string SelectedDescription
		{
			get => _selectedDescription;
			set
			{
				_selectedDescription = value;

				if (SelectedProject.Project.Id != ObjectId.Empty)
				{
					Debug.WriteLine($"Will update project with id: {SelectedProject.Project.Id} with the new description: {SelectedDescription}.");
					//_projectAdapter.
				}

				OnPropertyChanged();
			}
		}

		public IProjectInfo SelectedProject
		{
			get => _selectedProject;

			set
			{
				_selectedProject = value;
				if (value != null)
				{
					if (!_userIsSettingTheName)
					{
						SelectedName = _selectedProject.Name;
					}

					SelectedDescription = _selectedProject.Description;
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

		#region CreateProjectInfo Delegate

		private IProjectInfo CreateProjectInfo(Project project, DateTime lastSaved, int numberOfJobs, int minMapCoordsExponent, int minSamplePointDeltaExponent)
		{
			return new ProjectInfo(project, lastSaved, numberOfJobs, minMapCoordsExponent, minSamplePointDeltaExponent);
		}

		#endregion
	}
}

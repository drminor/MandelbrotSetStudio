using MSetRepo;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	public class ProjectOpenSaveViewModel : IProjectOpenSaveViewModel
	{
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		public ProjectOpenSaveViewModel(string selectedName)
		{
			SelectedName = selectedName;

			var projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING, CreateProjectInfo);

			var mondayWork = projectAdapter.GetAllProjectInfos();
		}

		public string SelectedName { get; }


		private IProjectInfo CreateProjectInfo(Project project, DateTime lastSaved, int numberOfJobs, int zoomLevel)
		{
			return new ProjectInfo(project, lastSaved, numberOfJobs, zoomLevel);
		}

	}
}

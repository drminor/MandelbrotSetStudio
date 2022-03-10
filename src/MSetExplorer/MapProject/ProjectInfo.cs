using MongoDB.Bson;
using MSetRepo;
using MSS.Types.MSet;
using System;

namespace MSetExplorer
{
	internal class ProjectInfo : ViewModelBase, IProjectInfo
	{
		private DateTime _lastSaved;
		private int _numberOfJobs;
		private int _zoomLevel;

		public ProjectInfo(Project project, DateTime lastSaved, int numberOfJobs, int zoomLevel)
		{
			Project = project;
			LastSaved = lastSaved;
			NumberOfJobs = numberOfJobs;
			ZoomLevel = zoomLevel;
		}

		public Project Project { get; init; }

		public ObjectId Id => Project.Id;
		public string Name => Project.Name;
		public DateTime DateCreated => Project.DateCreated;

		public DateTime LastSaved
		{
			get => _lastSaved;
			set { _lastSaved = value; OnPropertyChanged(); }
		}

		public int NumberOfJobs
		{
			get => _numberOfJobs;
			set { _numberOfJobs = value; OnPropertyChanged(); }
		}

		public int ZoomLevel
		{
			get => _zoomLevel;
			set { _zoomLevel = value; OnPropertyChanged(); }
		}



	}
}

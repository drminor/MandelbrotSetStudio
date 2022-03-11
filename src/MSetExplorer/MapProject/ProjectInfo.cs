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
		private int _minMapCoordsExponent;
		private int _minSamplePointDeltaExponent;

		private string _name;
		private string _description;

		public ProjectInfo(Project project, DateTime lastSaved, int numberOfJobs, int minMapCoordsExponent, int minSamplePointDeltaExponent)
		{
			Project = project;
			LastSaved = lastSaved;
			NumberOfJobs = numberOfJobs;
			MinMapCoordsExponent = minMapCoordsExponent;
			MinSamplePointDeltaExponent = minSamplePointDeltaExponent;

			_name = project.Name;
			_description = project.Description;
		}

		public Project Project { get; init; }

		public ObjectId Id => Project.Id;

		public DateTime DateCreated => Project.DateCreated;

		public string Name
		{
			get => _name;
			set { _name = value; OnPropertyChanged(); }
		}

		public string Description
		{
			get => _description;
			set { _description = value; OnPropertyChanged(); }
		}

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

		public int MinMapCoordsExponent
		{
			get => _minMapCoordsExponent * -1;
			set { _minMapCoordsExponent = value; OnPropertyChanged(); }
		}

		public int MinSamplePointDeltaExponent
		{
			get => _minSamplePointDeltaExponent;
			set { _minSamplePointDeltaExponent = value; OnPropertyChanged(); }
		}



	}
}

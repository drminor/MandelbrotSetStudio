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

		public ProjectInfo(Project project, DateTime lastSaved, int numberOfJobs, int minMapCoordsExponent, int minSamplePointDeltaExponent)
		{
			Project = project;
			LastSaved = lastSaved;
			NumberOfJobs = numberOfJobs;
			MinMapCoordsExponent = minMapCoordsExponent;
			MinSamplePointDeltaExponent = minSamplePointDeltaExponent;
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

		public int MinMapCoordsExponent
		{
			get => _minMapCoordsExponent;
			set { _minMapCoordsExponent = value; OnPropertyChanged(); }
		}

		public int MinSamplePointDeltaExponent
		{
			get => _minSamplePointDeltaExponent;
			set { _minSamplePointDeltaExponent = value; OnPropertyChanged(); }
		}



	}
}

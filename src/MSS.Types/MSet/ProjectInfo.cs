using MongoDB.Bson;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MSS.Types.MSet
{
	public class ProjectInfo : IProjectInfo
	{
		private string _name;
		private string? _description;
		private DateTime _lastSavedUtc;

		#region Constructor

		public ProjectInfo(ObjectId projectId, DateTime dateCreated, string name, string? description, DateTime lastSavedUtc, int numberOfJobs, int minMapCoordsExponent, int minSamplePointDeltaExponent)
		{
			ProjectId = projectId;
			DateCreated = dateCreated;

			_lastSavedUtc = lastSavedUtc;
			NumberOfJobs = numberOfJobs;
			MinMapCoordsExponent = minMapCoordsExponent;
			MinSamplePointDeltaExponent = minSamplePointDeltaExponent;

			_name = name;
			_description = description;
		}

		#endregion

		#region Public Properties

		public ObjectId ProjectId { get; init; }

		public DateTime DateCreated { get; init; }
		public int NumberOfJobs { get; init; }
		public int MinMapCoordsExponent { get; init; }
		public int MinSamplePointDeltaExponent { get; init; }

		public string Name
		{
			get => _name;
			set { _name = value; OnPropertyChanged(); }
		}

		public string? Description
		{
			get => _description;
			set { _description = value; OnPropertyChanged(); }
		}

		public DateTime LastSavedUtc
		{
			get => _lastSavedUtc;
			set { _lastSavedUtc = value; OnPropertyChanged(); }
		}

		public DateTime LastSaved => LastSavedUtc.ToLocalTime();

		//public int NumberOfJobs
		//{
		//	get => _numberOfJobs;
		//	set { _numberOfJobs = value; OnPropertyChanged(); }
		//}

		//public int MinMapCoordsExponent
		//{
		//	get => _minMapCoordsExponent * -1;
		//	set { _minMapCoordsExponent = value; OnPropertyChanged(); }
		//}

		//public int MinSamplePointDeltaExponent
		//{
		//	get => _minSamplePointDeltaExponent;
		//	set { _minSamplePointDeltaExponent = value; OnPropertyChanged(); }
		//}

		#endregion

		#region NotifyPropertyChanged Support

		public event PropertyChangedEventHandler? PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion

	}
}

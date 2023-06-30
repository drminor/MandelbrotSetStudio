using MongoDB.Bson;
using System;
using System.ComponentModel;

namespace MSS.Common
{
	public interface IProjectInfo : INotifyPropertyChanged
	{
		//Project Project { get; }

		ObjectId ProjectId { get; }
		string Name { get; set; }
		string? Description { get; set; }
		int Bytes { get; set; }
		int NumberOfJobs { get; }
		int MinSamplePointDeltaExponent { get; }

		DateTime DateCreated { get; }
		DateTime LastSavedUtc { get; set; }
		DateTime LastSaved { get; }
	}
}
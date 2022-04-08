using System;
using System.ComponentModel;

namespace MSS.Types.MSet
{
	public interface IProjectInfo : INotifyPropertyChanged
	{
		Project Project { get; }
		DateTime DateCreated { get; }
		string Name { get; set; }
		string? Description { get; set; }
		int NumberOfJobs { get; }
		int MinSamplePointDeltaExponent { get; }

		DateTime LastSavedUtc { get; set; }
		DateTime LastSaved { get; }
	}
}
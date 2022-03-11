using MSS.Types.MSet;
using System;
using System.ComponentModel;

namespace MSetRepo
{
	public interface IProjectInfo
	{
		public event PropertyChangedEventHandler PropertyChanged;

		Project Project { get; }
		DateTime DateCreated { get; }
		string Name { get; set; }
		string Description { get; set; }
		int NumberOfJobs { get; }
		int MinSamplePointDeltaExponent { get; }
		DateTime LastSaved { get; }
	}
}
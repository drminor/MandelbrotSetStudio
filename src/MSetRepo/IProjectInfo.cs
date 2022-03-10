using MSS.Types.MSet;
using System;

namespace MSetRepo
{
	public interface IProjectInfo
	{
		DateTime DateCreated { get; }
		DateTime LastSaved { get; set; }
		string Name { get; }
		int NumberOfJobs { get; set; }
		Project Project { get; init; }
		int ZoomLevel { get; set; }
	}
}
using MSS.Types.MSet;
using System;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class GenMapRequestInfo
	{
		public Job Job { get; set; }

		public int JobNumber { get; private set; }
		public MapLoader MapLoader { get; private set; }

		public GenMapRequestInfo(Job job)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			JobNumber = -1;
			MapLoader = null;
		}

		public GenMapRequestInfo(Job job, int jobNumber, MapLoader mapLoader)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			JobNumber = jobNumber;
			MapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
		}

		public void Renew(int jobNumber, MapLoader mapLoader)
		{
			JobNumber = jobNumber;
			MapLoader = mapLoader;
		}

		public void LoadingComplete(Task _)
		{
			MapLoader = null;
		}
	}
}

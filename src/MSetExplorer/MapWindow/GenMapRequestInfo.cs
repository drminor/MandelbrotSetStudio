using MSS.Types;
using MSS.Types.MSet;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class GenMapRequestInfo
	{
		public Job Job { get; private set; }

		public int JobNumber { get; private set; }
		public MapLoader MapLoader { get; private set; }

		public GenMapRequestInfo(Job job, MapLoader mapLoader, int jobNumber)
		{
			Job = job;
			JobNumber = jobNumber;
			MapLoader = mapLoader;
		}

		public void Renew(int jobNumber, MapLoader mapLoader)
		{
			JobNumber = jobNumber;
			MapLoader = mapLoader;
		}

		public void UpdateJob(Job job)
		{
			Job = job;
		}

		public void LoadingComplete(Task _)
		{
			MapLoader = null;
		}
	}
}

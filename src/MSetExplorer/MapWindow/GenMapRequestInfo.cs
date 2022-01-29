using MSS.Types.MSet;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class GenMapRequestInfo
	{
		public Job Job { get; init; }
		public int GenMapRequestId { get; init; }
		public MapLoader MapLoader { get; private set; }

		public GenMapRequestInfo(Job job, int genMapRequestId, MapLoader mapLoader)
		{
			Job = job;
			GenMapRequestId = genMapRequestId;
			MapLoader = mapLoader;
		}

		public void LoadingComplete(Task _)
		{
			MapLoader = null;
		}
	}
}

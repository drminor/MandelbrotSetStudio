using MSS.Types;
using MSS.Types.MSet;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class GenMapRequestInfo
	{
		public Job Job { get; init; }
		public SizeInt NewArea { get; set; }
		public int GenMapRequestId { get; init; }
		public MapLoader MapLoader { get; private set; }

		public GenMapRequestInfo(Job job, SizeInt newArea, int genMapRequestId, MapLoader mapLoader)
		{
			Job = job;
			NewArea = newArea;
			GenMapRequestId = genMapRequestId;
			MapLoader = mapLoader;
		}

		public void LoadingComplete(Task _)
		{
			MapLoader = null;
		}
	}
}

using MSS.Types;
using MSS.Types.MSet;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class GenMapRequestInfo
	{
		public Job Job { get; private set; }
		public int JobNumber { get; private set; }

		public TransformType TransformType { get; init; }
		public SizeInt NewArea { get; set; }
		public MapLoader MapLoader { get; private set; }

		public GenMapRequestInfo(Job job, int jobNumber, TransformType transformType, SizeInt newArea, MapLoader mapLoader)
		{
			Job = job;
			JobNumber = jobNumber;
			TransformType = transformType;
			NewArea = newArea;
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

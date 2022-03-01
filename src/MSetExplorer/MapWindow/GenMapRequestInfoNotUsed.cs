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

		public GenMapRequestInfo(Job job, MapLoader mapLoader)
		{
			Job = job ?? throw new ArgumentNullException(nameof(job));
			MapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
			JobNumber = mapLoader.JobNumber;
		}

		public void Renew(MapLoader mapLoader)
		{
			JobNumber = mapLoader.JobNumber;
			MapLoader = mapLoader;
		}

		public void StartLoading()
		{
			_ = MapLoader.Start().ContinueWith(LoadingComplete);
		}

		public void LoadingComplete(Task _)
		{
			MapLoader = null;
		}
	}
}

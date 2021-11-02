using MSS.Types;

namespace MClient
{
	public class JobFactory
	{
		public static IJob CreateJob(SMapWorkRequest sMapWorkRequest)
		{
			IJob result = new JobForMq(sMapWorkRequest);

			return result;
		}
	}
}

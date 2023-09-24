using System.Linq;
using System.ServiceProcess;

namespace MSetExplorer
{
	internal class ServiceHelper
	{
		public static ServiceControllerStatus? CheckService(string ServiceName)
		{
			var services = ServiceController.GetServices();
			var service = services.FirstOrDefault(x => x.ServiceName == ServiceName);

			ServiceControllerStatus? result;

			if (service == null)
			{
				result = null;
			}
			else
			{
				//if (service.Status == ServiceControllerStatus.Running)
				//{
				//	result = RepoStatus.Running;
				//}
				//else if (service.Status == ServiceControllerStatus.Stopped)
				//{
				//	result = RepoStatus.Stopped;
				//}
				//else
				//{
				//	result = RepoStatus.Transitioning;
				//}
				result = service.Status;
			}

			return result;
		}
	}

	public enum RepoStatus
	{
		Connected,
		Running,
		Stopped,
		Transitioning,
		NotFound
	}
}

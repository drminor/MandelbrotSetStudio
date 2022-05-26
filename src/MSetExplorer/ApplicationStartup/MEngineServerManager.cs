using System.Collections.Generic;
using System.Diagnostics;

namespace MSetExplorer
{
	public class MEngineServerManager
	{
		private readonly string _serverExePath;
		private readonly string[] _mEngineEndPointAddresses;

		private readonly IList<Process> _serverProcesses;

		public MEngineServerManager(string serverExePath, string[] mEngineEndPointAddresses)
		{
			_serverExePath = serverExePath;
			_mEngineEndPointAddresses = mEngineEndPointAddresses;
			_serverProcesses = new List<Process>();
		}

		public void Start()
		{
			StartServer(_mEngineEndPointAddresses);
		}

		private void StartServer(string[] urls)
		{
			var exists = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(_serverExePath)).Length > 0;
			if (!exists)
			{
				foreach (var ep in urls)
				{
					if (ep.ToLower().Contains("localhost"))
					{
						var proc = Process.Start(_serverExePath, " --urls " + ep);
						_serverProcesses.Add(proc);
					}
				}
			}
		}

		public void Stop()
		{
			foreach (var proc in _serverProcesses)
			{
				proc.Kill();
			}

			_serverProcesses.Clear();
		}
	}
}

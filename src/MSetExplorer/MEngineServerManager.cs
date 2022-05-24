using System.Collections.Generic;
using System.Diagnostics;

namespace MSetExplorer
{
	public class MEngineServerManager
	{
		private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net5.0\MEngineService.exe";

		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5004", "https://localhost:5001" };
		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "http://192.168.2.104:5000", "https://localhost:5001" };
		private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5001" };

		private readonly IList<Process> _serverProcesses;

		public MEngineServerManager()
		{
			_serverProcesses = new List<Process>();
		}

		public void Start()
		{
			StartServer(M_ENGINE_END_POINT_ADDRESSES);
		}

		private void StartServer(string[] urls)
		{
			var exists = Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(SERVER_EXE_PATH)).Length > 0;
			if (!exists)
			{
				foreach (var ep in urls)
				{
					if (ep.ToLower().Contains("localhost"))
					{
						var proc = Process.Start(SERVER_EXE_PATH, " --urls " + ep);
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

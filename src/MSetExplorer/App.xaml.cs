using System;
using System.Diagnostics;
using System.Windows;

namespace MSetExplorer
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		//private const string SERVER_EXE_PATH = @"C:\Users\david\source\repos\MandelbrotSetStudio\src_FGEN\MEngineService\bin\x64\Debug\net5.0\MEngineService.exe";
		//private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		////private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5004", "https://localhost:5001" };
		////private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "http://192.168.2.104:5000", "https://localhost:5001" };
		//private static readonly string[] M_ENGINE_END_POINT_ADDRESSES = new string[] { "https://localhost:5001" };

		private readonly MEngineServerManager _mEngineServerManager;

		private AppNavWindow? _appNavWindow;

		public App()
		{
			Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
			_mEngineServerManager = new MEngineServerManager();
		}

		void App_Startup(object sender, StartupEventArgs e)
		{
			Debug.WriteLine("H");
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			_mEngineServerManager.Start();

			_appNavWindow = new AppNavWindow
			{
				DataContext = new AppNavViewModel()
			};

			_appNavWindow.WindowState = WindowState.Minimized;

			_appNavWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			_mEngineServerManager.Stop();
		}

		//private void DoSchemaUpdates()
		//{
		//	//_projectAdapter.AddColorBandSetIdToAllJobs();
		//	//_projectAdapter.AddIsPreferredChildToAllJobs();

		//	//var report = _projectAdapter.FixAllJobRels();
		//	//Debug.WriteLine(report);

		//	//var report1 = _projectAdapter.OpenAllJobs();
		//	//Debug.WriteLine($"Could not open these projects:\n {string.Join("; ", report1)}");

		//	//Debug.WriteLine("About to call DeleteUnusedColorBandSets.");
		//	//var report = _projectAdapter.DeleteUnusedColorBandSets();
		//	//Debug.WriteLine(report);

		//	//var mapSectionAdapter = MSetRepoHelper.GetMapSectionAdapter(MONGO_DB_CONN_STRING);
		//	//mapSectionAdapter.AddCreatedDateToAllMapSections();

		//	//var res = MessageBox.Show("FixAll complete, stop application?", "done", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes);
		//	//if (res == MessageBoxResult.Yes)
		//	//{
		//	//	Current.Shutdown();
		//	//	return;
		//	//}
		//}

	}
}

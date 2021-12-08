using MapSectionProviderLib;
using MEngineClient;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase
	{
		private SizeInt _blockSize;
		private readonly ProjectAdapter _projectAdapter;
		private readonly IMapSectionRepo _mapSectionRepo;
		private readonly IMEngineClient _mEngineClient;

		private Job _job;
		//private MapLoader _mapLoader;
		private MapLoader2 _mapLoader;
		private IProgress<MapSection> _progress;
		private Task _mapLoaderTask;

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, IMapSectionRepo mapSectionRepo, IMEngineClient mEngineClient)
		{
			_blockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRepo = mapSectionRepo;
			_mEngineClient = mEngineClient;
		}

		public void LoadMap(MSetInfo mSetInfo, IProgress<MapSection> progress)
		{
			if (!(_job is null))
			{
				Debug.WriteLine("Warning, not saving current job.");

				if (!(_mapLoader is null))
				{
					throw new InvalidOperationException("Cannot call GenerateMapSections until the current task is complete.");
				}
			}

			_progress = progress;

			_job = BuildJob(mSetInfo);

			//_mapLoader = new MapLoader(_mapSectionProvider);
			//_mapLoaderTask = _mapLoader.LoadMap(_job, HandleMapSection);
			//_mapLoaderTask.ContinueWith(OnTaskComplete);

			_mapLoader = new MapLoader2(_mEngineClient, _mapSectionRepo);
			_mapLoaderTask = Task.Run(() => _mapLoader.LoadMap(_job, HandleMapSection));
			_mapLoaderTask.ContinueWith(OnTaskComplete);
		}

		private Job BuildJob(MSetInfo mSetInfo)
		{
			var project = new Project(ObjectId.GenerateNewId(), "un-named");
			Subdivision temp = MSetInfoHelper.GetSubdivision(mSetInfo, _blockSize);
			Subdivision subdivision = _projectAdapter.GetOrCreateSubdivision(temp);

			var canvasOffset = new PointDbl(512, 384);

			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, "initial job", mSetInfo, canvasOffset);
			return job;
		}

		private object hmsLock = new object();

		private void HandleMapSection(MapSection mapSection)
		{
			lock (hmsLock)
			{
				_progress.Report(mapSection);
			}
		}

		private void OnTaskComplete(Task t)
		{
			_mapLoader.Stop();
			_mapLoader = null;
			_mapLoaderTask = null;
		}

	}
}

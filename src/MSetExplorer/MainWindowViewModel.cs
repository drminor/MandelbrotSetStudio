using MapSectionProviderLib;
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
		private readonly MapSectionRequestQueue _mapSectionRequestQueue;

		private Job _job;
		private MapLoader _mapLoader;
		private IProgress<MapSection> _progress;
		private Task _mapLoaderTask;

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionRequestQueue mapSectionRequestQueue)
		{
			_blockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionRequestQueue = mapSectionRequestQueue;

		}

		public void LoadMap(SizeInt canvasControlSize, MSetInfo mSetInfo, IProgress<MapSection> progress)
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


			_mapLoader = new MapLoader(_mapSectionRequestQueue);
			_mapLoaderTask = Task.Run(() => _mapLoader.LoadMap(_job, HandleMapSection));
			_ = _mapLoaderTask.ContinueWith(OnTaskComplete);
		}

		private Job BuildJob(MSetInfo mSetInfo)
		{
			var project = new Project(ObjectId.GenerateNewId(), "un-named");
			var subdivision = GetSubdivision(mSetInfo, _blockSize, _projectAdapter);

			var canvasOffset = new PointDbl(512, 384);

			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, "initial job", mSetInfo, canvasOffset);
			return job;
		}

		private Subdivision GetSubdivision(MSetInfo mSetInfo, SizeInt blockSize, ProjectAdapter projectAdapter)
		{
			var temp = JobHelper.GetSubdivision(mSetInfo, blockSize);
			var result = projectAdapter.GetOrCreateSubdivision(temp);

			return result;
		}

		private readonly object hmsLock = new();

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

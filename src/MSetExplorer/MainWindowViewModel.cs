using MapSectionProviderLib;
using MongoDB.Bson;
using MSetRepo;
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
		private readonly MapSectionProvider _mapSectionProvider;

		private Job _job;
		private DPoint _canvasPosition;
		private MapLoader _mapLoader;
		private IProgress<MapSection> _progress;
		private Task _mapLoaderTask;

		//private JobInfo _currentJobInfo;

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionProvider mapSectionProvider)
		{
			_blockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionProvider = mapSectionProvider;
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

			_job = BuildJob(mSetInfo, out DPoint canvasPosition);
			_canvasPosition = canvasPosition;

			_mapLoader = new MapLoader(_mapSectionProvider);
			_mapLoaderTask = _mapLoader.LoadMap(_job, HandleMapSection);
			_mapLoaderTask.ContinueWith(OnTaskComplete);
		}

		private Job BuildJob(MSetInfo mSetInfo, out DPoint canvasPosition)
		{
			var project = new Project(ObjectId.GenerateNewId(), "un-named");
			Subdivision temp = MSetInfoHelper.GetSubdivision(mSetInfo, _blockSize);
			Subdivision subdivision = _projectAdapter.GetOrCreateSubdivision(temp);

			canvasPosition = new DPoint(384, 384);

			var job = new Job(ObjectId.GenerateNewId(), parentJob: null, project, subdivision, "initial job", mSetInfo);
			return job;
		}

		private void HandleMapSection(MapSection mapSection)
		{
			_progress.Report(mapSection);
		}

		private void OnTaskComplete(Task t)
		{
			_mapLoader = null;
			_mapLoaderTask = null;
		}

	}
}

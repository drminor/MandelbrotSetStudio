using MapSectionProviderLib;
using MEngineClient;
using MEngineDataContracts;
using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase
	{
		private readonly SizeInt _blockSize;
		private readonly MapSectionProvider _mapSectionProvider;
		private readonly ProjectAdapter _projectAdapter;

		private ColorMap _colorMap;
		private IProgress<MapSection> _progress;
		private JobTask _jobTask;

		public MainWindowViewModel(SizeInt blockSize, ProjectAdapter projectAdapter, MapSectionProvider mapSectionProvider)
		{
			_blockSize = blockSize;
			_projectAdapter = projectAdapter;
			_mapSectionProvider = mapSectionProvider;

			Project = null;
			Subdivision = null;
			_jobTask = null;
		}

		public Project Project { get; private set; }
		public Subdivision Subdivision { get; private set; }

		public Job Job => _jobTask?.Job;
		public MSetInfo MSetInfo => _jobTask?.Job.MSetInfo;
		public bool IsTaskComplete => _jobTask?.Task?.IsCompleted ?? true;

		public void CreateJob(MSetInfo mSetInfo, IProgress<MapSection> progress)
		{
			if (!(Job is null))
			{
				Debug.WriteLine("Warning, not saving current job.");

				if (!IsTaskComplete)
				{
					throw new InvalidOperationException("Cannot call GenerateMapSections until the current task is complete.");
				}
			}

			Project = new Project(ObjectId.GenerateNewId(), "un-named");
			Subdivision temp = MSetInfoHelper.GetSubdivision(mSetInfo, _blockSize);
			Subdivision = _projectAdapter.GetOrCreateSubdivision(temp);

			var job = new Job(ObjectId.GenerateNewId(), parentJobId: null, projectId: Project.Id, Subdivision.Id, "initial job", mSetInfo);
			var task = GetSectionsAsync(mSetInfo, progress);
			_jobTask = new JobTask(job, task);
		}

		public async Task GetSectionsAsync(MSetInfo mSetInfo, IProgress<MapSection> progress)
		{
			_progress = progress;

			_colorMap = new ColorMap(mSetInfo.ColorMapEntries, mSetInfo.MapCalcSettings.MaxIterations, mSetInfo.HighColorCss);

			var numVertBlocks = 6;
			var numHoriBlocks = 6;

			for (var yBlockPtr = -3; yBlockPtr < numVertBlocks; yBlockPtr++)
			{
				for (var xBlockPtr = -4; xBlockPtr < numHoriBlocks; xBlockPtr++)
				{
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr);

					var mapSectionResponse = await _mapSectionProvider.GenerateMapSectionAsync(Subdivision, blockPosition, mSetInfo.MapCalcSettings);
					HandleResponse(mapSectionResponse);
				}
			}
		}

		private void HandleResponse(MapSectionResponse mapSectionResponse)
		{
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, Subdivision.BlockSize, _colorMap);
			var mapSection = new MapSection(Subdivision, mapSectionResponse.BlockPosition, pixels1d);
			_progress.Report(mapSection);
		}

		private byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap)
		{
			var numberofCells = blockSize.NumberOfCells;
			var result = new byte[4 * numberofCells];

			for(var rowPtr = 0; rowPtr < blockSize.Height; rowPtr++)
			{
				var resultRowPtr = -1 + blockSize.Height - rowPtr;
				var curResultPtr = resultRowPtr * blockSize.Width * 4;
				var curSourcePtr = rowPtr * blockSize.Width;

				for (var colPtr = 0; colPtr < blockSize.Width; colPtr++)
				{
					var countVal = counts[curSourcePtr++];
					countVal = Math.DivRem(countVal, 1000, out var ev);
					var escapeVel = ev / 1000d;
					var colorComps = colorMap.GetColor(countVal, escapeVel);

					for (var j = 2; j > -1; j--)
					{
						result[curResultPtr++] = colorComps[j];
					}
					result[curResultPtr++] = 255;
				}
			}

			return result;
		}

		class JobTask
		{
			public readonly Job Job;
			public readonly Task Task;

			public JobTask(Job job, Task task)
			{
				Job = job;
				Task = task;
			}
		}

	}
}

using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoader
	{
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;

		private readonly Job _job;
		private readonly int _jobNumber;

		private readonly Action<int, MapSection> _callback;
		private readonly ColorMap _colorMap;

		private readonly IList<MapSectionRequest> _pendingRequests;

		private bool _isStopping;
		private int _sectionsRequested;
		private int _sectionsCompleted;

		private TaskCompletionSource _tcs;

		#region Constructor

		public MapLoader(Job job, int jobNumber, IReadOnlyList<MapSection> loadedMapSections, Action<int, MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_job = job;
			_jobNumber = jobNumber;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));

			_colorMap = new ColorMap(job.MSetInfo.ColorMapEntries);
			_pendingRequests = CreateSectionRequests();

			_isStopping = false;
			_sectionsRequested = 0;
			_sectionsCompleted = 0;

			_tcs = null;
		}

		#endregion

		#region Public Methods

		public Task Start()
		{
			if (_tcs != null)
			{
				throw new InvalidOperationException("This MapLoader has already been started.");
			}

			_ = Task.Run(SubmitSectionRequests);

			_tcs = new TaskCompletionSource();
			return _tcs.Task;
		}

		public void Stop()
		{
			if (_tcs == null)
			{
				throw new InvalidOperationException("This MapLoader has not been started.");
			}

			if (!_isStopping && _tcs.Task.Status != TaskStatus.RanToCompletion)
			{
				_mapSectionRequestProcessor.CancelJob(_jobNumber);
				_isStopping = true;
			}
		}

		#endregion

		#region Private Methods

		private IList<MapSectionRequest> CreateSectionRequests()
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(_job.CanvasSizeInBlocks, _job.CanvasControlOffset);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			for (var yBlockPtr = 0; yBlockPtr < mapExtentInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < mapExtentInBlocks.Width; xBlockPtr++)
				{
					var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
					var mapSectionRequest = MapSectionHelper.CreateRequest(screenPosition, _job.MapBlockOffset, _job.Subdivision, _job.MSetInfo.MapCalcSettings);

					result.Add(mapSectionRequest);
				}
			}

			return result;
		}

		private void SubmitSectionRequests()
		{
			foreach(var mapSectionRequest in _pendingRequests)
			{
				if (_isStopping)
				{
					if (_sectionsCompleted == _sectionsRequested)
					{
						_tcs.SetResult();
					}
					break;
				}

				//Debug.WriteLine($"Sending request: {blockPosition}::{mapPosition} for ScreenBlkPos: {screenPosition}");

				var mapSectionWorkItem = new WorkItem<MapSectionRequest, MapSectionResponse>(_jobNumber, mapSectionRequest, HandleResponse);

				_mapSectionRequestProcessor.AddWork(mapSectionWorkItem);
				mapSectionRequest.Sent = true;
				_ = Interlocked.Increment(ref _sectionsRequested);
			}
		}

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			var mapSection = MapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionResponse, _job.MapBlockOffset, _colorMap);

			_callback(_jobNumber, mapSection);
			mapSectionRequest.Handled = true;

			_ = Interlocked.Increment(ref _sectionsCompleted);
			if (_sectionsCompleted == _job.CanvasSizeInBlocks.NumberOfCells || (_isStopping && _sectionsCompleted == _sectionsRequested))
			{
				if (!_tcs.Task.IsCompleted)
				{
					_tcs.SetResult();
				}

				var pr = _mapSectionRequestProcessor.GetPendingRequests();
				Debug.WriteLine($"MapLoader is done with Job: x, there are {pr.Count} requests still pending.");
			}
		}

		#endregion
	}
}

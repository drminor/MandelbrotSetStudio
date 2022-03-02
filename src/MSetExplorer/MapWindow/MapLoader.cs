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

		private readonly Action<object, MapSection> _callback;
		private readonly ColorMap _colorMap;

		private IList<MapSectionRequest> _pendingRequests;

		private bool _isStopping;
		private int _sectionsRequested;
		private int _sectionsCompleted;

		private TaskCompletionSource _tcs;

		#region Constructor

		public MapLoader(Job job, Action<object, MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_job = job;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));
			JobNumber = _mapSectionRequestProcessor.GetNextRequestId();

			_colorMap = new ColorMap(job.MSetInfo.ColorMapEntries);
			_pendingRequests = null; // CreateSectionRequests();

			_isStopping = false;
			_sectionsRequested = 0;
			_sectionsCompleted = 0;

			_tcs = null;
		}

		#endregion

		#region Public Properties

		public int JobNumber { get; init; }

		#endregion

		#region Public Methods

		/// <summary>
		/// Submits requests for all map sections for the job.
		/// </summary>
		/// <returns></returns>
		public Task Start()
		{
			return Start(CreateSectionRequests(_job));
		}

		public Task Start(IList<MapSectionRequest> requests)
		{
			if (_tcs != null)
			{
				throw new InvalidOperationException("This MapLoader has already been started.");
			}

			_pendingRequests = requests;

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
				_mapSectionRequestProcessor.CancelJob(JobNumber);
				_isStopping = true;
			}
		}

		#endregion

		#region Private Methods

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

				//var mapSectionWorkItem = new WorkItem<MapSectionRequest, MapSectionResponse>(JobNumber, mapSectionRequest, HandleResponse);
				_mapSectionRequestProcessor.AddWork(JobNumber, mapSectionRequest, HandleResponse);
				mapSectionRequest.Sent = true;
				_ = Interlocked.Increment(ref _sectionsRequested);
			}
		}

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			if (!(mapSectionResponse.Counts is null))
			{
				var mapSection = MapSectionHelper.CreateMapSection(mapSectionRequest, mapSectionResponse, _job.MapBlockOffset, _colorMap);

				_callback(this, mapSection);
				mapSectionRequest.Handled = true;
			}

			_ = Interlocked.Increment(ref _sectionsCompleted);
			if (_sectionsCompleted == _job.CanvasSizeInBlocks.NumberOfCells || (_isStopping && _sectionsCompleted == _sectionsRequested))
			{
				if (!_tcs.Task.IsCompleted)
				{
					_tcs.SetResult();
				}

				var pr = _mapSectionRequestProcessor.GetPendingRequests(JobNumber);
				Debug.WriteLine($"MapLoader is done with Job: {JobNumber}, there are {pr.Count} requests still pending.");
			}
		}

		#endregion

		#region Static Methods

		public static IList<MapSectionRequest> CreateSectionRequests(Job job)
		{
			var result = new List<MapSectionRequest>();

			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(job.CanvasSizeInBlocks, job.CanvasControlOffset);
			Debug.WriteLine($"Creating section requests. The map extent is {mapExtentInBlocks}.");

			for (var yBlockPtr = 0; yBlockPtr < mapExtentInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < mapExtentInBlocks.Width; xBlockPtr++)
				{
					var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
					var mapSectionRequest = MapSectionHelper.CreateRequest(screenPosition, job.MapBlockOffset, job.Subdivision, job.MSetInfo.MapCalcSettings);
					result.Add(mapSectionRequest);
				}
			}

			return result;
		}

		#endregion

	}
}

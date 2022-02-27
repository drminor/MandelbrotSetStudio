using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
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
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly DtoMapper _dtoMapper;

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

		public MapLoader(Job job, int jobNumber, Action<int, MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_job = job;
			_jobNumber = jobNumber;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));

			_dtoMapper = new DtoMapper();
			_mapSectionHelper = new MapSectionHelper(_dtoMapper);

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
					// Translate to subdivision coordinates.
					var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
					var repoPosition = RMapHelper.ToSubdivisionCoords(screenPosition, _job.MapBlockOffset, out var isInverted);
					var mapSectionRequest = _mapSectionHelper.CreateRequest(_job.Subdivision, repoPosition, isInverted, _job.MSetInfo.MapCalcSettings);

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
			var repoBlockPosition = _dtoMapper.MapFrom(mapSectionRequest.BlockPosition);
			var screenPosition = RMapHelper.ToScreenCoords(repoBlockPosition, mapSectionRequest.IsInverted, _job.MapBlockOffset);
			//Debug.WriteLine($"MapLoader handling response: {repoBlockPosition} for ScreenBlkPos: {screenPosition} Inverted = {mapSectionRequest.IsInverted}.");

			var blockSize = _job.Subdivision.BlockSize;
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, blockSize, _colorMap, !mapSectionRequest.IsInverted);
			var mapSection = new MapSection(screenPosition, blockSize, pixels1d, mapSectionRequest.SubdivisionId, repoBlockPosition);

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

		private byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap, bool invert)
		{
			if (counts == null)
			{
				return null;
			}

			var numberofCells = blockSize.NumberOfCells;
			var result = new byte[4 * numberofCells];

			for (var rowPtr = 0; rowPtr < blockSize.Height; rowPtr++)
			{
				// Calculate the array index for the beginning of this destination and source row.
				var resultRowPtr = GetResultRowPtr(blockSize.Height - 1, rowPtr, invert);

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

		private int GetResultRowPtr(int maxRowIndex, int rowPtr, bool invert) 
		{
			// The Source's origin is at the bottom, left.
			// If inverted, the Destination's origin is at the top, left, otherwise bottom, left. 
			var result = invert ? maxRowIndex - rowPtr : rowPtr;
			return result;
		}

		#endregion
	}
}

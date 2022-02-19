using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using MSS.Types.Screen;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MSetExplorer
{
	internal class MapLoader
	{
		private readonly MapSectionRequestProcessor _mapSectionRequestProcessor;
		private readonly Job _job;
		private readonly SizeInt _blockSize;
		private readonly SizeInt _mapBlockOffset;
		private readonly Action<int, MapSection> _callback;
		private readonly ColorMap _colorMap;

		private bool _isStopping;
		private int _sectionsRequested;
		private int _sectionCompleted;

		private TaskCompletionSource _tcs;

		public MapLoader(Job job, int jobNumber, Action<int, MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			JobNumber = jobNumber;

			_job = job;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));

			_blockSize = job.Subdivision.BlockSize;
			_mapBlockOffset = job.MapBlockOffset;
			_colorMap = new ColorMap(job.MSetInfo.ColorMapEntries);

			_isStopping = false;
			_sectionsRequested = 0;
			_sectionCompleted = 0;

			_tcs = null;
		}

		public int JobNumber { get; }

		public Task Start()
		{
			if (_tcs != null)
			{
				throw new InvalidOperationException("This MapLoader has already been started.");
			}

			_tcs = new TaskCompletionSource();
			_ = Task.Run(SubmitSectionRequests);
			return _tcs.Task;
		}

		private void SubmitSectionRequests()
		{
			var mapExtentInBlocks = GetMapExtentInBlocks(_job.CanvasSizeInBlocks, _mapBlockOffset);
			for (var yBlockPtr = 0; yBlockPtr < mapExtentInBlocks.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < mapExtentInBlocks.Width; xBlockPtr++)
				{
					if (_isStopping)
					{
						if (_sectionCompleted == _sectionsRequested)
						{
							_tcs.SetResult();
						}
						break;
					}

					// Translate to subdivision coordinates.
					var blockPosition = new PointInt(xBlockPtr, yBlockPtr).Translate(_mapBlockOffset);
					var mapSectionRequest = MapSectionHelper.CreateRequest(_job.Subdivision, blockPosition, _job.MSetInfo.MapCalcSettings, out var mapPosition);

					//Debug.WriteLine($"Sending request: {blockPosition}::{BigIntegerHelper.GetDisplay(mapPosition)}");

					_mapSectionRequestProcessor.AddWork(JobNumber, mapSectionRequest, HandleResponse);
					_ = Interlocked.Increment(ref _sectionsRequested);
				}
			}
		}

		private SizeInt GetMapExtentInBlocks(SizeInt canvasSizeInBlocks, SizeInt mapBlockOffset)
		{
			var result = new SizeInt(
				canvasSizeInBlocks.Width + (Math.Abs(mapBlockOffset.Width) > 0 ? 1 : 0),
				canvasSizeInBlocks.Height + (Math.Abs(mapBlockOffset.Height) > 0 ? 1 : 0)
				);

			return result;
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

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, _blockSize, _colorMap, !mapSectionRequest.Inverted);

			// Translate subdivision coordinates to block coordinates.
			var blockPosition = mapSectionResponse.BlockPosition.Diff(_mapBlockOffset);

			Debug.WriteLine($"MapLoader handling response. ScreenBlkPos: {blockPosition}, RepoBlkPos: {mapSectionResponse.BlockPosition}.");

			var mapSection = new MapSection(blockPosition, _blockSize, pixels1d);

			_callback(JobNumber, mapSection);

			_ = Interlocked.Increment(ref _sectionCompleted);
			if (_sectionCompleted == _job.CanvasSizeInBlocks.NumberOfCells || (_isStopping && _sectionCompleted == _sectionsRequested))
			{
				if (!_tcs.Task.IsCompleted)
				{
					_tcs.SetResult();
				}
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
				// Calculate the array index for the beginning of this 
				// destination and source row.
				// The Destination's origin is at the top, left.
				// The Source's origin is at the bottom, left.

				//var resultRowPtr = -1 + blockSize.Height - rowPtr;
				var resultRowPtr = GetResultRowPtr(blockSize.Height, rowPtr, invert);

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

		private int GetResultRowPtr(int blockHeight, int rowPtr, bool invert) 
		{
			var result = invert ? -1 + blockHeight - rowPtr : rowPtr;
			return result;
		}

	}
}

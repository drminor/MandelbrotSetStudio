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

		public MapLoader(Job job, Action<int, MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			GenMapRequestId = mapSectionRequestProcessor.GetNextRequestId();

			_job = job;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));

			_blockSize = job.Subdivision.BlockSize;
			_mapBlockOffset = job.MapBlockOffset;
			var mSetInfo = job.MSetInfo;

			_colorMap = new ColorMap(mSetInfo.ColorMapEntries, mSetInfo.MapCalcSettings.MaxIterations, mSetInfo.HighColorCss);

			_isStopping = false;
			_sectionsRequested = 0;
			_sectionCompleted = 0;

			_tcs = null;
		}

		public int GenMapRequestId { get; }

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
			var canvasSize = _job.CanvasSizeInBlocks;
			for (var yBlockPtr = 0; yBlockPtr < canvasSize.Height; yBlockPtr++)
			{
				for (var xBlockPtr = 0; xBlockPtr < canvasSize.Width; xBlockPtr++)
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

					var disp = BigIntegerHelper.GetDisplay(mapPosition);
					Debug.WriteLine($"Sending request for {blockPosition} with map position: {disp}");

					_mapSectionRequestProcessor.AddWork(GenMapRequestId, mapSectionRequest, HandleResponse);
					_ = Interlocked.Increment(ref _sectionsRequested);
				}
			}
		}

		public void Stop()
		{
			if (_tcs == null)
			{
				throw new InvalidOperationException("This MapLoader has not been started.");
			}

			if (!_isStopping && _tcs.Task.Status != TaskStatus.RanToCompletion)
			{
				_mapSectionRequestProcessor.CancelJob(GenMapRequestId);
				_isStopping = true;
			}
		}

		private void HandleResponse(MapSectionResponse mapSectionResponse)
		{
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, _blockSize, _colorMap);

			// Translate subdivision coordinates to canvas coordinates.
			var position = mapSectionResponse.BlockPosition.Diff(_mapBlockOffset).Scale(_blockSize);
			var offset = new SizeInt((int)_job.CanvasControlOffset.Width, (int)_job.CanvasControlOffset.Height);
			position = position.Diff(offset);
			var mapSection = new MapSection(position, _blockSize, pixels1d);

			_callback(GenMapRequestId, mapSection);

			_ = Interlocked.Increment(ref _sectionCompleted);
			if (_sectionCompleted == _job.CanvasSizeInBlocks.NumberOfCells
				|| (_isStopping && _sectionCompleted == _sectionsRequested))
			{
				if (!_tcs.Task.IsCompleted)
				{
					_tcs.SetResult();
				}
			}
		}

		private byte[] GetPixelArray(int[] counts, SizeInt blockSize, ColorMap colorMap)
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

	}
}

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
		private readonly int _jobNumber;

		private readonly Action<int, MapSection> _callback;
		private readonly ColorMap _colorMap;

		private bool _isStopping;
		private int _sectionsRequested;
		private int _sectionCompleted;

		private TaskCompletionSource _tcs;

		#region Constructor

		public MapLoader(Job job, int jobNumber, Action<int, MapSection> callback, MapSectionRequestProcessor mapSectionRequestProcessor)
		{
			_job = job;
			_jobNumber = jobNumber;
			_callback = callback;
			_mapSectionRequestProcessor = mapSectionRequestProcessor ?? throw new ArgumentNullException(nameof(mapSectionRequestProcessor));
			_colorMap = new ColorMap(job.MSetInfo.ColorMapEntries);

			_isStopping = false;
			_sectionsRequested = 0;
			_sectionCompleted = 0;

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

			_tcs = new TaskCompletionSource();
			_ = Task.Run(SubmitSectionRequests);
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

		private void SubmitSectionRequests()
		{
			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(_job.CanvasSizeInBlocks, _job.CanvasControlOffset.Round());
			var mapBlockOffset = _job.MapBlockOffset;

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
					var screenPosition = new PointInt(xBlockPtr, yBlockPtr);
					var blockPosition = ToSubdivisionCoords(screenPosition, _job, out var inverted);
					var mapSectionRequest = MapSectionHelper.CreateRequest(_job.Subdivision, blockPosition, inverted, _job.MSetInfo.MapCalcSettings, out var mapPosition);

					Debug.WriteLine($"Sending request: {blockPosition}::{mapPosition} for ScreenBlkPos: {screenPosition}");
					_mapSectionRequestProcessor.AddWork(_jobNumber, mapSectionRequest, HandleResponse);
					_ = Interlocked.Increment(ref _sectionsRequested);
				}
			}
		}

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			var blockPosition = mapSectionResponse.BlockPosition;
			var screenPosition = ToScreenCoords(blockPosition, mapSectionRequest.Inverted, _job);
			Debug.WriteLine($"MapLoader handling response: {blockPosition} for ScreenBlkPos: {screenPosition}.");

			var blockSize = _job.Subdivision.BlockSize;
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, blockSize, _colorMap, !mapSectionRequest.Inverted);
			var mapSection = new MapSection(screenPosition, blockSize, pixels1d);

			_callback(_jobNumber, mapSection);

			_ = Interlocked.Increment(ref _sectionCompleted);
			if (_sectionCompleted == _job.CanvasSizeInBlocks.NumberOfCells || (_isStopping && _sectionCompleted == _sectionsRequested))
			{
				if (!_tcs.Task.IsCompleted)
				{
					_tcs.SetResult();
				}
			}
		}

		// TODO: ToSubdivisionCoords should take a vector and return a vector
		private PointInt ToSubdivisionCoords(PointInt blockPosition, Job job, out bool inverted)
		{
			var repoPos = blockPosition.Translate(job.MapBlockOffset);

			PointInt result;
			if (repoPos.Y < 0)
			{
				inverted = true;
				result = new PointInt(repoPos.X, (repoPos.Y * -1) - 1);
			}
			else
			{
				inverted = false;
				result = repoPos;
			}

			return result;
		}

		// TODO: ToScreenCoords should take a vector and return a vector
		private PointInt ToScreenCoords(PointInt blockPosition, bool inverted, Job job)
		{
			PointInt posT;

			if (inverted)
			{
				posT = new PointInt(blockPosition.X, (blockPosition.Y + 1) * -1);
			}
			else
			{
				posT = blockPosition;
			}

			var result = posT.Diff(job.MapBlockOffset);
			return result;
		}

		private bool JobCrossesZeroY(RRectangle mapCoords)
		{
			return mapCoords.Y1.Sign != mapCoords.Y2.Sign;
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
				// The Source's origin is at the bottom, left.
				// If inverted, the Destination's origin is at the top, left, otherwise bottom, left. 

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
			var result = invert ? maxRowIndex - rowPtr : rowPtr;
			return result;
		}

		#endregion
	}
}

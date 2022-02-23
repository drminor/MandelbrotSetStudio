using MapSectionProviderLib;
using MEngineDataContracts;
using MSS.Common;
using MSS.Common.DataTransferObjects;
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
		private readonly MapSectionHelper _mapSectionHelper;
		private readonly DtoMapper _dtoMapper;
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

			_dtoMapper = new DtoMapper();
			_mapSectionHelper = new MapSectionHelper(_dtoMapper);

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
			var mapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(_job.CanvasSizeInBlocks, _job.CanvasControlOffset);
			Debug.WriteLine($"Submitting section requests. The map extent is {mapExtentInBlocks}.");

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
					var blockPosition = ToSubdivisionCoords(screenPosition, _job, out var isInverted);
					var mapSectionRequest = _mapSectionHelper.CreateRequest(_job.Subdivision, blockPosition, isInverted, _job.MSetInfo.MapCalcSettings, out var mapPosition);

					//Debug.WriteLine($"Sending request: {blockPosition}::{mapPosition} for ScreenBlkPos: {screenPosition}");
					_mapSectionRequestProcessor.AddWork(_jobNumber, mapSectionRequest, HandleResponse);
					_ = Interlocked.Increment(ref _sectionsRequested);
				}
			}
		}

		private void HandleResponse(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			var blockPositionDto = mapSectionRequest.BlockPosition;
			var repoBlockPosition = _dtoMapper.MapFrom(blockPositionDto);
			var screenPosition = ToScreenCoords(repoBlockPosition, mapSectionRequest.IsInverted, _job);
			//Debug.WriteLine($"MapLoader handling response: {blockPosition} for ScreenBlkPos: {screenPosition}.");

			var blockSize = _job.Subdivision.BlockSize;
			var pixels1d = GetPixelArray(mapSectionResponse.Counts, blockSize, _colorMap, !mapSectionRequest.IsInverted);
			var mapSection = new MapSection(screenPosition, blockSize, pixels1d, mapSectionRequest.SubdivisionId, repoBlockPosition);

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

		private BigVector ToSubdivisionCoords(PointInt blockPosition, Job job, out bool isInverted)
		{
			var bigBlockPosition = new BigVector(blockPosition.X, blockPosition.Y);

			var repoPos = bigBlockPosition.Translate(job.MapBlockOffset);

			BigVector result;
			if (repoPos.Y < 0)
			{
				isInverted = true;
				result = new BigVector(repoPos.X, (repoPos.Y * -1) - 1);
			}
			else
			{
				isInverted = false;
				result = repoPos;
			}

			return result;
		}

		// TODO: ToScreenCoords should take a vector and return a vector
		private PointInt ToScreenCoords(BigVector blockPosition, bool inverted, Job job)
		{
			BigVector posT;

			if (inverted)
			{
				posT = new BigVector(blockPosition.XNumerator, (blockPosition.YNumerator + 1) * -1);
			}
			else
			{
				posT = blockPosition;
			}

			var screenOffsetRat = posT.Diff(job.MapBlockOffset);
			var reducedOffset = Reducer.Reduce(screenOffsetRat);

			if (BigIntegerHelper.TryConvertToInt(reducedOffset, out var values))
			{
				var result = new PointInt(values);
				return result;
			}
			else
			{
				throw new InvalidOperationException($"Cannot convert the ScreenCoords to integers.");
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

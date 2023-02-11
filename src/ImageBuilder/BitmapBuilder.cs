using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public class BitmapBuilder
	{
		private const double VALUE_FACTOR = 10000;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionHelper _mapSectionHelper;

		private int? _currentJobNumber;
		private IDictionary<int, MapSection?>? _currentResponses;
		private bool _isStopping;

		public BitmapBuilder(IMapLoaderManager mapLoaderManager)
		{
			_mapLoaderManager = mapLoaderManager;

			var mapSectionVectorsPool = new MapSectionVectorsPool(RMapConstants.BLOCK_SIZE, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			var mapSectionValuesPool = new MapSectionValuesPool(RMapConstants.BLOCK_SIZE, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			var mapSectionZVectorsPool = new MapSectionZVectorsPool(RMapConstants.BLOCK_SIZE, limbCount:2, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionHelper = new MapSectionHelper(mapSectionVectorsPool, mapSectionValuesPool, mapSectionZVectorsPool);

			_currentJobNumber = null;
			_currentResponses = null;
			_isStopping = false;
		}

		public long NumberOfCountValSwitches { get; private set; }

		public async Task<byte[]?> BuildAsync(MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, CancellationToken ct, Action<double>? statusCallBack = null)
		{
			var mapBlockOffset = mapAreaInfo.MapBlockOffset;
			var canvasControlOffset = mapAreaInfo.CanvasControlOffset;

			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			var imageSize = mapAreaInfo.CanvasSize;

			var result = new byte[imageSize.NumberOfCells * 4];

			try
			{
				var numberOfWholeBlocks = RMapHelper.GetMapExtentInBlocks(imageSize, canvasControlOffset, blockSize);
				var w = numberOfWholeBlocks.Width;
				var h = numberOfWholeBlocks.Height;

				Debug.WriteLine($"The PngBuilder is processing section requests. The map extent is {numberOfWholeBlocks}. The ColorMap has Id: {colorBandSet.Id}.");

				var destPixPtr = 0;

				for (var blockPtrY = h - 1; blockPtrY >= 0 && !ct.IsCancellationRequested; blockPtrY--)
				{
					var blocksForThisRow = await GetAllBlocksForRowAsync(blockPtrY, w, mapBlockOffset, mapAreaInfo.Precision, mapAreaInfo.Subdivision, mapCalcSettings);
					if (ct.IsCancellationRequested || blocksForThisRow.Count == 0)
					{
						return null;
					} 

					//var checkCnt = blocksForThisRow.Count;
					//Debug.Assert(checkCnt == w);

					var numberOfLines = GetNumberOfLines(blockPtrY, imageSize.Height, h, blockSize.Height, canvasControlOffset.Y, out var linesTopSkip);

					var startingLinePtr = blockSize.Height - 1 - linesTopSkip;
					var endingLinePtr = startingLinePtr - (numberOfLines - 1);

					for (var linePtr = startingLinePtr; linePtr >= endingLinePtr; linePtr--)
					{
						for (var blockPtrX = 0; blockPtrX < w; blockPtrX++)
						{
							MapSection? mapSection;
							try
							{
								mapSection = blocksForThisRow[blockPtrX];
							}
							catch (Exception e)
							{
								Debug.WriteLine($"Got ee: {e}.");
								throw;
							}

							if (mapSection == null)
							{
								Debug.WriteLine($"Got a null mapSection.");
							}

							var countsForThisSegment = GetOneLineFromCountsBlock(mapSection?.MapSectionValues?.Counts, linePtr, blockSize.Width);
							//var escVelsForThisSegment = GetOneLineFromCountsBlock(mapSection?.MapSectionValues?.EscapeVelocities, linePtr, blockSize.Width);
							var escVelsForThisSegment = new ushort[countsForThisSegment?.Length ?? 0];


							var segmentLength = GetSegmentLength(blockPtrX, imageSize.Width, w, blockSize.Width, canvasControlOffset.X, out var samplesToSkip);

							try
							{
								FillImageLineSegment(result, destPixPtr, countsForThisSegment, escVelsForThisSegment, segmentLength, samplesToSkip, colorMap);
								destPixPtr += segmentLength;
							}
							catch (Exception e)
							{
								if (!ct.IsCancellationRequested)
								{
									Debug.WriteLine($"FillPngImageLineSegment encountered an exception: {e}.");
									throw;
								}
							}
						}
					}

					var percentageCompleted = (h - blockPtrY) / (double)h;
					statusCallBack?.Invoke(100 * percentageCompleted);
				}
			}
			catch (Exception e)
			{
				if (!ct.IsCancellationRequested)
				{
					Debug.WriteLine($"PngBuilder encountered an exception: {e}.");
					throw;
				}
			}

			return result;
		}

		private int GetNumberOfLines(int blockPtrY, int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY, out int linesToSkip)
		{
			int numberOfLines;

			if (blockPtrY == 0)
			{
				linesToSkip = canvasControlOffsetY;
				numberOfLines = blockHeight - canvasControlOffsetY;
			}
			else if (blockPtrY == numberOfWholeBlocksY - 1)
			{
				numberOfLines = canvasControlOffsetY + imageHeight - (blockHeight * (numberOfWholeBlocksY - 1));
				linesToSkip = blockHeight - numberOfLines;

			}
			else
			{
				linesToSkip = 0;
				numberOfLines = blockHeight;
			}

			return numberOfLines;

		}

		private int GetSegmentLength(int blockPtrX, int imageWidth, int numberOfWholeBlocksX, int blockWidth, int canvasControlOffsetX, out int samplesToSkip)
		{
			int result;

			if (blockPtrX == 0)
			{
				samplesToSkip = canvasControlOffsetX;
				result = blockWidth - canvasControlOffsetX;
			}
			else if (blockPtrX == numberOfWholeBlocksX - 1)
			{
				samplesToSkip = 0;
				result = canvasControlOffsetX + imageWidth - (blockWidth * (numberOfWholeBlocksX - 1));
			}
			else
			{
				samplesToSkip = 0;
				result = blockWidth;
			}

			return result;
		}

		private async Task<IDictionary<int, MapSection?>> GetAllBlocksForRowAsync(int rowPtr, int stride, BigVector mapBlockOffset, int precision, Subdivision subdivision, MapCalcSettings mapCalcSettings)
		{
			var requests = new List<MapSectionRequest>();
			var ownerId = ObjectId.GenerateNewId().ToString();
			var jobOwnerType = JobOwnerType.ImageBuilder;

			for (var colPtr = 0; colPtr < stride; colPtr++)
			{
				var key = new PointInt(colPtr, rowPtr);
				var mapSectionRequest = _mapSectionHelper.CreateRequest(key, mapBlockOffset, precision, ownerId, jobOwnerType, subdivision, mapCalcSettings);
				requests.Add(mapSectionRequest);
			}

			_currentResponses = new Dictionary<int, MapSection?>();

			try
			{
				_currentJobNumber = _mapLoaderManager.Push(mapBlockOffset, requests, MapSectionReady);

				var task = _mapLoaderManager.GetTaskForJob(_currentJobNumber.Value);

				if (task != null)
				{
					try
					{
						await task;
					}
					catch (OperationCanceledException)
					{
						Debug.WriteLine($"The BitmapBuilder's MapLoader's Task is cancelled.");
						throw;
					}
					catch (Exception e)
					{
						Debug.WriteLine($"The BitmapBuilder's MapLoader's Task encountered an exception: {e}.");
						throw;
					}
				}
				else
				{
					Debug.WriteLine($"The BitmapBuilder's MapLoader's Task was not found.");
					throw new InvalidOperationException("The MapLoaderManger task could be found.");
				}
			}
			catch
			{
				_isStopping = true;
			}

			if (_isStopping)
			{
				_currentResponses.Clear();
			}

			return _currentResponses;
		}

		private void MapSectionReady(MapSection mapSection, int jobNumber, bool isLastSection)
		{
			if (!_isStopping && jobNumber == _currentJobNumber)
			{
				if (!mapSection.IsEmpty)
				{
					_currentResponses?.Add(mapSection.BlockPosition.X, mapSection);
				}
				else
				{
					Debug.WriteLine($"Bitmap Builder recieved an empty MapSection. LastSection = {isLastSection}, Job Number: {jobNumber}.");
				}
			}
		}

		private ushort[]? GetOneLineFromCountsBlock(ushort[]? counts, int lPtr, int stride)
		{
			if (counts == null)
			{
				return null;
			}
			else
			{
				var result = new ushort[stride];

				Array.Copy(counts, lPtr * stride, result, 0, stride);
				return result;
			}
		}

		private void FillImageLineSegment(byte[] imageData, int pixPtr, ushort[]? counts, ushort[]? escapeVelocities, int lineLength, int samplesToSkip, ColorMap colorMap)
		{
			if (counts == null || escapeVelocities == null)
			{
				FillPngImageLineSegmentWithWhite(imageData, pixPtr, lineLength);
				return;
			}

			var previousCountVal = counts[0];

			for (var xPtr = 0; xPtr < lineLength; xPtr++)
			{
				var countVal = counts[xPtr + samplesToSkip];

				if (countVal != previousCountVal)
				{
					NumberOfCountValSwitches++;
					previousCountVal = countVal;
				}

				var escapeVelocity = colorMap.UseEscapeVelocities ? escapeVelocities[xPtr + samplesToSkip] / VALUE_FACTOR : 0;

				if (escapeVelocity > 1.0)
				{
					Debug.WriteLine($"The Escape Velocity is greater that 1.0");
				}

				var offset = pixPtr++ * 4;
				var dest = new Span<byte>(imageData, offset, 4);

				colorMap.PlaceColor(countVal, escapeVelocity, dest);
			}
		}

		private void FillPngImageLineSegmentWithWhite(Span<byte> imageLine, int pixPtr, int len)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				var offset = pixPtr++ * 4;

				imageLine[offset] = 255;
				imageLine[offset + 1] = 255;
				imageLine[offset + 2] = 255;
				imageLine[offset + 3] = 255;
			}
		}

	}
}


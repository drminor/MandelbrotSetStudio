using MEngineDataContracts;
using MongoDB.Bson;
using MSS.Common;
using MSS.Common.DataTransferObjects;
using MSS.Types;
using MSS.Types.MSet;
using PngImageLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public class PngBuilder
	{
		private const double VALUE_FACTOR = 10000;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionHelper _mapSectionHelper;

		private int? _currentJobNumber;
		private IDictionary<int, MapSection?>? _currentResponses;

		public PngBuilder(IMapLoaderManager mapLoaderManager)
		{
			_mapLoaderManager = mapLoaderManager;
			var mapSectionVectorsPool = new MapSectionVectorsPool(RMapConstants.BLOCK_SIZE, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			//var mapSectionValuesPool = new MapSectionValuesPool(RMapConstants.BLOCK_SIZE, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			var mapSectionZVectorsPool = new MapSectionZVectorsPool(RMapConstants.BLOCK_SIZE, limbCount: 2, RMapConstants.MAP_SECTION_VALUE_POOL_SIZE);
			_mapSectionHelper = new MapSectionHelper(mapSectionVectorsPool/*, mapSectionValuesPool*/, mapSectionZVectorsPool);

			_currentJobNumber = null;
			_currentResponses = null;
		}

		public long NumberOfCountValSwitches { get; private set; }

		public async Task<bool> BuildAsync(string imageFilePath, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, Action<double> statusCallBack, CancellationToken ct)
		{
			var mapBlockOffset = mapAreaInfo.MapBlockOffset;
			var canvasControlOffset = mapAreaInfo.CanvasControlOffset;

			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			PngImage? pngImage = null;
			
			try
			{
				var stream = File.Open(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

				var imageSize = mapAreaInfo.CanvasSize;
				pngImage = new PngImage(stream, imageFilePath, imageSize.Width, imageSize.Height);

				var numberOfWholeBlocks = RMapHelper.GetMapExtentInBlocks(imageSize, canvasControlOffset, blockSize);
				var w = numberOfWholeBlocks.Width;
				var h = numberOfWholeBlocks.Height;

				Debug.WriteLine($"The PngBuilder is processing section requests. The map extent is {numberOfWholeBlocks}. The ColorMap has Id: {colorBandSet.Id}.");

				for (var blockPtrY = h - 1; blockPtrY >= 0 && !ct.IsCancellationRequested; blockPtrY--)
				{
					var blocksForThisRow = await GetAllBlocksForRowAsync(blockPtrY, w, mapBlockOffset, mapAreaInfo.Precision, mapAreaInfo.Subdivision, mapCalcSettings);

					//var checkCnt = blocksForThisRow.Count;
					//Debug.Assert(checkCnt == w);

					var numberOfLines = GetNumberOfLines(blockPtrY, imageSize.Height, h, blockSize.Height, canvasControlOffset.Y, out var linesTopSkip);
					var startingLinePtr = blockSize.Height - 1 - linesTopSkip;
					var endingLinePtr = startingLinePtr - (numberOfLines - 1);

					for (var linePtr = startingLinePtr; linePtr >= endingLinePtr; linePtr--)
					{
						var iLine = pngImage.ImageLine;
						var destPixPtr = 0;

						for (var blockPtrX = 0; blockPtrX < w; blockPtrX++)
						{
							var mapSection = blocksForThisRow[blockPtrX];
							var countsForThisLine = GetOneLineFromCountsBlock(mapSection?.MapSectionVectors?.Counts, linePtr, blockSize.Width);
							//var escVelsForThisLine = GetOneLineFromCountsBlock(mapSection?.MapSectionValues?.EscapeVelocities, linePtr, blockSize.Width);
							var escVelsForThisLine = new ushort[countsForThisLine?.Length ?? 0];

							var lineLength = GetSegmentLength(blockPtrX, imageSize.Width, w, blockSize.Width, canvasControlOffset.X, out var samplesToSkip);

							try
							{
								FillPngImageLineSegment(iLine, destPixPtr, countsForThisLine, escVelsForThisLine, lineLength, samplesToSkip, colorMap);
								destPixPtr += lineLength;
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

						pngImage.WriteLine(iLine);
					}

					var percentageCompleted = (h - blockPtrY) / (double)h;
					statusCallBack(100 * percentageCompleted);
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
			finally
			{
				if (!ct.IsCancellationRequested)
				{
					pngImage?.End();
				}
				else
				{
					pngImage?.Abort();
				}
			}

			return true;
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

			_currentJobNumber = _mapLoaderManager.Push(requests, MapSectionReady);
			_currentResponses = new Dictionary<int, MapSection?>();

			var task = _mapLoaderManager.GetTaskForJob(_currentJobNumber.Value);

			if (task != null)
			{
				try
				{
					await task;
				}
				catch (OperationCanceledException)
				{

				}
			}

			return _currentResponses ?? new Dictionary<int, MapSection?>();
		}

		private void MapSectionReady(MapSection mapSection, int jobNumber, bool isLastSection)
		{
			if (jobNumber == _currentJobNumber)
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

		private void FillPngImageLineSegment(ImageLine iLine, int pixPtr, ushort[]? counts, ushort[]? escapeVelocities, int lineLength, int samplesToSkip, ColorMap colorMap)
		{
			if (counts == null || escapeVelocities == null)
			{
				FillPngImageLineSegmentWithWhite(iLine, pixPtr, lineLength);
				return;
			}

			var cComps = new byte[4];
			var dest = new Span<byte>(cComps);

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

				colorMap.PlaceColor(countVal, escapeVelocity, dest);

				ImageLineHelper.SetPixel(iLine, pixPtr++, cComps[2], cComps[1], cComps[0]);
			}
		}

		private void FillPngImageLineSegmentWithWhite(ImageLine iLine, int pixPtr, int len)
		{
			for (var xPtr = 0; xPtr < len; xPtr++)
			{
				ImageLineHelper.SetPixel(iLine, pixPtr++, 255, 255, 255);
			}
		}
		/*
		private int GetNumberOfLines(int blockPtrY, int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY, out int numberOfLinesToSkip)
		{
			int result;

			if (blockPtrY == 0)
			{
				numberOfLinesToSkip = 0;
				result = blockHeight - canvasControlOffsetY;
			}
			else if (blockPtrY == numberOfWholeBlocksY - 1)
			{
				numberOfLinesToSkip = blockHeight - canvasControlOffsetY;
				result = canvasControlOffsetY;
			}
			else
			{
				numberOfLinesToSkip = 0;
				result = blockHeight;
			}

			return result;
		}

		private int GetLineLength(int blockPtrX, int imageWidth, int numberOfWholeBlocksX, int blockWidth, int canvasControlOffsetX, out int samplesToSkip)
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
		*/

	}
}


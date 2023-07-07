using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public class BitmapBuilder : IBitmapBuilder
	{
		//private const double VALUE_FACTOR = 10000;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private int? _currentJobNumber;
		private IDictionary<int, MapSection?>? _currentResponses;
		private bool _isStopping;

		public BitmapBuilder(IMapLoaderManager mapLoaderManager)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionBuilder = new MapSectionBuilder();

			_currentJobNumber = null;
			_currentResponses = null;
			_isStopping = false;
		}

		public long NumberOfCountValSwitches { get; private set; }

		public async Task<byte[]?> BuildAsync(ObjectId jobId, OwnerType ownerType, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities, CancellationToken ct, Action<double>? statusCallBack = null)
		{
			var mapBlockOffset = mapAreaInfo.MapBlockOffset;
			var canvasControlOffset = mapAreaInfo.CanvasControlOffset;

			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			var imageSize = mapAreaInfo.CanvasSize.Round();

			var result = new byte[imageSize.NumberOfCells * 4];

			try
			{
				var numberOfWholeBlocks = RMapHelper.GetMapExtentInBlocks(imageSize, canvasControlOffset, blockSize);
				var w = numberOfWholeBlocks.Width;
				var h = numberOfWholeBlocks.Height;

				Debug.WriteLine($"The BitmapBuilder is processing section requests. The map extent is {numberOfWholeBlocks} w = {w}, h = {h} CanvasControlOffset = {canvasControlOffset}. The ColorMap has Id: {colorBandSet.Id}.");

				var destPixPtr = 0;

				for (var blockPtrY = h - 1; blockPtrY >= 0 && !ct.IsCancellationRequested; blockPtrY--)
				{
					var blockIndexY = blockPtrY - (h / 2);
					var blocksForThisRow = await GetAllBlocksForRowAsync(jobId, ownerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId, mapBlockOffset, blockPtrY, blockIndexY, w, mapCalcSettings, mapAreaInfo.Precision);
					if (ct.IsCancellationRequested || blocksForThisRow.Count == 0)
					{
						return null;
					}

					//var checkCnt = blocksForThisRow.Count;
					//Debug.Assert(checkCnt == w);

					var numberOfLines = BitmapHelper.GetNumberOfLines(blockPtrY, imageSize.Height, h, blockSize.Height, canvasControlOffset.Y, out var linesTopSkip);

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

							var countsForThisSegment = BitmapHelper.GetOneLineFromCountsBlock(mapSection?.MapSectionVectors?.Counts, linePtr, blockSize.Width);
							//var escVelsForThisSegment = GetOneLineFromCountsBlock(mapSection?.MapSectionValues?.EscapeVelocities, linePtr, blockSize.Width);
							var escVelsForThisSegment = new ushort[countsForThisSegment?.Length ?? 0];


							var segmentLength = BitmapHelper.GetSegmentLength(blockPtrX, imageSize.Width, w, blockSize.Width, canvasControlOffset.X, out var samplesToSkip);

							try
							{
								BitmapHelper.FillImageLineSegment(result, destPixPtr, countsForThisSegment, escVelsForThisSegment, segmentLength, samplesToSkip, colorMap);
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

		//private int GetNumberOfLines(int blockPtrY, int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY, out int linesToSkip)
		//{
		//	int numberOfLines;

		//	if (blockPtrY == 0)
		//	{
		//		// This is the block with the largest y coordinate (aka the last block)
		//		linesToSkip = 0;
		//		var numberOfLinesForFirstBlock = GetNumberOfLinesForFirstBlock(imageHeight, numberOfWholeBlocksY, blockHeight, canvasControlOffsetY);
		//		var numberOfLinesSoFar = numberOfLinesForFirstBlock + (blockHeight * (numberOfWholeBlocksY - 2));
		//		numberOfLines = imageHeight - numberOfLinesSoFar;
		//	}
		//	else if (blockPtrY == numberOfWholeBlocksY - 1)
		//	{
		//		// This is the block with the smallest y coordinate (aka the first block)
		//		numberOfLines = GetNumberOfLinesForFirstBlock(imageHeight, numberOfWholeBlocksY, blockHeight, canvasControlOffsetY);
		//		linesToSkip = blockHeight - numberOfLines; // (Since the pixel lines are accessed from high to low index, this is measured from index = blockHeight - 1)
		//	}
		//	else
		//	{
		//		linesToSkip = 0;
		//		numberOfLines = blockHeight;
		//	}

		//	return numberOfLines;

		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//private int GetNumberOfLinesForFirstBlock(int imageHeight, int numberOfWholeBlocksY, int blockHeight, int canvasControlOffsetY)
		//{
		//	return canvasControlOffsetY + imageHeight - (blockHeight * (numberOfWholeBlocksY - 1));
		//}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		//private int GetSegmentLength(int blockPtrX, int imageWidth, int numberOfWholeBlocksX, int blockWidth, int canvasControlOffsetX, out int samplesToSkip)
		//{
		//	// TODO: Build an array of Segment lengths, once and then re-use for each row.
		//	int result;

		//	if (blockPtrX == 0)
		//	{
		//		// TODO: Why does this work for the x-axis, but not the y-axis.
		//		samplesToSkip = canvasControlOffsetX;
		//		result = blockWidth - canvasControlOffsetX;
		//	}
		//	else if (blockPtrX == numberOfWholeBlocksX - 1)
		//	{
		//		samplesToSkip = 0;
		//		result = canvasControlOffsetX + imageWidth - (blockWidth * (numberOfWholeBlocksX - 1));
		//	}
		//	else
		//	{
		//		samplesToSkip = 0;
		//		result = blockWidth;
		//	}

		//	return result;
		//}

		//private async Task<IDictionary<int, MapSection?>> GetAllBlocksForRowAsync(Subdivision subdivision, BigVector mapBlockOffset, int rowPtr, int stride, MapCalcSettings mapCalcSettings, int precision)

		private async Task<IDictionary<int, MapSection?>> GetAllBlocksForRowAsync(ObjectId jobId, OwnerType ownerType, Subdivision subdivision, ObjectId originalSourceSubdivisionId, BigVector mapBlockOffset, int rowPtr, int blockIndexY, int stride, MapCalcSettings mapCalcSettings, int precision)
		{
			var jobType = JobType.SizeEditorPreview;
			var requests = new List<MapSectionRequest>();

			for (var colPtr = 0; colPtr < stride; colPtr++)
			{
				var key = new PointInt(colPtr, rowPtr);

				var blockIndexX = colPtr - (stride / 2);
				var screenPositionRelativeToCenter = new VectorInt(blockIndexX, blockIndexY);
				var mapSectionRequest = _mapSectionBuilder.CreateRequest(jobType, key, screenPositionRelativeToCenter, mapBlockOffset, precision, jobId.ToString(), ownerType, subdivision, originalSourceSubdivisionId, mapCalcSettings, colPtr);
				requests.Add(mapSectionRequest);
			}

			_currentResponses = new Dictionary<int, MapSection?>();

			try
			{
				//_currentJobNumber = _mapLoaderManager.Push(requests, MapSectionReady);
				var mapSectionResponses = _mapLoaderManager.Push(requests, MapSectionReady, out var newJobNumber, out var _);
				_currentJobNumber = newJobNumber;

				foreach (var response in mapSectionResponses)
				{
					_currentResponses.Add(response.ScreenPosition.X, response);
				}

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
					// TODO: Add logic to confirm that all of the responses were received.
					//Debug.WriteLine($"The BitmapBuilder's MapLoader's Task was not found.");
					//throw new InvalidOperationException("The MapLoaderManger task could be found.");
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

		private void MapSectionReady(MapSection mapSection)
		{
			if (!_isStopping && mapSection.JobNumber == _currentJobNumber)
			{
				if (!mapSection.IsEmpty)
				{
					_currentResponses?.Add(mapSection.ScreenPosition.X, mapSection);
				}
				else
				{
					Debug.WriteLine($"Bitmap Builder recieved an empty MapSection. LastSection = {mapSection.IsLastSection}, Job Number: {mapSection.JobNumber}.");
				}
			}
		}

		//private ushort[]? GetOneLineFromCountsBlock(ushort[]? counts, int lPtr, int stride)
		//{
		//	if (counts == null)
		//	{
		//		return null;
		//	}
		//	else
		//	{
		//		var result = new ushort[stride];

		//		Array.Copy(counts, lPtr * stride, result, 0, stride);
		//		return result;
		//	}
		//}

		//private void FillImageLineSegment(byte[] imageData, int pixPtr, ushort[]? counts, ushort[]? escapeVelocities, int lineLength, int samplesToSkip, ColorMap colorMap)
		//{
		//	if (counts == null || escapeVelocities == null)
		//	{
		//		FillPngImageLineSegmentWithWhite(imageData, pixPtr, lineLength);
		//		return;
		//	}

		//	var previousCountVal = counts[0];

		//	for (var xPtr = 0; xPtr < lineLength; xPtr++)
		//	{
		//		var countVal = counts[xPtr + samplesToSkip];

		//		if (countVal != previousCountVal)
		//		{
		//			NumberOfCountValSwitches++;
		//			previousCountVal = countVal;
		//		}

		//		var escapeVelocity = colorMap.UseEscapeVelocities ? escapeVelocities[xPtr + samplesToSkip] / VALUE_FACTOR : 0;

		//		if (escapeVelocity > 1.0)
		//		{
		//			Debug.WriteLine($"The Escape Velocity is greater that 1.0");
		//		}

		//		var offset = pixPtr++ * 4;
		//		var dest = new Span<byte>(imageData, offset, 4);

		//		colorMap.PlaceColor(countVal, escapeVelocity, dest);
		//	}
		//}

		//private void FillPngImageLineSegmentWithWhite(Span<byte> imageLine, int pixPtr, int len)
		//{
		//	for (var xPtr = 0; xPtr < len; xPtr++)
		//	{
		//		var offset = pixPtr++ * 4;

		//		imageLine[offset] = 255;
		//		imageLine[offset + 1] = 255;
		//		imageLine[offset + 2] = 255;
		//		imageLine[offset + 3] = 255;
		//	}
		//}

	}
}


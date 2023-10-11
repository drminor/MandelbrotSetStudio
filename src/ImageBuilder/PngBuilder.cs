using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using PngImageLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public class PngBuilder : IImageBuilder
	{
		#region Private Fields

		//private const double VALUE_FACTOR = 10000;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private int? _currentJobNumber;
		private IDictionary<int, MapSection?>? _mapSectionsForRow;

		#endregion

		#region Constructor

		public PngBuilder(IMapLoaderManager mapLoaderManager)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionBuilder = new MapSectionBuilder();

			_currentJobNumber = null;
			_mapSectionsForRow = null;
		}

		#endregion

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public async Task<bool> BuildAsync(string imageFilePath, ObjectId jobId, OwnerType ownerType, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, bool useEscapeVelocities, MapCalcSettings mapCalcSettings, 
			Action<double> statusCallBack, CancellationToken ct)
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

				var msrJob = _mapLoaderManager.CreateMapSectionRequestJob(JobType.Image, jobId.ToString(), ownerType, mapAreaInfo.Subdivision, mapAreaInfo.OriginalSourceSubdivisionId.ToString(),
					mapBlockOffset, mapAreaInfo.Precision, crossesXZero: false, mapCalcSettings: mapCalcSettings);


				var imageSize = mapAreaInfo.CanvasSize.Round();
				pngImage = new PngImage(stream, imageFilePath, imageSize.Width, imageSize.Height);

				var newMapExtentInBlocks = RMapHelper.GetMapExtentInBlocks(imageSize, canvasControlOffset, blockSize, out var sizeOfFirstBlock, out var sizeOfLastBlock);
				var stride = newMapExtentInBlocks.Width;
				var h = newMapExtentInBlocks.Height;


				Debug.WriteLine($"The PngBuilder is processing section requests. The map extent is {newMapExtentInBlocks}. The ColorMap has Id: {colorBandSet.Id}.");

				var segmentLengths = BitmapHelper.GetSegmentLengths(newMapExtentInBlocks.Width, sizeOfFirstBlock.Width, sizeOfLastBlock.Width, blockSize.Width);

				for (var blockPtr = h - 1; blockPtr >= 0 && !ct.IsCancellationRequested; blockPtr--)
				{
					var blockIndexY = blockPtr - (h / 2);
					var blocksForThisRow = await GetAllBlocksForRowAsync(msrJob, blockPtr, blockIndexY, stride);

					Debug.Assert(blocksForThisRow.Count == newMapExtentInBlocks.Width);

					// An Inverted MapSection should be processed from first to last instead of as we do normally from last to first.

					// MapSection.IsInverted indicates that the MapSection was generated using postive y coordinates, but in this case, the mirror image is being displayed.
					// Normally we must process the contents of the MapSection from last Y to first Y because the Map coordinates increase from the bottom of the display to the top of the display
					// But the screen coordinates increase from the top of the display to be bottom.
					// We set the invert flag to indicate that the contents should be processed from last y to first y to compensate for the Map/Screen direction difference.

					var invert = !blocksForThisRow[0]?.IsInverted ?? false; // Invert the coordinates if the MapSection is not Inverted. Do not invert if the MapSection is inverted.

					var (startingLinePtr, numberOfLines, lineIncrement) = BitmapHelper.GetNumberOfLines(blockPtr, invert, newMapExtentInBlocks.Height, sizeOfFirstBlock.Height, sizeOfLastBlock.Height, blockSize.Height);

					BuildARow(pngImage, blockPtr, invert, startingLinePtr, numberOfLines, lineIncrement, newMapExtentInBlocks.Width, blocksForThisRow, segmentLengths, colorMap, blockSize.Width, ct);

					var percentageCompleted = (h - blockPtr) / (double)h;
					statusCallBack(100 * percentageCompleted);
				}
			}
			catch (Exception e)
			{
				if (!ct.IsCancellationRequested)
				{
					await Task.Delay(10);
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

		#endregion

		#region Private Methods

		private void BuildARow(PngImage pngImage, int blockPtrY, bool isInverted, int startingPtr, int numberOfLines, int increment, int extentInBlocksWidth, 
			IDictionary<int, MapSection?> blocksForThisRow, ValueTuple<int, int>[] segmentLengths, ColorMap colorMap, int blockSizeWidth, CancellationToken ct)
		{
			var linePtr = startingPtr;
			for (var cntr = 0; cntr < numberOfLines; cntr++)
			{
				var iLine = pngImage.ImageLine;
				var destPixPtr = 0;

				for (var blockPtrX = 0; blockPtrX < extentInBlocksWidth; blockPtrX++)
				{
					var mapSection = blocksForThisRow[blockPtrX];
					var invertThisBlock = !mapSection?.IsInverted ?? false;

					Debug.Assert(invertThisBlock == isInverted, $"The block at {blockPtrX}, {blockPtrY} has a differnt value of isInverted as does the block at 0, {blockPtrY}.");

					var countsForThisLine = BitmapHelper.GetOneLineFromCountsBlock(mapSection?.MapSectionVectors?.Counts, linePtr, blockSizeWidth);
					var escVelsForThisLine = BitmapHelper.GetOneLineFromCountsBlock(mapSection?.MapSectionVectors?.EscapeVelocities, linePtr, blockSizeWidth);
					//var escVelsForThisLine = new ushort[countsForThisLine?.Length ?? 0];

					var lineLength = segmentLengths[blockPtrX].Item1;
					var samplesToSkip = segmentLengths[blockPtrX].Item2;

					try
					{
						BitmapHelper.FillPngImageLineSegment(iLine, destPixPtr, countsForThisLine, escVelsForThisLine, lineLength, samplesToSkip, colorMap);
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

				linePtr += increment;
			}
		}



		private async Task<IDictionary<int, MapSection?>> GetAllBlocksForRowAsync(MsrJob msrJob, int rowPtr, int blockIndexY, int stride)
		{
			var requests = new List<MapSectionRequest>();

			for (var colPtr = 0; colPtr < stride; colPtr++)
			{
				var key = new PointInt(colPtr, rowPtr);

				var blockIndexX = colPtr - (stride / 2);
				var screenPositionRelativeToCenter = new VectorInt(blockIndexX, blockIndexY);
				var mapSectionRequest = _mapSectionBuilder.CreateRequest(msrJob, requestNumber: colPtr, screenPosition: key, screenPositionRelativeToCenter: screenPositionRelativeToCenter);

				requests.Add(mapSectionRequest);
			}

			//_currentJobNumber = _mapLoaderManager.Push(requests, MapSectionReady);
			var mapSections = _mapLoaderManager.Push(msrJob, requests, MapSectionReady, msrJob.CancellationTokenSource.Token, out var _);
			_currentJobNumber = msrJob.MapLoaderJobNumber;

			_mapSectionsForRow = new Dictionary<int, MapSection?>();

			foreach (var mapSection in mapSections)
			{
				_mapSectionsForRow.Add(mapSection.ScreenPosition.X, mapSection);
			}

			// TODO: Create a wait handle that is signalled when the MsrJob raises its JobComplete Event.
			//var task = _mapLoaderManager.GetTaskForJob(_currentJobNumber.Value);

			Task? task = null;

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

			return _mapSectionsForRow ?? new Dictionary<int, MapSection?>();
		}

		private void MapSectionReady(MapSection mapSection)
		{
			if (mapSection.JobNumber == _currentJobNumber)
			{
				if (!mapSection.IsEmpty)
				{
					_mapSectionsForRow?.Add(mapSection.ScreenPosition.X, mapSection);
				}
				else
				{
					Debug.WriteLine($"Bitmap Builder recieved an empty MapSection. LastSection = {mapSection.IsLastSection}, Job Number: {mapSection.JobNumber}.");
				}
			}
		}

		#endregion

		#region Old Code

		///// <summary>
		///// 
		///// </summary>
		///// <param name="blockPtr">Which row is being processed.</param>
		///// <param name="invert">True if the Counts Array should be accessed from high to low index values, i.e., from top to bottom. The index into Counts increase from left to right, and from bottom to top.</param>
		///// <param name="extentInBlocksY"></param>
		///// <param name="heightOfFirstBlock"></param>
		///// <param name="heightOfLastBlock"></param>
		///// <param name="blockHeight"></param>
		///// <returns></returns>
		//private (int startingPtr, int endingPtr, int increment) GetNumberOfLines(int blockPtr, bool invert, int extentInBlocksY, int heightOfFirstBlock, int heightOfLastBlock, int blockHeight)
		//{
		//	//var numberOfLines = BitmapHelper.GetNumberOfLines(blockPtrY, imageSize.Height, h, blockSize.Height, canvasControlOffset.Y, out var linesTopSkip);

		//	int startingLinePtr;
		//	int numberOfLines;

		//	if (invert)
		//	{
		//		// Invert = true is the normal case. Since in the vertical, Map Coordinates are opposite of screen coordinates.
		//		// The first row from counts is at the bottom of the image.
		//		// The last row from counts is at the top of the image.

		//		// The startingLinePtr starts big and is reduced by numberOfLines

		//		if (blockPtr == 0)
		//		{
		//			// This is the block with the smallest Map Coordinate and the largest Y screen coordinate (aka the first block)

		//			startingLinePtr = blockHeight - 1; //(heightOfFirstBlock ranges from 1 to 128, startingLinePtr ranges from 127 to 0)
		//			numberOfLines = heightOfFirstBlock;
		//		}
		//		else if (blockPtr == extentInBlocksY - 1)
		//		{
		//			// This is the block with the largest Map Coordinate and the smallest Y screen coordinate, (aka the last block)

		//			startingLinePtr = heightOfLastBlock - 1;
		//			numberOfLines = heightOfLastBlock;
		//		}
		//		else
		//		{
		//			startingLinePtr = blockHeight - 1;
		//			numberOfLines = blockHeight;
		//		}
		//	}
		//	else
		//	{
		//		// We are displaying a map section originally generated for an area with a positive Y values, in an area with negative Y values.
		//		// The content of any given block have either all postive or all negative Y values.
		//		// The content of these blocks must not be reversed

		//		// The first row from counts is at the top of the image.
		//		// The last row from counts is at the bottom of the image.

		//		// The startingLinePtr starts small and is increased by numberOfLines

		//		if (blockPtr == 0)
		//		{
		//			// This is the block with the smallest Map Coordinate and the largest Y screen coordinate (aka the first block)

		//			startingLinePtr = 0;
		//			numberOfLines = heightOfFirstBlock;
		//		}
		//		else if (blockPtr == extentInBlocksY - 1)
		//		{
		//			// This is the block with the largest Map Coordinate and the smallest Y screen coordinate, (aka the last block)

		//			startingLinePtr = blockHeight - heightOfLastBlock;
		//			numberOfLines = heightOfLastBlock;
		//		}
		//		else
		//		{
		//			startingLinePtr = 0;
		//			numberOfLines = blockHeight;
		//		}
		//	}


		//	var lineIncrement = invert ? -1 : 1;

		//	return (startingLinePtr, numberOfLines, lineIncrement);
		//}

		//// GetSegmentLengths
		//private ValueTuple<int, int>[] GetSegmentLengths(int numberOfWholeBlocksX, int widthOfFirstBlock, int widthOfLastBlock, int blockWidth)
		//{
		//	var segmentLengths = new ValueTuple<int, int>[numberOfWholeBlocksX];

		//	for (var blockPtrX = 0; blockPtrX < numberOfWholeBlocksX; blockPtrX++)
		//	{
		//		int lineLength;
		//		int samplesToSkip;

		//		if (blockPtrX == 0)
		//		{
		//			samplesToSkip = blockWidth - widthOfFirstBlock;
		//			lineLength = widthOfFirstBlock;
		//		}
		//		else if (blockPtrX == numberOfWholeBlocksX - 1)
		//		{
		//			samplesToSkip = 0;
		//			lineLength = widthOfLastBlock; 
		//		}
		//		else
		//		{
		//			samplesToSkip = 0;
		//			lineLength = blockWidth;
		//		}

		//		segmentLengths[blockPtrX] = new ValueTuple<int, int>(lineLength, samplesToSkip);
		//	}

		//	return segmentLengths;
		//}

		#endregion
	}
}


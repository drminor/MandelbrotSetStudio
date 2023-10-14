using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public class BitmapBuilder : IBitmapBuilder
	{
		#region Private Fields

		//private const double VALUE_FACTOR = 10000;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private AsyncManualResetEvent _blocksForRowAreReady;
		private int? _currentJobNumber;
		private IDictionary<int, MapSection?>? _currentResponses;
		private bool _isStopping;

		#endregion

		#region Constructor

		public BitmapBuilder(IMapLoaderManager mapLoaderManager)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionBuilder = new MapSectionBuilder();

			_blocksForRowAreReady = new AsyncManualResetEvent();
			_currentJobNumber = null;
			_currentResponses = null;
			_isStopping = false;
		}

		#endregion

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public async Task<byte[]?> BuildAsync(ObjectId jobId, OwnerType ownerType, MapAreaInfo mapAreaInfo, ColorBandSet colorBandSet, bool useEscapeVelocities, MapCalcSettings mapCalcSettings, 
			CancellationToken ct, Action<double>? statusCallback = null)
		{
			var mapBlockOffset = mapAreaInfo.MapBlockOffset;
			var canvasControlOffset = mapAreaInfo.CanvasControlOffset;

			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			var msrJob = _mapLoaderManager.CreateMapSectionRequestJob(JobType.Image, jobId.ToString(), ownerType, mapAreaInfo, mapCalcSettings);				

			var imageSize = mapAreaInfo.CanvasSize.Round();

			var result = new byte[imageSize.NumberOfCells * 4];

			try
			{
				//var numberOfWholeBlocks = RMapHelper.GetMapExtentInBlocks(imageSize, canvasControlOffset, blockSize);
				//var w = numberOfWholeBlocks.Width;
				//var h = numberOfWholeBlocks.Height;

				var extentInBlocks = RMapHelper.GetMapExtentInBlocks(imageSize, canvasControlOffset, blockSize, out var sizeOfFirstBlock, out var sizeOfLastBlock);
				var stride = extentInBlocks.Width;
				var h = extentInBlocks.Height;

				Debug.WriteLine($"The BitmapBuilder is processing section requests. The map extent is {extentInBlocks}. The ColorMap has Id: {colorBandSet.Id}.");

				var segmentLengths = BitmapHelper.GetSegmentLengths(extentInBlocks.Width, sizeOfFirstBlock.Width, sizeOfLastBlock.Width, blockSize.Width);

				var destPixPtr = 0;

				for (var blockPtrY = h - 1; blockPtrY >= 0 && !ct.IsCancellationRequested; blockPtrY--)
				{
					var blockIndexY = blockPtrY - (h / 2);

					var msrSubJob = _mapLoaderManager.CreateNewCopy(msrJob); // Each row must use a fresh MsrJob.

					var blocksForThisRow = await GetAllBlocksForRowAsync(msrSubJob, blockPtrY, blockIndexY, stride, ct);

					if (ct.IsCancellationRequested || blocksForThisRow.Count == 0)
					{
						return null;
					}

					//var checkCnt = blocksForThisRow.Count;
					//Debug.Assert(checkCnt == w);

					Debug.Assert(blocksForThisRow.Count == extentInBlocks.Width);

					// An Inverted MapSection should be processed from first to last instead of as we do normally from last to first.

					// MapSection.IsInverted indicates that the MapSection was generated using postive y coordinates, but in this case, the mirror image is being displayed.
					// Normally we must process the contents of the MapSection from last Y to first Y because the Map coordinates increase from the bottom of the display to the top of the display
					// But the screen coordinates increase from the top of the display to be bottom.
					// We set the invert flag to indicate that the contents should be processed from last y to first y to compensate for the Map/Screen direction difference.

					var invert = !blocksForThisRow[0]?.IsInverted ?? false; // Invert the coordinates if the MapSection is not Inverted. Do not invert if the MapSection is inverted.

					var (startingLinePtr, numberOfLines, lineIncrement) = BitmapHelper.GetNumberOfLines(blockPtrY, invert, extentInBlocks.Height, sizeOfFirstBlock.Height, sizeOfLastBlock.Height, blockSize.Height);

					destPixPtr = BuildARow(result, destPixPtr, blockPtrY, invert, startingLinePtr, numberOfLines, lineIncrement, extentInBlocks.Width, blocksForThisRow, segmentLengths, colorMap, blockSize.Width, ct);

					var percentageCompleted = (h - blockPtrY) / (double)h;
					statusCallback?.Invoke(100 * percentageCompleted);
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

		#endregion

		#region Private Methods

		private int BuildARow(byte[] result, int destPixPtr, int blockPtrY, bool isInverted, int startingPtr, int numberOfLines, int increment, int extentInBlocksWidth,
			IDictionary<int, MapSection?> blocksForThisRow, ValueTuple<int, int>[] segmentLengths, ColorMap colorMap, int blockSizeWidth, CancellationToken ct)
		{
			var linePtr = startingPtr;
			for (var cntr = 0; cntr < numberOfLines; cntr++)
			{
				for (var blockPtrX = 0; blockPtrX < extentInBlocksWidth; blockPtrX++)
				{
					var mapSection = blocksForThisRow[blockPtrX];
					var invertThisBlock = !mapSection?.IsInverted ?? false;

					Debug.Assert(invertThisBlock == isInverted, $"The block at {blockPtrX}, {blockPtrY} has a differnt value of isInverted as does the block at 0, {blockPtrY}.");

					var countsForThisLine = BitmapHelper.GetOneLineFromCountsBlock(mapSection?.MapSectionVectors?.Counts, linePtr, blockSizeWidth);
					//var escVelsForThisLine = new ushort[countsForThisLine?.Length ?? 0];
					var escVelsForThisLine = BitmapHelper.GetOneLineFromCountsBlock(mapSection?.MapSectionVectors?.EscapeVelocities, linePtr, blockSizeWidth);

					var lineLength = segmentLengths[blockPtrX].Item1;
					var samplesToSkip = segmentLengths[blockPtrX].Item2;

					try
					{
						BitmapHelper.FillImageLineSegment(result, destPixPtr, countsForThisLine, escVelsForThisLine, lineLength, samplesToSkip, colorMap);

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

				linePtr += increment;
			}

			return destPixPtr;
		}

		private async Task<IDictionary<int, MapSection?>> GetAllBlocksForRowAsync(MsrJob msrJob, int rowPtr, int blockIndexY, int stride, CancellationToken ct)
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

			_currentResponses = new Dictionary<int, MapSection?>();

			try
			{
				Debug.WriteLine("Resetting the Async Manual Reset Event.");
				_blocksForRowAreReady.Reset();

				Debug.WriteLine("Pushing a new request.");

				var mapSections = _mapLoaderManager.Push(msrJob, requests, MapSectionReady, MapViewUpdateIsComplete, ct, out var _);
				_currentJobNumber = msrJob.MapLoaderJobNumber;

				foreach (var response in mapSections)
				{
					_currentResponses.Add(response.ScreenPosition.X, response);
				}

				Debug.WriteLine($"Beginning to Wait for the blocks. Job#: {msrJob.MapLoaderJobNumber}");
				await _blocksForRowAreReady.WaitAsync();
				Debug.WriteLine($"Completed Waiting for the blocks. Job#: {msrJob.MapLoaderJobNumber}. {_currentResponses.Count} blocks were created.");
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

		private void MapViewUpdateIsComplete(int jobNumber, bool isCancelled)
		{
			Debug.WriteLine($"MapViewUpdateIsComplete callback is being called. JobNumber: {jobNumber}, Cancelled = {isCancelled}.");

			_blocksForRowAreReady.SetAsync();
		}

		#endregion
	}
}


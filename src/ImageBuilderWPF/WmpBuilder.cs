using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ImageBuilderWPF
{
	public class WmpBuilder : IImageBuilder
	{
		#region Private Fields

		private const double VALUE_FACTOR = 10000;
		private const int BYTES_PER_PIXEL = 4;

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private AsyncManualResetEvent _blocksForRowAreReady;
		private int? _currentJobNumber;
		private IDictionary<int, MapSection>? _mapSectionsForRow;
		private int _blocksPerRow;
		private bool _isStopping;

		private readonly bool _useDetailedDebug = true;

		#endregion

		#region Constructor

		public WmpBuilder(IMapLoaderManager mapLoaderManager)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionBuilder = new MapSectionBuilder();

			_blocksForRowAreReady = new AsyncManualResetEvent();
			_currentJobNumber = null;
			_mapSectionsForRow = null;
			_blocksPerRow = -1;
			_isStopping = false;
		}

		#endregion

		#region Public Properties

		public long NumberOfCountValSwitches { get; private set; }

		#endregion

		#region Public Methods

		public async Task<bool> BuildAsync(string imageFilePath, ObjectId jobId, OwnerType ownerType, MapPositionSizeAndDelta mapAreaInfo, ColorBandSet colorBandSet, bool useEscapeVelocities, MapCalcSettings mapCalcSettings,
			Action<double> statusCallback, CancellationToken ct, SynchronizationContext synchronizationContext)
		{
			var result = true;


			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			WmpImage? wmpImage = null;

			try
			{
				var outputStream = File.Open(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

				var imageSize = mapAreaInfo.CanvasSize.Round();
				wmpImage = new WmpImage(outputStream, imageFilePath, imageSize.Width, imageSize.Height, synchronizationContext);

				var msrJob = _mapLoaderManager.CreateMapSectionRequestJob(JobType.Image, jobId.ToString(), ownerType, mapAreaInfo, mapCalcSettings);
				var canvasControlOffset = mapAreaInfo.CanvasControlOffset;
				var mapExtent = RMapHelper.GetMapExtent(imageSize, canvasControlOffset, blockSize);
				_blocksPerRow = mapExtent.Width;
				var h = mapExtent.Height;
				var maxBlockYPtr = h - 1;

				Debug.WriteLineIf(_useDetailedDebug, $"The WmpBuilder is processing section requests. The map extent is {mapExtent.Extent}. The ColorMap has Id: {colorBandSet.Id}.");

				var segmentLengths = RMapHelper.GetSegmentLengths(mapExtent);

				for (var blockPtrY = h - 2; blockPtrY >= 1 && !ct.IsCancellationRequested; blockPtrY--)
				{
					// Get all of the blocks for this row.
					var blockIndexY = blockPtrY - (h / 2);
					var msrSubJob = _mapLoaderManager.CreateNewCopy(msrJob); // Each row must use a fresh MsrJob.
					var blocksForThisRow = await GetAllBlocksForRowAsync(msrSubJob, blockPtrY, blockIndexY, _blocksPerRow, ct);

					if (ct.IsCancellationRequested || msrSubJob.IsCancelled || blocksForThisRow.Count == 0)
					{
						result = false;
						break;
					}

					// An Inverted MapSection should be processed from first to last instead of as we do normally from last to first.

					// MapSection.IsInverted indicates that the MapSection was generated using postive y coordinates, but in this case, the mirror image is being displayed.
					// Normally we must process the contents of the MapSection from last Y to first Y because the Map coordinates increase from the bottom of the display to the top of the display
					// But the screen coordinates increase from the top of the display to be bottom.
					// We set the invert flag to indicate that the contents should be processed from last y to first y to compensate for the Map/Screen direction difference.

					var invert = !blocksForThisRow[0]?.IsInverted ?? false; // Invert the coordinates if the MapSection is not Inverted. Do not invert if the MapSection is inverted.

					// Calculate the number of lines used for this row of blocks
					var (startingLinePtr, numberOfLines, lineIncrement) = RMapHelper.GetNumberOfLines(blockPtrY, invert, mapExtent);

					// Calculate the pixel values and write them to the image file.
					var invertedBlockPtrY = maxBlockYPtr - blockPtrY;
					BuildARow(wmpImage, invertedBlockPtrY, invert, startingLinePtr, numberOfLines, lineIncrement, blocksForThisRow, segmentLengths, colorMap, blockSize.Width, ct);

					var percentageCompleted = (h - blockPtrY) / (double)h;

					statusCallback(100 * percentageCompleted);
				}
			}
			catch (Exception e)
			{
				if (!ct.IsCancellationRequested)
				{
					await Task.Delay(10);
					Debug.WriteLine($"WmpBuilder encountered an exception: {e}.");
					throw;
				}
			}
			finally
			{
				if (!ct.IsCancellationRequested)
				{
					wmpImage?.End();
				}
				else
				{
					wmpImage?.Abort();
				}
			}

			return result;
		}

		#endregion

		#region Private Methods

		private void BuildARow(WmpImage wmpImage, int blockPtrY, bool isInverted, int startingPtr, int numberOfLines, int increment, 
			IDictionary<int, MapSection> blocksForThisRow, ValueTuple<int, int>[] segmentLengths, ColorMap colorMap, int blockSizeWidth, CancellationToken ct)
		{
			for (var blockPtrX = 1; blockPtrX < blocksForThisRow.Count - 1; blockPtrX++)
			{
				var mapSection = blocksForThisRow[blockPtrX];
				var invertThisBlock = !mapSection.IsInverted;

				Debug.Assert(invertThisBlock == isInverted, $"The block at {blockPtrX}, {blockPtrY} has a different value of isInverted as does the block at 0, {blockPtrY}.");

				//var countsForThisLine = mapSection.GetOneLineFromCountsBlock(linePtr);
				//var escVelsForThisLine = mapSection.GetOneLineFromEscapeVelocitiesBlock(linePtr);
				//var escVelsForThisLine = new ushort[countsForThisLine?.Length ?? 0];

				//var lineLength = segmentLengths[blockPtrX].Item1;
				//var samplesToSkip = segmentLengths[blockPtrX].Item2;

				if (mapSection.MapSectionVectors != null)
				{
					LoadPixelArray(mapSection.MapSectionVectors, colorMap, invertThisBlock);
					var sourceRect = new Int32Rect(0, 0, 128, 128);
					var sourceStride = blockSizeWidth * BYTES_PER_PIXEL;

					try
					{
						wmpImage.WriteBlock(sourceRect, mapSection.MapSectionVectors, sourceStride, blockPtrX * 128, blockPtrY * 128);
						mapSection.MapSectionVectors.DecreaseRefCount();
					}
					catch (Exception e)
					{
						if (!ct.IsCancellationRequested)
						{
							Debug.WriteLine($"Got exception: {e} from wmpImage.WriteBlock.");
							throw;
						}
					}
				}

			}
		}

		private async Task<IDictionary<int, MapSection>> GetAllBlocksForRowAsync(MsrJob msrJob, int rowPtr, int blockIndexY, int stride, CancellationToken ct)
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

			Debug.WriteLineIf(_useDetailedDebug, "Resetting the Async Manual Reset Event.");
			_blocksForRowAreReady.Reset();
			_mapSectionsForRow = new Dictionary<int, MapSection>();

			try
			{
				Debug.WriteLineIf(_useDetailedDebug, "Pushing a new request.");
				var mapSections = _mapLoaderManager.Push(msrJob, requests, MapSectionReady, MapViewUpdateIsComplete, ct, out var requestsPendingGeneration);
				_currentJobNumber = msrJob.MapLoaderJobNumber;

				foreach (var mapSection in mapSections)
				{
					_mapSectionsForRow.Add(mapSection.ScreenPosition.X, mapSection);
					mapSection.MapSectionVectors?.IncreaseRefCount();
				}

				if (_mapSectionsForRow.Count != _blocksPerRow)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"Beginning to Wait for the blocks. Job#: {msrJob.MapLoaderJobNumber}");
					await _blocksForRowAreReady.WaitAsync();
				}

				if (ct.IsCancellationRequested || msrJob.IsCancelled)
				{
					_mapSectionsForRow.Clear();
				}
			}
			catch
			{
				_isStopping = true;
			}

			if (_isStopping)
			{
				_mapSectionsForRow.Clear();
			}
			Debug.WriteLineIf(_useDetailedDebug, $"WmpBuilder: Completed Waiting for the blocks. Job#: {msrJob.MapLoaderJobNumber}. {_mapSectionsForRow.Count} blocks were received.");

			return _mapSectionsForRow;
		}

		private void MapSectionReady(MapSection mapSection)
		{
			if (_mapSectionsForRow == null)
			{
				return;
			}

			if (mapSection.JobNumber == _currentJobNumber)
			{
				if (!mapSection.IsEmpty)
				{
					_mapSectionsForRow.Add(mapSection.ScreenPosition.X, mapSection);
					mapSection.MapSectionVectors?.IncreaseRefCount();

					if (_mapSectionsForRow.Count == _blocksPerRow)
					{
						// We now have received the full row.
						_blocksForRowAreReady.SetAsync();
					}
				}
				else
				{
					Debug.WriteLine($"WmpBuilder recieved an empty MapSection. Job Number: {mapSection.JobNumber}.");
				}
			}
		}

		private void MapViewUpdateIsComplete(int jobNumber, bool isCancelled)
		{
			Debug.WriteLineIf(_useDetailedDebug, $"MapViewUpdateIsComplete callback is being called. JobNumber: {jobNumber}, Cancelled = {isCancelled}.");

			if (isCancelled)
			{
				_blocksForRowAreReady.SetAsync();
			}
		}

		#endregion

		#region Pixel Array Support

		private long LoadPixelArray(MapSectionVectors mapSectionVectors, ColorMap colorMap, bool invert)
		{
			Debug.Assert(mapSectionVectors.ReferenceCount > 0, "Getting the Pixel Array from a MapSectionVectors whose RefCount is < 1.");

			var errors = 0L;
			var useEscapeVelocities = colorMap.UseEscapeVelocities;

			var rowCount = mapSectionVectors.BlockSize.Height;
			var colCount = mapSectionVectors.BlockSize.Width;
			var maxRowIndex = rowCount - 1;

			var pixelStride = colCount * BYTES_PER_PIXEL;

			var backBuffer = mapSectionVectors.BackBuffer;

			Debug.Assert(backBuffer.Length == mapSectionVectors.BlockSize.NumberOfCells * BYTES_PER_PIXEL);

			var counts = mapSectionVectors.Counts;
			var previousCountVal = counts[0];

			var resultRowPtr = invert ? maxRowIndex * pixelStride : 0;
			var resultRowPtrIncrement = invert ? -1 * pixelStride : pixelStride;
			var sourcePtrUpperBound = rowCount * colCount;

			if (useEscapeVelocities)
			{
				var escapeVelocities = mapSectionVectors.EscapeVelocities;

				CheckMissingEscapeVelocities(escapeVelocities);

				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < colCount; colPtr++)
					{
						var countVal = counts[sourcePtr];
						//TrackValueSwitches(countVal, ref previousCountVal);

						var escapeVelocity = escapeVelocities[sourcePtr] / VALUE_FACTOR;
						CheckEscapeVelocity(escapeVelocity);

						var destination = new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL);
						errors += colorMap.PlaceColor(countVal, escapeVelocity, destination);

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}
			else
			{
				// The main for loop on GetPixel Array 
				// is for each row of pixels (0 -> 128)
				//		for each pixel in that row (0, -> 128)
				// each new row advanced the resultRowPtr to the pixel byte address at column 0 of the current row.
				// if inverted, the first row = 127 * # of bytes / Row (Pixel stride)

				for (var sourcePtr = 0; sourcePtr < sourcePtrUpperBound; resultRowPtr += resultRowPtrIncrement)
				{
					var resultPtr = resultRowPtr;
					for (var colPtr = 0; colPtr < colCount; colPtr++)
					{
						var countVal = counts[sourcePtr];
						TrackValueSwitches(countVal, ref previousCountVal);

						var destination = new Span<byte>(backBuffer, resultPtr, BYTES_PER_PIXEL);
						errors += colorMap.PlaceColor(countVal, escapeVelocity: 0, destination);

						resultPtr += BYTES_PER_PIXEL;
						sourcePtr++;
					}
				}
			}

			mapSectionVectors.BackBufferIsLoaded = true;

			return errors;
		}

		[Conditional("DEBUG2")]
		private void TrackValueSwitches(ushort countVal, ref ushort previousCountVal)
		{
			if (countVal != previousCountVal)
			{
				NumberOfCountValSwitches++;
				previousCountVal = countVal;
			}
		}

		[Conditional("DEBUG2")]
		private void CheckEscapeVelocity(double escapeVelocity)
		{
			if (escapeVelocity > 1.0)
			{
				Debug.WriteLine($"WARNING: The Escape Velocity is greater than 1.0");
			}
		}

		[Conditional("DEBUG2")]
		private void CheckMissingEscapeVelocities(ushort[] escapeVelocities)
		{
			if (!escapeVelocities.Any(x => x > 0))
			{
				Debug.WriteLine("No EscapeVelocities Found.");
			}
		}


		#endregion
	}

}

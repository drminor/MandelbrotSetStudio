using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly MapSectionBuilder _mapSectionBuilder;
		private readonly BitmapHelper _bitmapHelper;

		private AsyncManualResetEvent _blocksForRowAreReady;
		private int? _currentJobNumber;
		private IDictionary<int, MapSection>? _mapSectionsForRow;
		private int _blocksPerRow;
		private bool _isStopping;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public WmpBuilder(IMapLoaderManager mapLoaderManager, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_mapSectionBuilder = new MapSectionBuilder();
			_bitmapHelper = new BitmapHelper();

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

		public async Task<bool> BuildAsync(string imageFilePath, string jobId, OwnerType ownerType, MapPositionSizeAndDelta mapAreaInfo, ColorBandSet colorBandSet, bool useEscapeVelocities, MapCalcSettings mapCalcSettings,
			Action<double> statusCallback, CancellationToken ct, SynchronizationContext synchronizationContext)
		{
			var result = true;

			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			//var outputStream = File.Open(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
			var imageSize = mapAreaInfo.CanvasSize.Round();
			var wmpImage = new WmpImage(imageFilePath, imageSize.Width, imageSize.Height, synchronizationContext, _mapSectionVectorProvider);

			try
			{
				var msrJob = _mapLoaderManager.CreateMapSectionRequestJob(JobType.Image, jobId, ownerType, mapAreaInfo, mapCalcSettings);
				
				var canvasControlOffset = mapAreaInfo.CanvasControlOffset;
				var mapExtent = RMapHelper.GetMapExtent(imageSize, canvasControlOffset, blockSize);
				_blocksPerRow = mapExtent.Width;
				var h = mapExtent.Height;

				Debug.WriteLineIf(_useDetailedDebug, $"The WmpBuilder is processing section requests. The map extent is {mapExtent.Extent}. The ColorMap has Id: {colorBandSet.Id}.");

				var segmentLengths = RMapHelper.GetHorizontalIntraBlockOffsets(mapExtent);

				// Process rows using the map coordinates, from the highest Y coordinate to the lowest coordinate.
				for (var blockPtrY = h - 1; blockPtrY >= 0 && !ct.IsCancellationRequested; blockPtrY--)
				{
					// Get all of the blocks for this row.
					// Each row must use a fresh MsrJob.
					var msrSubJob = _mapLoaderManager.CreateNewCopy(msrJob);
					var blockIndexY = blockPtrY - (h / 2);
					var blocksForThisRow = await GetAllBlocksForRowAsync(msrSubJob, blockPtrY, blockIndexY, _blocksPerRow, ct);

					if (ct.IsCancellationRequested || msrSubJob.IsCancelled || blocksForThisRow.Count == 0)
					{
						result = false;
						break;
					}

					// Calculate the pixel values and write them to the image file.
					BuildARow(wmpImage, blockPtrY, blocksForThisRow, colorMap, segmentLengths, mapExtent, ct);

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

		private void BuildARow(WmpImage wmpImage, int blockPtrY, IDictionary<int, MapSection> blocksForThisRow, ColorMap colorMap, ValueTuple<int, int>[] segmentLengths, MapExtent mapExtent, CancellationToken ct)
		{
			// Blocks with a negative Y map coordinate are drawn up-side, down.
			//bool drawInverted = GetDrawInverted(blocksForThisRow[0]);

			var widthOfFirstBlock = mapExtent.SizeOfFirstBlock.Width;
			var heightOfLastBlock = mapExtent.SizeOfLastBlock.Height;

			// The destination Y location is measured from the top of the image -- opposite of the map coordinate which is measured from the bottom of the image.
			var maxBlockYPtr = mapExtent.Height - 1;
			var invertedBlockPtrY = maxBlockYPtr - blockPtrY;
			var yLoc = invertedBlockPtrY == 0 ? 0 : heightOfLastBlock + ((invertedBlockPtrY - 1) * 128);

			// Calculate the number of lines used for this row of blocks
			var (startingLinePtr, numberOfLines) = RMapHelper.GetVerticalPixelBufferOffsets(blockPtrY, mapExtent);

			Debug.WriteLineIf(_useDetailedDebug, $"BlockYPtr: {blockPtrY}, InvertedBlockYPtr: {invertedBlockPtrY}, yLoc: {yLoc}, startingLinePtr: {startingLinePtr}, NumberOfLines: {numberOfLines}.");

			for (var blockPtrX = 1; blockPtrX < blocksForThisRow.Count - 1; blockPtrX++)
			{
				var mapSection = blocksForThisRow[blockPtrX];

				if (mapSection.MapSectionVectors == null)
					continue;

				//Debug.Assert(!mapSection.IsInverted == drawInverted, $"The block at {blockPtrX}, {blockPtrY} has a different value of isInverted as does the block at 0, {blockPtrY}.");
				_bitmapHelper.LoadPixelArray(mapSection, colorMap, mapSection.MapSectionVectors.BackBuffer);

				var (samplesToSkip, lineLength) = segmentLengths[blockPtrX];
				var sourceRect = new Int32Rect(samplesToSkip, startingLinePtr, lineLength, numberOfLines);

				var sourceStride = mapExtent.BlockSize.Width * BYTES_PER_PIXEL;

				var xLoc = blockPtrX == 0 ? 0 : widthOfFirstBlock + ((blockPtrX - 1) * 128);

				wmpImage.WriteBlock(sourceRect, mapSection.MapSectionVectors, sourceStride, xLoc, yLoc);
			}
		}

		//private bool GetDrawInverted(MapSection mapSection)
		//{
		//	// An Inverted MapSection should be processed from first to last instead of as we do normally from last to first.

		//	// MapSection.IsInverted indicates that the MapSection was generated using postive y coordinates, but in this case, the mirror image is being displayed.
		//	// Normally we must process the contents of the MapSection from last Y to first Y because the Map coordinates increase from the bottom of the display to the top of the display
		//	// But the screen coordinates increase from the top of the display to be bottom.
		//	// We set the invert flag to indicate that the contents should be processed from last y to first y to compensate for the Map/Screen direction difference.

		//	var result = !mapSection.IsInverted; // Invert the coordinates if the MapSection is not Inverted. Do not invert if the MapSection is inverted.
		//	return result;
		//}

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
					foreach (var ms in _mapSectionsForRow.Values)
					{
						_mapSectionVectorProvider.ReturnToPool(ms);
					}
					_mapSectionsForRow.Clear();
				}
			}
			catch
			{
				_isStopping = true;
			}

			if (_isStopping)
			{
				foreach (var ms in _mapSectionsForRow.Values)
				{
					_mapSectionVectorProvider.ReturnToPool(ms);
				}
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
	}

}

using MongoDB.Bson;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImageBuilder
{
	public class PngBuilder : IImageBuilder
	{
		#region Private Fields

		private readonly IMapLoaderManager _mapLoaderManager;
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly MapSectionBuilder _mapSectionBuilder;

		private AsyncManualResetEvent _blocksForRowAreReady;
		private int? _currentJobNumber;
		private IDictionary<int, MapSection>? _mapSectionsForRow;
		private int _blocksPerRow;
		private bool _isStopping;

		private readonly bool _useDetailedDebug = false;

		#endregion

		#region Constructor

		public PngBuilder(IMapLoaderManager mapLoaderManager, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_mapLoaderManager = mapLoaderManager;
			_mapSectionVectorProvider = mapSectionVectorProvider;
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

		public async Task<bool> BuildAsync(string imageFilePath, ObjectId jobId, OwnerType ownerType, MapPositionSizeAndDelta mapAreaInfo, ColorBandSet colorBandSet, MapCalcSettings mapCalcSettings, bool useEscapeVelocities,
			CancellationToken ct/*, SynchronizationContext _*/, Action<double> statusCallback)
		{
			var result = true;

			var blockSize = mapAreaInfo.Subdivision.BlockSize;
			var colorMap = new ColorMap(colorBandSet)
			{
				UseEscapeVelocities = useEscapeVelocities
			};

			PngImage? pngImage = null;

			try
			{
				var stream = File.Open(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);

				var msrJob = _mapLoaderManager.CreateMapSectionRequestJob(JobType.Image, jobId, ownerType, mapAreaInfo, mapCalcSettings);

				var imageSize = mapAreaInfo.CanvasSize.Round();
				pngImage = new PngImage(stream, imageFilePath, imageSize.Width, imageSize.Height);

				var canvasControlOffset = mapAreaInfo.CanvasControlOffset;
				var mapExtent = RMapHelper.GetMapExtent(imageSize, canvasControlOffset, blockSize);
				_blocksPerRow = mapExtent.Width;
				var h = mapExtent.Height;

				Debug.WriteLineIf(_useDetailedDebug, $"The PngBuilder is processing section requests. The map extent is {mapExtent.Extent}. The ColorMap has Id: {colorBandSet.Id}.");

				var segmentLengths = RMapHelper.GetHorizontalIntraBlockOffsets(mapExtent);

				for (var blockPtrY = h - 1; blockPtrY >= 0 && !ct.IsCancellationRequested; blockPtrY--)
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

					BuildARow(pngImage, blockPtrY, blocksForThisRow, colorMap, segmentLengths, mapExtent, ct);

					foreach (var ms in blocksForThisRow.Values)
					{
						_mapSectionVectorProvider.ReturnToPool(ms);
					}

					var percentageCompleted = (h - blockPtrY) / (double)h;

					statusCallback(100 * percentageCompleted);
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

			return result;
		}

		#endregion

		#region Private Methods

		private void BuildARow(PngImage pngImage, int blockPtrY, IDictionary<int, MapSection> blocksForThisRow, ColorMap colorMap, ValueTuple<int, int>[] segmentLengths, MapExtent mapExtent, CancellationToken ct)
		{
			// Blocks with a negative Y map coordinate are drawn up-side, down.
			bool drawInverted = GetDrawInverted(blocksForThisRow[0]);

			// Calculate the number of lines used for this row of blocks
			var (startingLinePtr, numberOfLines, lineIncrement) = RMapHelper.GetVerticalIntraBlockOffsets(blockPtrY, drawInverted, mapExtent);

			var linePtr = startingLinePtr;
			for (var cntr = 0; cntr < numberOfLines && !ct.IsCancellationRequested; cntr++)
			{
				var iLine = pngImage.ImageLine;
				var destPixPtr = 0;

				for (var blockPtrX = 1; blockPtrX < blocksForThisRow.Count - 1; blockPtrX++)
				{
					var mapSection = blocksForThisRow[blockPtrX];
					var invertThisBlock = !mapSection.IsInverted;

					Debug.Assert(invertThisBlock == drawInverted, $"The block at {blockPtrX}, {blockPtrY} has a different value of isInverted as does the block at 0, {blockPtrY}.");

					var countsForThisLine = mapSection.GetOneLineFromCountsBlock(linePtr);
					var escVelsForThisLine = mapSection.GetOneLineFromEscapeVelocitiesBlock(linePtr); // TODO: Avoid fetching the EscapeVelocities from the MapSectionVectors if UseEscapeVelocities = false.

					var (samplesToSkip, lineLength) = segmentLengths[blockPtrX];

					try
					{
						PngImage.FillPngImageLineSegment(iLine, destPixPtr, countsForThisLine, escVelsForThisLine, lineLength, samplesToSkip, colorMap);
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

				linePtr += lineIncrement;
			}
		}

		private bool GetDrawInverted(MapSection mapSection)
		{
			// An Inverted MapSection should be processed from first to last instead of as we do normally from last to first.

			// MapSection.IsInverted indicates that the MapSection was generated using postive y coordinates, but in this case, the mirror image is being displayed.
			// Normally we must process the contents of the MapSection from last Y to first Y because the Map coordinates increase from the bottom of the display to the top of the display
			// But the screen coordinates increase from the top of the display to be bottom.
			// We set the invert flag to indicate that the contents should be processed from last y to first y to compensate for the Map/Screen direction difference.

			var result = !mapSection.IsInverted; // Invert the coordinates if the MapSection is not Inverted. Do not invert if the MapSection is inverted.
			return result;
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
				_mapSectionsForRow.Clear();
			}

			Debug.WriteLineIf(_useDetailedDebug, $"PngBuilder: Completed Waiting for the blocks. Job#: {msrJob.MapLoaderJobNumber}. {_mapSectionsForRow.Count} blocks were received.");

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

					if (_mapSectionsForRow.Count == _blocksPerRow)
					{
						// We now have received the full row.
						_blocksForRowAreReady.SetAsync();
					}
				}
				else
				{
					Debug.WriteLine($"Bitmap Builder recieved an empty MapSection. Job Number: {mapSection.JobNumber}.");
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


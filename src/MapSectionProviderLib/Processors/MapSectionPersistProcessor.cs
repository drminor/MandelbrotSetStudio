using MongoDB.Bson;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionPersistProcessor : IDisposable
	{
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private const int QUEUE_CAPACITY = 200;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionPersistRequest> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;

		private readonly object _queueLock = new();

		#region Constructor

		public MapSectionPersistProcessor(IMapSectionAdapter mapSectionAdapter, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_mapSectionAdapter = mapSectionAdapter;
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSectionPersistRequest>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
		}

		#endregion

		#region Public Methods

		internal void AddWork(MapSectionPersistRequest mapSectionWorkItem, CancellationToken ct)
		{
			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionWorkItem, ct);
			}
			else
			{
				Debug.WriteLine($"Not adding: {mapSectionWorkItem.Request}, The MapSectionPersistProcessor's WorkQueue IsAddingComplete has been set.");
			}
		}

		public void Stop(bool immediately)
		{
			lock (_queueLock)
			{
				if (immediately)
				{
					_cts.Cancel();
				}
				else
				{
					if (!_workQueue.IsCompleted && !_workQueue.IsAddingCompleted)
					{
						_workQueue.CompleteAdding();
					}
				}
			}

			try
			{
				if (_workQueueProcessor.Wait(RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS * 1000))
				{
					Debug.WriteLine("The MapSectionPersistProcesssor's WorkQueueProcessor Task has completed.");
				}
				else
				{
					Debug.WriteLine($"The MapSectionPersistProcesssor's WorkQueueProcessor Task did not complete after waiting for {RMapConstants.MAP_SECTION_PROCESSOR_STOP_TIMEOUT_SECONDS} seconds.");
				}
			}
			catch
			{ }
		}

		#endregion

		#region Private Methods

		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
			while (!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionPersistRequest = _workQueue.Take(ct);

					var mapSectionRequest = mapSectionPersistRequest.Request;
					var mapSectionResponse = mapSectionPersistRequest.Response;

					if (mapSectionPersistRequest.OnlyInsertJobMapSectionRecord)
					{
						_ = await SaveJobMapSection(mapSectionRequest, mapSectionResponse);
					}
					else
					{
						if (mapSectionResponse.MapSectionVectors != null)
						{
							await PersistTheCountsAndZValuesAsync(mapSectionRequest, mapSectionResponse);
						}
						else
						{
							Debug.WriteLine($"The MapSectionPersist Processor received an empty MapSectionResponse.");
						}
					}

					_mapSectionVectorProvider.ReturnMapSectionResponse(mapSectionResponse);
				}
				catch (OperationCanceledException)
				{
					//Debug.WriteLine("The persist queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The persist queue got an exception: {e}.");
					Console.WriteLine($"\n\nWARNING:The persist queue got an exception: {e}.\n\n");
					//throw;
				}
			}
		}

		private async Task PersistTheCountsAndZValuesAsync(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			if (mapSectionResponse.RecordOnFile)
			{
				var mapSectionId = new ObjectId(mapSectionResponse.MapSectionId!);
				Debug.WriteLine($"Updating Z Values for {mapSectionResponse.MapSectionId}, bp: {mapSectionResponse.BlockPosition}.");

				_ = await _mapSectionAdapter.UpdateCountValuesAync(mapSectionResponse);

				if (mapSectionResponse.MapSectionZVectors != null)
				{
					if (mapSectionResponse.AllRowsHaveEscaped)
					{
						_ = await _mapSectionAdapter.DeleteZValuesAync(mapSectionId);
					}
					else
					{
						_ = await _mapSectionAdapter.UpdateZValuesAync(mapSectionResponse, mapSectionId);
					}

					// A JobMapSectionRecord (identified by the triplet of mapSectionId, ownerId and jobOwnerType) may not be on file.
					// This will insert one if not already present.

					_ = await SaveJobMapSection(mapSectionRequest, mapSectionResponse);
				}
			}
			else
			{
				//Debug.WriteLine($"Creating MapSection for {mapSectionResponse.MapSectionId}, bp: {mapSectionResponse.BlockPosition}.");
				var mapSectionId = await _mapSectionAdapter.SaveMapSectionAsync(mapSectionResponse);

				if (mapSectionId.HasValue)
				{
					mapSectionResponse.MapSectionId = mapSectionId.ToString();

					if (mapSectionResponse.MapSectionZVectors != null && !mapSectionResponse.AllRowsHaveEscaped)
					{
						_ = await _mapSectionAdapter.SaveMapSectionZValuesAsync(mapSectionResponse, mapSectionId.Value);
					}

					_ = await SaveJobMapSection(mapSectionRequest, mapSectionResponse);
				}
			}
		}

		private Task<ObjectId?> SaveJobMapSection(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{

			var mapSectionIdStr = mapSectionResponse.MapSectionId;
			if (string.IsNullOrEmpty(mapSectionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.MapSectionId), "The MapSectionId cannot be null.");
			}

			var mapSubdivisionIdStr = mapSectionResponse.SubdivisionId;
			if (string.IsNullOrEmpty(mapSubdivisionIdStr))
			{
				throw new ArgumentNullException(nameof(MapSectionResponse.SubdivisionId), "The SubdivisionId cannot be null.");
			}

			var jobSubdivisionIdStr = mapSectionRequest.OriginalSourceSubdivisionId;
			if (string.IsNullOrEmpty(jobSubdivisionIdStr))
			{
				throw new ArgumentNullException(nameof(mapSectionRequest.OriginalSourceSubdivisionId), "The OriginalSourceSubdivisionId cannot be null.");
			}

			var jobIdStr = mapSectionRequest.JobId;
			if (string.IsNullOrEmpty(jobIdStr))
			{
				throw new ArgumentNullException(nameof(mapSectionRequest.JobId), "The OwnerId cannot be null.");
			}

			var blockIndex = new SizeInt(mapSectionRequest.ScreenPositionReleativeToCenter);

			//var result = _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionResponse, mapSectionRequest.JobId, mapSectionRequest.JobType, blockIndex, mapSectionRequest.IsInverted, mapSectionRequest.OwnerType, jobSubdivisionId);

			var result = _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionRequest.JobType, new ObjectId(jobIdStr), new ObjectId(mapSectionIdStr), blockIndex, mapSectionRequest.IsInverted, new ObjectId(mapSubdivisionIdStr), new ObjectId(jobSubdivisionIdStr), mapSectionRequest.OwnerType);

			return result;

		}

		#endregion

		#region IDisposable Support

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					Stop(true);

					if (_cts != null)
					{
						_cts.Dispose();
					}

					if (_workQueue != null)
					{
						_workQueue.Dispose();
					}
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}

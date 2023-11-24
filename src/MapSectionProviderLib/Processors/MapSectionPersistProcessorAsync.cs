﻿using MongoDB.Bson;
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
	public class MapSectionPersistProcessorAsync : IDisposable
	{
		private readonly MapSectionVectorProvider _mapSectionVectorProvider;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private const int QUEUE_CAPACITY = 500;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionPersistRequest> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;

		private readonly object _queueLock = new();

		private readonly bool _useDetailedDebug = false;

		#region Constructor

		public MapSectionPersistProcessorAsync(IMapSectionAdapter mapSectionAdapter, MapSectionVectorProvider mapSectionVectorProvider)
		{
			_mapSectionVectorProvider = mapSectionVectorProvider;
			_mapSectionAdapter = mapSectionAdapter;
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSectionPersistRequest>(QUEUE_CAPACITY);
			_workQueueProcessor = Task.Run(async () => await ProcessTheQueueAsync());
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

		private async Task ProcessTheQueueAsync()
		{
			while (!_cts.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionPersistRequest = _workQueue.Take(_cts.Token);
					
					if (_cts.IsCancellationRequested) break;

					var mapSectionRequest = mapSectionPersistRequest.Request;

					if (mapSectionPersistRequest.OnlyInsertJobMapSectionRecord)
					{
						var mapSectionIdStr = mapSectionRequest.MapSectionId ?? throw new InvalidOperationException("The MapSectionRequest's MapSectionId is null on call to SaveJobMapSection.");
						var mapSectionId = new ObjectId(mapSectionIdStr);

						await SaveJobMapSectionAsync(mapSectionId, mapSectionRequest);
					}
					else
					{
						var mapSectionResponse = mapSectionPersistRequest.Response;

						if (mapSectionResponse == null)
						{
							throw new InvalidOperationException("The MapSectionPersist Processor received an empty MapSectionResponse.");
						}

						if (mapSectionResponse.MapSectionVectors2 != null)
						{
							await PersistTheCountAndZValuesAsync(mapSectionRequest, mapSectionResponse);
						}
						else
						{
							Debug.WriteLine($"The MapSectionPersist Processor received an empty MapSectionResponse.");
						}

						_mapSectionVectorProvider.ReturnToPool(mapSectionResponse);
					}
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

		private async Task PersistTheCountAndZValuesAsync(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			var mapSectionId = await PersistTheCountValuesAsync(mapSectionRequest, mapSectionResponse);

			if (mapSectionId.HasValue)
			{
				if (mapSectionResponse.AllRowsHaveEscaped)
				{
					var zValuesRecordOnFile = await _mapSectionAdapter.DoesMapSectionZValuesExistAsync(mapSectionId.Value, _cts.Token);

					if (zValuesRecordOnFile)
					{
						_ = await _mapSectionAdapter.DeleteZValuesAync(mapSectionId.Value);
					}
				}
				else
				{
					if (mapSectionResponse.MapSectionZVectors != null)
					{
						await PersistTheZValuesAsync(mapSectionId.Value, mapSectionResponse);
					}
					else
					{
						if (mapSectionRequest.MapCalcSettings.SaveTheZValues)
						{
							Debug.WriteLine("WARNING: MapSectionPersistProcessor: The MapSectionZValues is null, but the SaveTheZValues setting is true.");
						}
					}
				}
			}
		}

		private async Task<ObjectId?> PersistTheCountValuesAsync(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			CheckMapSectionId(mapSectionRequest, mapSectionResponse);

			var mapSectionIdStr = mapSectionResponse.MapSectionId;
			ObjectId? mapSectionId;

			if (mapSectionIdStr != null)
			{
				// UPDATE
				mapSectionId = new ObjectId(mapSectionIdStr);
				Debug.WriteLineIf(_useDetailedDebug, $"Updating Count Values for {mapSectionId}, bp: {mapSectionResponse.BlockPosition}.");

				// TODO: Consider creating and using a syncrhonous version of the UpdateCountValuesAsync method.
				_ = await _mapSectionAdapter.UpdateCountValuesAync(mapSectionResponse);

				// A JobMapSectionRecord (identified by the triplet of mapSectionId, ownerId and jobOwnerType) may not be on file.
				// This will insert one if not already present.
				await SaveJobMapSectionAsync(mapSectionId.Value, mapSectionRequest);
			}
			else
			{
				// INSERT
				//Debug.WriteLine($"Creating MapSection for {mapSectionResponse.MapSectionId}, bp: {mapSectionResponse.BlockPosition}.");

				// TODO: Consider creating and using a syncrhonous version of the SaveMapSectionAsync method.
				mapSectionId = await _mapSectionAdapter.SaveMapSectionAsync(mapSectionResponse);

				// Experimental
				if (mapSectionResponse.MapSectionId != null && mapSectionId != null)
				{
					var msrMapSectionId = new ObjectId(mapSectionResponse.MapSectionId);

					if (msrMapSectionId != mapSectionId)
					{
						// Record already on file.
						Debug.WriteLine($"Not Inserting MapSectionRecord with BlockPos: {mapSectionResponse.BlockPosition} and ScreenPos: {mapSectionRequest.ScreenPosition}. A record already exists for this block position with Id: {mapSectionId}.");
					}
				}

				if (mapSectionId.HasValue)
				{
					mapSectionResponse.MapSectionId = mapSectionId.ToString();
					await SaveJobMapSectionAsync(mapSectionId.Value, mapSectionRequest);
				}
			}

			return mapSectionId;
		}

		private async Task PersistTheZValuesAsync(ObjectId mapSectionId, MapSectionResponse mapSectionResponse)
		{
			var zValuesRecordOnFile = await _mapSectionAdapter.DoesMapSectionZValuesExistAsync(mapSectionId, _cts.Token);

			if (zValuesRecordOnFile)
			{
				Debug.WriteLineIf(_useDetailedDebug, $"PersistProc: UpdateZValuesAsync for {mapSectionId}, bp: {mapSectionResponse.BlockPosition}.");
				_ = await _mapSectionAdapter.UpdateZValuesAync(mapSectionResponse, mapSectionId);
			}
			else
			{
				if (!mapSectionResponse.AllRowsHaveEscaped)
				{
					Debug.WriteLineIf(_useDetailedDebug, $"PersistProc: SaveMapSectionZValuesAsync for {mapSectionId}, bp: {mapSectionResponse.BlockPosition}.");
					_ = await _mapSectionAdapter.SaveMapSectionZValuesAsync(mapSectionResponse, mapSectionId);
				}
			}
		}

		private Task SaveJobMapSectionAsync(ObjectId mapSectionId, MapSectionRequest mapSectionRequest)
		{
			//var mapSectionIdStr = mapSectionResponse.MapSectionId;
			//if (string.IsNullOrEmpty(mapSectionIdStr))
			//{
			//	throw new ArgumentNullException(nameof(MapSectionResponse.MapSectionId), "The MapSectionId cannot be null.");
			//}

			var mapSubdivisionIdStr = mapSectionRequest.Subdivision.Id.ToString();
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

			Task<ObjectId?> result;

			if (mapSectionRequest.RegularPosition != null)
			{
				var blockIndex = new SizeInt(mapSectionRequest.RegularPosition.ScreenPositionReleativeToCenter);
				result = _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionRequest.JobType, new ObjectId(jobIdStr), mapSectionId, blockIndex, isInverted: false, new ObjectId(mapSubdivisionIdStr), new ObjectId(jobSubdivisionIdStr), mapSectionRequest.OwnerType);
			}
			else
			{
				var blockIndex = new SizeInt(mapSectionRequest.InvertedPosition!.ScreenPositionReleativeToCenter);
				result = _mapSectionAdapter.SaveJobMapSectionAsync(mapSectionRequest.JobType, new ObjectId(jobIdStr), mapSectionId, blockIndex, isInverted: true, new ObjectId(mapSubdivisionIdStr), new ObjectId(jobSubdivisionIdStr), mapSectionRequest.OwnerType);
			}

			return result;
		}

		private void CheckMapSectionId(MapSectionRequest mapSectionRequest, MapSectionResponse mapSectionResponse)
		{
			var reqId = mapSectionRequest.MapSectionId;
			var resId = mapSectionResponse.MapSectionId;

			if (reqId == null)
			{
				Debug.Assert(resId == null, "The Request's MapSectionId is null, but the Response's MapSectionId is not null.");
			}
			else
			{
				Debug.Assert(string.Equals(reqId, resId, StringComparison.OrdinalIgnoreCase), "The Request's MapSectionId does not equal the Reponse's MapSectionId.");
			}
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
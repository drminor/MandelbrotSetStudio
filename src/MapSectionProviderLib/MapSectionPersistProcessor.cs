using MEngineDataContracts;
using MSS.Common;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MapSectionProviderLib
{
	public class MapSectionPersistProcessor : IDisposable
	{
		private readonly IMapSectionRepo _mapSectionRepo;

		private const int MAX_WORK_ITEMS = 4;
		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSectionResponse> _workQueue;

		private Task _workQueueProcessor;
		private bool disposedValue;

		public MapSectionPersistProcessor(IMapSectionRepo mapSectionRepo)
		{
			_mapSectionRepo = mapSectionRepo;
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSectionResponse>(MAX_WORK_ITEMS);
			_workQueueProcessor = Task.Run(async () => await ProcessTheQueueAsync(_cts.Token));
		}

		public void AddWork(MapSectionResponse item)
		{
			_workQueue.Add(item);
		}

		public void Stop(bool immediately)
		{
			if (immediately)
			{
				_cts.Cancel();
			}
			else
			{
				_workQueue.CompleteAdding();
			}

			try
			{
				_workQueueProcessor.Wait(120 * 1000);
			}
			catch
			{

			}
		}

		private async Task ProcessTheQueueAsync(CancellationToken ct)
		{
			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionResponse = _workQueue.Take(ct);
					var mapSectionId = await _mapSectionRepo.SaveMapSectionAsync(mapSectionResponse);
				}
				catch (TaskCanceledException)
				{
					Debug.WriteLine("The persist queue got a TCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The persist queue got an exception: {e}.");
					throw;
				}
			}
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)
					Stop(false);

					if (_cts != null)
					{
						_cts.Dispose();
					}

					if (_workQueue != null)
					{
						_workQueue.Dispose();
					}

					if (_workQueueProcessor != null)
					{
						_workQueueProcessor.Dispose();
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
	}
}

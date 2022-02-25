using MEngineDataContracts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using MapSecWorkReqType = MapSectionProviderLib.WorkItem<MEngineDataContracts.MapSectionRequest, MEngineDataContracts.MapSectionResponse>;

namespace MapSectionProviderLib
{
	public class MapSectionResponseProcessor : IDisposable
	{
		private const int QUEUE_CAPACITY = 200;
		private readonly object _lock = new();

		private readonly CancellationTokenSource _cts;
		private readonly BlockingCollection<MapSecWorkReqType> _workQueue;

		private readonly List<int> _cancelledJobIds;

		private Task _workQueueProcessor;
		private bool disposedValue;

		public MapSectionResponseProcessor()
		{
			_cts = new CancellationTokenSource();

			_workQueue = new BlockingCollection<MapSecWorkReqType>(QUEUE_CAPACITY);
			_cancelledJobIds = new List<int>();

			_workQueueProcessor = Task.Run(ProcessTheQueue);
		}

		public void AddWork(MapSecWorkReqType mapSectionWorkItem)
		{
			// TODO: Use a Lock to prevent update of IsAddingCompleted while we are in this method.
			if (!_workQueue.IsAddingCompleted)
			{
				_workQueue.Add(mapSectionWorkItem);
			}
		}

		public void CancelJob(int jobId)
		{
			lock (_lock)
			{
				if (!_cancelledJobIds.Contains(jobId))
				{
					_cancelledJobIds.Add(jobId);
				}
			}
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

		private void ProcessTheQueue()
		{
			var ct = _cts.Token;

			while(!ct.IsCancellationRequested && !_workQueue.IsCompleted)
			{
				try
				{
					var mapSectionWorkItem = _workQueue.Take(ct);

					mapSectionWorkItem.Request.Completed = true;
					mapSectionWorkItem.RunWorkAction();
				}
				catch (OperationCanceledException)
				{
					Debug.WriteLine("The response queue got a OCE.");
				}
				catch (Exception e)
				{
					Debug.WriteLine($"The response queue got an exception: {e}.");
					throw;
				}
			}
		}

		#region IDispoable Support

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

		#endregion
	}
}

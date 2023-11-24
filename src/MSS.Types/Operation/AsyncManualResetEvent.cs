using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace MSS.Types
{
	// TODO: Add support for using a cancellation token.
	// See: https://github.com/StephenCleary/AsyncEx/blob/master/src/Nito.AsyncEx.Coordination/AsyncManualResetEvent.cs

	public class AsyncManualResetEvent
	{
		private volatile TaskCompletionSource<bool> m_tcs;

		public AsyncManualResetEvent()
		{
			m_tcs = new TaskCompletionSource<bool>();
		}

		// Return the task that will complete when Set is called.
		// This task can be awaited.
		public Task WaitAsync() { return m_tcs.Task; }

		// Signal the waiter and if the task is completed, execute the waiter on this thread before returning.
		public void Set() { m_tcs.TrySetResult(true); }

		// Signal the waiters on a background thread, allowing the caller to return immediately
		public void SetAsync()
		{
			var tcs = m_tcs;
			_ = Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s!).TrySetResult(true), tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);

			// Wait for the TrySetResult to complete. This will signal the waiter on a different thread and let this thread continue.
			tcs.Task.Wait();
		}

		// If the current TaskCompletionSource is not yet completed return it -- new callers will be blocked until this is completed.
		// Otherwise, prepare a new TaskCompletionSource that has not yet been completed -- so that new callers wil be blocked until this is completed.
		public void Reset()
		{
			while (true)
			{
				var tcs = m_tcs;
				if (!tcs.Task.IsCompleted ||
					Interlocked.CompareExchange(ref m_tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
					return;
			}
		}

		/// <summary>
		/// Asynchronously waits for this event to be set or for the wait to be canceled.
		/// </summary>
		/// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
		public Task WaitAsync(CancellationToken cancellationToken)
		{
			var waitTask = WaitAsync();
			if (waitTask.IsCompleted)
				return waitTask;
			return waitTask.WaitAsync(cancellationToken);
		}

		///// <summary>
		///// Synchronously waits for this event to be set. This method may block the calling thread.
		///// </summary>
		//public void Wait()
		//{
		//	var x = WaitAsync();
		//	x.GetAwaiter().GetResult();
		//}

		///// <summary>
		///// Synchronously waits for this event to be set. This method may block the calling thread.
		///// </summary>
		///// <param name="cancellationToken">The cancellation token used to cancel the wait. If this token is already canceled, this method will first check whether the event is set.</param>
		//public void Wait(CancellationToken cancellationToken)
		//{
		//	var ret = WaitAsync(CancellationToken.None);
		//	if (ret.IsCompleted)
		//		return;

		//	try
		//	{
		//		ret.Wait(cancellationToken);
		//	}
		//	catch (AggregateException ex)
		//	{
		//		throw PrepareForRethrow(ex.InnerException!);
		//	}
		//}

		//public static Exception PrepareForRethrow(Exception exception)
		//{
		//	ExceptionDispatchInfo.Capture(exception).Throw();

		//	// The code cannot ever get here. We just return a value to work around a badly-designed API (ExceptionDispatchInfo.Throw):
		//	//  https://connect.microsoft.com/VisualStudio/feedback/details/689516/exceptiondispatchinfo-api-modifications (http://www.webcitation.org/6XQ7RoJmO)
		//	return exception;
		//}
	}
}

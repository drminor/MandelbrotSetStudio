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

		//private bool TFunc(object? state)
		//{
		//	var ourTcs = state as TaskCompletionSource<bool>;

		//	ourTcs!.TrySetResult(true);

		//	return true;

		//	//if (state is TaskCompletionSource<bool> tcsBool)
		//	//{
		//	//	tcsBool.TrySetResult(true);
		//	//	return true;
		//	//}
		//	//else
		//	//{
		//	//	return false;
		//	//}
		//}
	}
}

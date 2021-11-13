using System;
using System.Diagnostics;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace FGenConsole
{
	class MqHelper
	{
		public static Task<Message> PeekMessageAsync(MessageQueue mq, TimeSpan timeout)
		{
			// this will be our sentry that will know when our async operation is completed
			var tcs = new TaskCompletionSource<Message>();

			try
			{
				mq.BeginPeek(timeout, null, (iar) =>
				{
					try
					{
						var result = mq.EndPeek(iar);
						tcs.TrySetResult(result);
					}
					catch (MessageQueueException mqe)
					{
						if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
						{
							tcs.TrySetResult(default);
						}
						else
						{
							Debug.WriteLine($"Got exception on EndPeek. The error is {mqe.Message}.");
							throw;
						}
					}
					catch (OperationCanceledException)
					{
						// if the inner operation was canceled, this task is cancelled too
						tcs.TrySetCanceled();
					}
					catch (Exception ex)
					{
						// general exception has been set
						bool flag = tcs.TrySetException(ex);
						if (flag && ex as ThreadAbortException != null)
						{
							Debug.WriteLine("Check this. Handling exception from End Peek.");
							//tcs.Task.m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
						}
					}
				});
			}
			catch (Exception e)
			{
				tcs.TrySetResult(default);

				// propagate exceptions to the outside
				Debug.WriteLine($"Got exception on BeginReceive. The error is {e.Message}.");
				throw;
			}

			return tcs.Task;
		}

		public static Task<Message> ReceiveMessageAsync(MessageQueue mq, TimeSpan timeout, object state = null)
		{
			// this will be our sentry that will know when our async operation is completed
			var tcs = new TaskCompletionSource<Message>();

			try
			{
				mq.BeginReceive(timeout, state, (iar) =>
				{
					try
					{
						var result = mq.EndReceive(iar);
						tcs.TrySetResult(result);
					}
					catch (MessageQueueException mqe)
					{
						if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
						{
							tcs.TrySetResult(default);
						}
						else
						{
							Debug.WriteLine($"Got exception on EndReceive. The error is {mqe.Message}.");
							throw;
						}
					}
					catch (OperationCanceledException)
					{
						// if the inner operation was canceled, this task is cancelled too
						tcs.TrySetCanceled();
					}
					catch (Exception ex)
					{
						// general exception has been set
						bool flag = tcs.TrySetException(ex);
						if (flag && ex as ThreadAbortException != null)
						{
							Debug.WriteLine("Check this. Handling exception from End Receive.");
							//tcs.Task.m_contingentProperties.m_exceptionsHolder.MarkAsHandled(false);
						}
					}
				});
			}
			catch (Exception e)
			{
				tcs.TrySetResult(default);

				// propagate exceptions to the outside
				Debug.WriteLine($"Got exception on BeginReceive. The error is {e.Message}.");
				throw;
			}

			return tcs.Task;
		}

		public static async Task<Message> ReceiveMessageByCorrelationIdAsync(MessageQueue mq, string id, TimeSpan timeout)
		{
			DateTime start = DateTime.Now;
			Message m = await PeekMessageAsync(mq, timeout);

			if (m == null)
				return m;

			Debug.WriteLine($"The first messasge has a cor id of {m.CorrelationId}.");

			int msElasped = (DateTime.Now - start).Milliseconds;

			TimeSpan remaining;
			if(msElasped < 100)
			{
				remaining = TimeSpan.FromMilliseconds(100);
			}
			else
			{
				remaining = TimeSpan.FromMilliseconds(msElasped);
			}
			m = GetMessageByCorId(mq, id, remaining);
			return m;
		}

		private static Message GetMessageByCorId(MessageQueue mq, string id, TimeSpan waitDuration)
		{
			try
			{
				Message m = mq.ReceiveByCorrelationId(id, waitDuration);
				return m;
			}
			catch (MessageQueueException mqe)
			{
				if (mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
				{
					return null;
				}
				else
				{
					Debug.WriteLine($"Received an MessageQueueException while executing: ReceiveByCorrelationId on queue with Path = {mq.Path}. The MqErrorCode is {mqe.MessageQueueErrorCode}. The error is {mqe.Message}.");
					throw;
				}
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Received an error while executing: ReceiveByCorrelationId on queue with Path = {mq.Path}. The error is {e.Message}.");
				throw;
			}
		}

		public static async Task<Message> ReceiveMessageByCorrelationIdAsync_OLD(MessageQueue mq, string id, CancellationToken ct)
		{
			TimeSpan waitDuration = TimeSpan.FromMilliseconds(1000);

			while(!ct.IsCancellationRequested)
			{
				try
				{
					Message m = mq.ReceiveByCorrelationId(id, waitDuration);
					return m;
					//if (m != null)
					//{
					//	return m;
					//}
				}
				catch (MessageQueueException mqe)
				{
					if(mqe.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
					{
						await Task.Delay(1000, ct);
						continue;
					}
					else
					{
						Debug.WriteLine($"Received an MessageQueueException while executing: ReceiveByCorrelationId on queue with Path = {mq.Path}. The MqErrorCode is {mqe.MessageQueueErrorCode}. The error is {mqe.Message}.");
						throw;
					}
				}
				catch (Exception e)
				{
					Debug.WriteLine($"Received an error while executing: ReceiveByCorrelationId on queue with Path = {mq.Path}. The error is {e.Message}.");
					throw;
				}
			}

			return null;
		}

		public static MessageQueue GetQ(string path, QueueAccessMode queueAccessMode, Type[] types, MessagePropertyFilter mpf)
		{
			if (MessageQueue.Exists(path) == false)
			{
				MessageQueue.Create(path);
			}

			MessageQueue mq = new MessageQueue(path, queueAccessMode);

			if (types != null)
			{
				mq.Formatter = new XmlMessageFormatter(types);
			}

			if(mpf != null)
			{
				mq.MessageReadPropertyFilter = mpf;
			}

			return mq;
		}

		//public static async Task WaitWithToken(int millisecondsToWait, CancellationToken ct)
		//{
		//	try
		//	{
		//		await Task.Delay(millisecondsToWait, ct);
		//	}
		//	catch (TaskCanceledException)
		//	{

		//	}
		//	catch (Exception e)
		//	{
		//		Debug.WriteLine($"Received error while from Task.Delay. The error message is {e.Message}.");
		//	}
		//}

	}
}

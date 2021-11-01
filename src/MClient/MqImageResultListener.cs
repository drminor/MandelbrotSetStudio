using Experimental.System.Messaging;
using FSTypes;
using MqMessages;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using MapSectionRepo;

namespace MClient
{
	internal class MqImageResultListener
	{
		private readonly JobForMq _jobForMq;
		private readonly string _inputQueuePath;
		private readonly BlockingCollection<SubJob> _sendQueue;
		private readonly TimeSpan _waitDuration;

		private readonly CancellationTokenSource _cts;
		Task _task;

		public MqImageResultListener(JobForMq jobForMq, string inputQueuePath, BlockingCollection<SubJob> sendQueue, TimeSpan waitDuration)
		{
			_jobForMq = jobForMq;
			_inputQueuePath = inputQueuePath;
			_sendQueue = sendQueue;
			_waitDuration = waitDuration;

			_cts = new CancellationTokenSource();
			_task = null;
		}

		public void Start()
		{
			_task = Task.Run(async () => await ReceiveImageResultsAsync());
		}

		public void Stop()
		{
			_cts.Cancel();

			try
			{
				_task.Wait(20 * 1000);
				Debug.WriteLine($"The response listener for Job: {_jobForMq.JobId} has completed.");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Received an exception while trying to stop the response listener for Job: {_jobForMq.JobId}. Got exception: {e.Message}.");
				throw;
			}

			// Remove "in transit" responses.
			RemoveResponses(_jobForMq.MqRequestCorrelationId);
		}

		private async Task ReceiveImageResultsAsync()
		{
			using MessageQueue inQ = GetJobResponseQueue();
			while (!_cts.IsCancellationRequested /*&& !jobForMq.IsLastSubJob*/)
			{
				Message m = await MqHelper.ReceiveMessageByCorrelationIdAsync(inQ, _jobForMq.MqRequestCorrelationId, _waitDuration);

				if (m == null)
				{
					Debug.WriteLine($"No FGenResult message present for correlationId: {FMsgId(_jobForMq.MqRequestCorrelationId)}.");
					continue;
				}

				FJobResult jobResult = (FJobResult)m.Body;
				PointInt pos = jobResult.Area.Point;
				Debug.WriteLine($"Received FJobResult for correlationId: {FMsgId(_jobForMq.MqRequestCorrelationId)}, X:{pos.X}, Y:{pos.Y}.");

				SubJob subJob = CreateSubJob(jobResult, _jobForMq);

				_sendQueue.Add(subJob);
			}

			if (_jobForMq.IsLastSubJob)
			{
				Debug.WriteLine($"The result listener for {_jobForMq.JobId} is stopping. We have received the last result.");
			}
			else if (_cts.IsCancellationRequested)
			{
				Debug.WriteLine($"The result listener for {_jobForMq.JobId} has been cancelled.");
			}
			else
			{
				Debug.WriteLine($"The result listener for {_jobForMq.JobId} is stopping for unknown reason.");
			}
		}

		private MessageQueue GetJobResponseQueue()
		{
			Type[] rTtypes = new Type[] { typeof(FJobResult) };

			var mpf = new MessagePropertyFilter()
			{
				Body = true,
				//Id = true,
				CorrelationId = true
			};

			MessageQueue result = MqHelper.GetQ(_inputQueuePath, QueueAccessMode.Receive, rTtypes, mpf);
			return result;
		}

		private void RemoveResponses(string correlationId)
		{
			if (correlationId == null)
			{
				Debug.WriteLine("Attempting to remove responses with a null cor id. Not removing any responses.");
				return;
			}

			using MessageQueue inQ = GetJobResponseQueue();
			Message m = null;
			do
			{
				m = MqHelper.GetMessageByCorId(inQ, correlationId, TimeSpan.FromMilliseconds(10));
			}
			while (m != null);
		}

		private static SubJob CreateSubJob(FJobResult jobResult, JobForMq parentJob)
		{
			MapSectionWorkResult workResult = CreateWorkResult(jobResult);

			MapSectionWorkRequest workRequest = CreateMSWR(jobResult, parentJob.SMapWorkRequest.MaxIterations);
			MapSectionResult msr = CreateMapSectionResult(parentJob.JobId, workRequest.MapSection, workResult);

			var subJob = new SubJob(parentJob, workRequest)
			{
				MapSectionResult = msr
			};

			// We need to keep track if the last sub job has been sent, not received.
			//if (jobResult.IsFinalResult) parentJob.SetIsLastSubJob(true);

			return subJob;
		}

		private static MapSectionWorkRequest CreateMSWR(FJobResult jobResult, int maxIterations)
		{
			RectangleInt mapSection = jobResult.Area;
			var result = new MapSectionWorkRequest(mapSection, maxIterations, 0, 0);
			return result;
		}

		private static MapSectionWorkResult CreateWorkResult(FJobResult fJobResult)
		{
			int[] counts = fJobResult.GetValues();
			var result = new MapSectionWorkResult(counts);
			return result;
		}

		private static MapSectionResult CreateMapSectionResult(int jobId, RectangleInt mapSection, MapSectionWorkResult workResult)
		{
			var result = new MapSectionResult(jobId, mapSection, workResult.Counts);
			return result;
		}

		private static string FMsgId(string mId)
		{
			return mId[^5..];
		}

	}
}

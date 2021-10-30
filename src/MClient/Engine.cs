using Experimental.System.Messaging;
using FSTypes;
using MqMessages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Coords = MqMessages.Coords;

namespace MClient
{
	public class Engine
	{
		public const int BLOCK_SIZE = 100;
		public const int NUMBER_OF_SUB_PROCESSORS = 4;

		public const string OUTPUT_Q_PATH = @".\private$\FGenJobs";
		public const string INPUT_Q_PATH = @".\private$\FGenResults";
		private static readonly TimeSpan DefaultWaitDuration = TimeSpan.FromSeconds(30);

		private IClientConnector _clientConnector;

		private int _nextJobId;
		private int _nextJobPtr;

		private readonly Dictionary<int, IJob> _jobs;
		private readonly CancellationTokenSource _cts;
		private readonly object _jobLock = new();
		private readonly ManualResetEvent _haveWork;

		private readonly BlockingCollection<SubJob> _sendQueue = new(50);

		public Engine()
		{
			_clientConnector = null;
			_jobs = new Dictionary<int, IJob>();
			_cts = new CancellationTokenSource();

			_haveWork = new ManualResetEvent(false);
			WaitDuration = DefaultWaitDuration;

			_nextJobId = 0;
			_nextJobPtr = 0;
		}

		public TimeSpan WaitDuration { get; set; }

		#region Job Control

		public int SubmitJob(IJob job)
		{
			int jobId;

			lock (_jobLock)
			{
				jobId = NextJobId;
				job.JobId = jobId;

				Debug.WriteLine("Adding job to queue.");
				_jobs.Add(jobId, job);
				_haveWork.Set();
			}

			return jobId;
		}

		public Histogram GetHistogram(int jobId)
		{
			Histogram result = null;

			IJob job = GetJob(jobId);
			if(job != null)
			{
				result = new MqHistogram().GetHistogram(jobId);
			}

			return result;
		}

		public void ReplayJob(int jobId, int targetIterations)
		{
			IJob job = GetJob(jobId);
			if (job != null)
			{
				job.SMapWorkRequest.MaxIterations = targetIterations;
				job.ResetSubJobsRemainingToBeSent();

				//mqJob.ResetSubJobsRemainingToBeSent();
				SendReplayJobRequestToMq(job as JobForMq);
			}
		}

		public void CancelJob(int jobId, bool deleteRepo)
		{
			IJob job = RemoveJob(jobId);

			if(job != null)
			{
				job.CancelRequested = true;
				if (_clientConnector != null)
				{
					_clientConnector.ConfirmJobCancel(job.ConnectionId, jobId);
				}

				JobForMq jobForMq = job as JobForMq;
				// Send Cancel message to MQ.
				SendDeleteJobRequestToMq(jobForMq, deleteRepo);

				jobForMq.MqImageResultListener.Stop();
			}
		}

		private IJob RemoveJob(int jobId)
		{
			lock (_jobLock)
			{
				if (_jobs.TryGetValue(jobId, out IJob job))
				{
					_jobs.Remove(jobId);
					return job;
				}
				else
				{
					return null;
				}
			}
		}

		public int NumberOfJobs
		{
			get
			{
				return _jobs.Count;
			}
		}

		private int NextJobId
		{
			get
			{
				return _nextJobId++;
			}
		}

		//private void CancelAllJobs()
		//{
		//	var jobIds = _jobs.Values.Select(v => v.JobId).ToList();
		//	foreach (int jobId in jobIds)
		//	{
		//		CancelJob(jobId, deleteRepo: false);
		//	}
		//}

		private IJob GetNextJob(CancellationToken cts)
		{
			IJob result = null;
			do
			{
				if (cts.IsCancellationRequested)
				{
					break;
				}

				bool wasSignaled = _haveWork.WaitOne(1000);

				if (cts.IsCancellationRequested)
				{
					break;
				}

				if (wasSignaled)
				{
					lock (_jobLock)
					{
						IJob[] jobs = _jobs.Values.Where(j => !j.IsCompleted).ToArray();

						if (jobs == null || jobs.Length == 0)
						{
							Debug.WriteLine("There are no un completed jobs, resetting HaveWork.");
							_nextJobPtr = 0;
							_haveWork.Reset();
						}
						else
						{
							if (_nextJobPtr > jobs.Length - 1)
								_nextJobPtr = 0;

							result = jobs[_nextJobPtr++];
							//Debug.WriteLine($"The next job has id = {result.JobId}.");
							break;
						}
					}
				}

			} while (true);

			//Debug.WriteLine("Get Next Job is returning.");
			return result;
		}

		#endregion

		#region Work

		public void Start(IClientConnector clientConnector)
		{
			_clientConnector = clientConnector;

			Task.Run(() => SendProcessor(_sendQueue, _cts.Token), _cts.Token);
			Task.Run(() => QueueWork(_cts.Token), _cts.Token);
		}

		public void Stop()
		{
			_cts.Cancel();
			_haveWork.Set();
		}

		private void QueueWork(CancellationToken ct)
		{
			do
			{
				IJob job = GetNextJob(ct);
				if (ct.IsCancellationRequested) return;

				JobForMq jobForMq = GetJobForMqFromJob(job);
				jobForMq.MqRequestCorrelationId = SendJobToMq(jobForMq);

				Debug.WriteLine($"Starting a new ImageResultListener for {jobForMq.JobId}.");
				MqImageResultListener resultListener = new(jobForMq, INPUT_Q_PATH, _sendQueue, WaitDuration);
				resultListener.Start();
				jobForMq.MqImageResultListener = resultListener;

				jobForMq.MarkAsCompleted();

			} while (true);
		}

		private static JobForMq GetJobForMqFromJob(IJob job)
		{
			if (job is JobForMq result)
			{
				return result;
			}
			else
			{
				//result = new JobForMq(job.SMapWorkRequest);
				//return result;
				throw new InvalidOperationException($"The job must be an instance of the JobForMq class. The job name is {job.SMapWorkRequest.Name}.");
			}
		}

		private void SendProcessor(BlockingCollection<SubJob> sendQueue, CancellationToken ct)
		{
			try
			{
				while(!ct.IsCancellationRequested)
				{
					if(sendQueue.TryTake(out SubJob subJob, -1, ct))
					{
						if (!subJob.ParentJob.CancelRequested)
						{
							subJob.ParentJob.DecrementSubJobsRemainingToBeSent();
							bool isFinalSubJob = subJob.ParentJob.IsLastSubJob;

							if (_clientConnector != null)
							{
								Debug.WriteLine($"Sending subjob with x: {subJob.MapSectionResult.MapSection.SectionAnchor.X} " +
									$"and y: {subJob.MapSectionResult.MapSection.SectionAnchor.Y}. " +
									$"with connId = {subJob.ConnectionId}. IsLastResult = {isFinalSubJob}.");
									//$"It has {subJob.result.ImageData.Length} count values.");
								_clientConnector.ReceiveImageData(subJob.ConnectionId, subJob.MapSectionResult, isFinalSubJob);


								//if(subJob.ParentJob is Job localJob)
								//{
								//	Debug.WriteLine($"Results written: {localJob.WorkResultWriteCount}, Results re-written: {localJob.WorkResultReWriteCount}.");
								//}
							}
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine("Send Queue Consuming Enumerable canceled.");
				//throw;
			}
			catch (InvalidOperationException)
			{
				Debug.WriteLine("Send Queue Consuming Enumerable completed.");
				//throw;
			}
		}

		private IJob GetJob(int jobId)
		{
			if (_jobs.TryGetValue(jobId, out IJob job))
			{
				return job;
			}
			else
			{
				return null;
			}
		}

		#endregion

		#region MQ Methods

		private static string SendJobToMq(JobForMq job)
		{
			using MessageQueue outQ = MqHelper.GetQ(OUTPUT_Q_PATH, QueueAccessMode.Send, null, null);
			FJobRequest fJobRequest = CreateFJobRequest(job.JobId, job.SMapWorkRequest);
			Debug.WriteLine($"Sending request with JobId {fJobRequest.JobId} to output Q.");

			Message m = new(fJobRequest);
			outQ.Send(m);

			return m.Id;
		}

		private static string SendDeleteJobRequestToMq(JobForMq job, bool deleteRepo)
		{
			using MessageQueue outQ = MqHelper.GetQ(OUTPUT_Q_PATH, QueueAccessMode.Send, null, null);
			FJobRequest fJobRequest = FJobRequest.CreateDeleteRequest(job.JobId, deleteRepo);
			Message m = new(fJobRequest);
			outQ.Send(m);

			return m.Id;
		}

		private static string SendReplayJobRequestToMq(JobForMq job)
		{
			using MessageQueue outQ = MqHelper.GetQ(OUTPUT_Q_PATH, QueueAccessMode.Send, null, null);
			FJobRequest fJobRequest = FJobRequest.CreateReplayRequest(job.JobId);
			Message m = new(fJobRequest);
			outQ.Send(m);

			return m.Id;
		}

		private static FJobRequest CreateFJobRequest(int jobId, SMapWorkRequest smwr)
		{
			Coords coords = smwr.SCoords.GetCoords();

			var area = new RectangleInt(new PointInt(smwr.Area.SectionAnchor.X, smwr.Area.SectionAnchor.Y), new SizeInt(smwr.Area.CanvasSize.Width, smwr.Area.CanvasSize.Height));
			var samplePoints = new SizeInt(smwr.CanvasSize.Width, smwr.CanvasSize.Height);

			FJobRequest fJobRequest = new(jobId, smwr.Name, FJobRequestType.Generate, coords, area, samplePoints, (uint) smwr.MaxIterations);

			return fJobRequest;
		}

		#endregion
	}
}

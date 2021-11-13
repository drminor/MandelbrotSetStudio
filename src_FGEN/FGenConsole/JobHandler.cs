using MqMessages;
using qdDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
//using System.Messaging;
using System.Threading;
using System.Threading.Tasks;
//using PointInt = MqMessages.PointInt;
//using RectangleInt = MqMessages.RectangleInt;
//using SizeInt = MqMessages.SizeInt;

using MSS.Types;

namespace FGenConsole
{
	class JobHandler
	{
		public const string INPUT_Q_PATH = @".\private$\FGenJobs";
		public const string OUTPUT_Q_PATH = @".\private$\FGenResults";
		public const string OUTPUT_COORDS_Q_PATH = @".\private$\FCoordResults";
		public const string OUTPUT_HIST_Q_PATH = @".\private$\FHistResults";

		public const int NUM_THREADS = 2;
		public static TimeSpan DefaultWaitDuration = TimeSpan.FromSeconds(10);

		private readonly Dictionary<int, Job> _jobs;
		private int _nextJobPtr;

		private readonly CancellationTokenSource _cts;
		private readonly object _jobLock = new object();
		private readonly ManualResetEvent _haveWork;

		//private MessageQueue _outQ;

		private readonly BlockingCollection<Job> _replayWorkQueue = new BlockingCollection<Job>(6);

		private readonly BlockingCollection<SubJob> _workQueue = new BlockingCollection<SubJob>(NUM_THREADS);
		private readonly BlockingCollection<SubJob> _sendQueue = new BlockingCollection<SubJob>(6);

		private SubJobProcessor[] _subJobProcessors = null;
		private JobReplayProcessor _jobReplayProcessor = null;

		#region Constructors

		public JobHandler() : this(DefaultWaitDuration) { }

		public JobHandler(TimeSpan readWaitInterval)
		{
			//_outQ = null;
			WaitDuration = readWaitInterval;

			_jobs = new Dictionary<int, Job>();
			_cts = new CancellationTokenSource();

			_haveWork = new ManualResetEvent(false);
			_nextJobPtr = 0;
		}

		#endregion

		#region Public Properties

		public TimeSpan WaitDuration { get; set; }

		#endregion

		#region Job Control

		public int SubmitJob(Job job)
		{
			lock (_jobLock)
			{
				CheckForExistingJob(job);
				Debug.WriteLine("Adding job to queue.");
				_jobs.Add(job.JobId, job);
				_haveWork.Set();
			}

			return job.JobId;
		}

		private void CheckForExistingJob(Job job)
		{
			if (TryGetJob(job.JobId, out Job foundJob))
			{
				throw new InvalidOperationException($"There is an existing job with JobId:{job.JobId}.");
			}
			else
			{
				if(_jobs.Where(x => x.Value.FJobRequest.Name == job.FJobRequest.Name).Count() > 0)
				{
					throw new InvalidOperationException($"There is an existing job with with the same name: {job.FJobRequest.Name}. The JobId is {job.JobId}.");
				}
			}
		}

		public IDictionary<int, int> GetHistogram(int jobId)
		{
			if (TryGetJob(jobId, out Job job))
			{
				IDictionary<int, int> result = job.GetHistogram();
				return result;
			}
			return null;
		}

		public void CancelJob(int jobId, bool deleteRepo)
		{
			Job job = RemoveJob(jobId);

			if (job != null)
			{
				bool success = job.Close(deleteRepo);
			}
		}

		private Job RemoveJob(int jobId)
		{
			lock (_jobLock)
			{
				if (TryGetJob(jobId, out Job job))
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

		private bool TryGetJob(int jobId, out Job job)
		{
			lock (_jobLock)
			{
				return _jobs.TryGetValue(jobId, out job);
			}
		}

		#endregion

		#region Main Processing Methods

		public async Task HandleJobs(CancellationToken cToken)
		{
			//HandleJobResponses(m.Id, cToken);

			//_outQ = MqHelper.GetQ(OUTPUT_Q_PATH, QueueAccessMode.Send, null, null);

			Task t1 = Task.Run(() => SendProcessor(_sendQueue, _outQ, _cts.Token), _cts.Token);

			_subJobProcessors = new SubJobProcessor[NUM_THREADS];
			for (int wpCntr = 0; wpCntr < NUM_THREADS; wpCntr++)
			{
				_subJobProcessors[wpCntr] = new SubJobProcessor(_workQueue, _sendQueue, wpCntr);
				_subJobProcessors[wpCntr].Start();
			}

			_jobReplayProcessor = new JobReplayProcessor(_replayWorkQueue, _sendQueue);
			_jobReplayProcessor.Start();

			Task t2 = Task.Run(() => ProcessJobs(_workQueue, _cts.Token), _cts.Token);

			Type[] rTtypes = new Type[] { typeof(FJobRequest) };
			MessagePropertyFilter mpf = new MessagePropertyFilter
			{
				Body = true,
				Id = true,
				CorrelationId = true
			};

			using (MessageQueue inQ = MqHelper.GetQ(INPUT_Q_PATH, QueueAccessMode.Receive, rTtypes, mpf))
			{
				while (!cToken.IsCancellationRequested)
				{
					Message m = await MqHelper.ReceiveMessageAsync(inQ, WaitDuration);
					if (cToken.IsCancellationRequested)
						break;

					if (m != null)
					{
						Debug.WriteLine("Received a request message.");
						FJobRequest jobRequest = null;
						string requestType = "Unknown";

						try
						{
							jobRequest = (FJobRequest)m.Body;
							requestType = jobRequest.RequestType.ToString();
							Debug.WriteLine($"The message type is {requestType}. JobId: {jobRequest.JobId}.");
							HandleRequestMessage(jobRequest, m.Id);
						}
						catch (Exception e)
						{
							Debug.WriteLine($"Got an exception while processing a FJobRequest message of type {requestType}. The error is {e.Message}.");
						}
					}
					else
					{
						Debug.WriteLine("No request message present.");
					}
				}
			}

			Debug.WriteLine("Worker thread ending, starting cleanup routine.");
			Cleanup();
		}

		private void Cleanup()
		{
			if (_subJobProcessors != null)
			{
				foreach (var processor in _subJobProcessors)
				{
					processor.BeginStop();
				}

				foreach (var processor in _subJobProcessors)
				{
					int instNum = processor.InstanceNum;
					processor.Stop();
					Console.WriteLine($"SubJobProcessor for instance: {instNum} has been disposed.");
				}
			}

			if(_jobReplayProcessor != null)
			{
				_jobReplayProcessor.BeginStop();
				_jobReplayProcessor.Stop();
				Console.WriteLine($"The JobReplayProcessor has been disposed.");
			}

			foreach (var job in _jobs.Values)
			{
				job.Close(deleteRepo: false);
			}

			_jobs.Clear();

			_outQ.Dispose();
		}

		private void HandleRequestMessage(FJobRequest jobRequest, string requestMsgId)
		{
			try
			{
				switch (jobRequest.RequestType)
				{
					case FJobRequestType.Generate:
						Console.WriteLine($"Starting Job: {jobRequest.JobId}.");
						Job job = new Job(jobRequest, requestMsgId);

						// Send all blocks on file.
						_replayWorkQueue.Add(job);

						// Add job to list of jobs from which new "build block" operations are queued.
						SubmitJob(job);

						Console.WriteLine($"The job has been started. Job: {jobRequest.JobId}.");
						break;

					case FJobRequestType.Replay:
						Console.WriteLine($"\n\nReplaying Job: {jobRequest.JobId}.");
						ReplayExistingJob(jobRequest.JobId);
						break;

					case FJobRequestType.GetHistogram:
						Console.WriteLine($"Handling GetHistogram Job: {jobRequest.JobId}.");
						FHistorgram fHistorgram = GetHistogram(jobRequest);

						Console.WriteLine($"Sending GetHistogram Result for Job: {jobRequest.JobId}.");
						SendHistogram(fHistorgram, requestMsgId);
						break;

					//case FJobRequestType.IncreaseInterations:
					//	break;

					case FJobRequestType.TransformCoords:
						Console.WriteLine($"Handling Transform Coords Job: {jobRequest.JobId}.");
						FCoordsResult fCoordsResult = GetNewCoords(jobRequest);
						SendFCoordsResult(fCoordsResult, requestMsgId);
						break;

					case FJobRequestType.Delete:
						bool deleteJob = jobRequest.Name.ToLowerInvariant() == "deljob";

						string delClause = deleteJob ? $". Deleting the repo" : null;
						Console.WriteLine($"Cancelling Job: {jobRequest.JobId}{delClause}.");

						CancelJob(jobRequest.JobId, deleteJob);
						Console.WriteLine($"The Job has been stopped Job: {jobRequest.JobId}.");
						break;

					default:
						Console.WriteLine($"Ignoring JobRequest with type: {jobRequest.RequestType} with JobId: {jobRequest.JobId}.");
						break;
				}
			}
			catch(Exception e)
			{
				Debug.WriteLine($"Got an exception while processing a request message. The error is {e.Message}.");
			}
		}

		public void Stop()
		{
			_cts.Cancel();
			_haveWork.Set();
		}

		private void ProcessJobs(BlockingCollection<SubJob> workQueue, CancellationToken ct)
		{
			do
			{
				Job job = GetNextJob(ct);
				if (ct.IsCancellationRequested) break;

				SubJob subJob = job.GetNextSubJob();
				if (subJob != null)
				{
					//Debug.WriteLine($"Adding subJob for JobId:{subJob.ParentJob.JobId}, the pos is {subJob.MapSectionWorkRequest.MapSection.SectionAnchor.X},{subJob.MapSectionWorkRequest.MapSection.SectionAnchor.Y}.");
					workQueue.Add(subJob, ct);
				}
			}
			while (true);
		}

		private void SendProcessor(BlockingCollection<SubJob> sendQueue/*, MessageQueue outQ*/, CancellationToken ct)
		{
			try
			{
				while (!ct.IsCancellationRequested)
				{
					if (sendQueue.TryTake(out SubJob subJob, -1, ct))
					{
						Job parentJob = subJob.ParentJob;
						if (!parentJob.Closed)
						{
							parentJob.DecrementSubJobsRemainingToBeSent();

							bool isFinalSubJob = subJob.ParentJob.IsLastSubJob;
							//Debug.WriteLine($"Sending subjob with x: {subJob.result.MapSection.SectionAnchor.X} " +
							//	$"and y: {subJob.result.MapSection.SectionAnchor.Y}. " +
							//	$"It has {subJob.result.ImageData.Length} count values.");

							FJobResult fJobResult = subJob.GetResultFromSubJob(isFinalSubJob);

							// Mark the SubJobResult as free.
							subJob.SubJobResult.IsFree = true;

							Message r = new Message(fJobResult)
							{
								CorrelationId = parentJob.RequestMsgId
							};

							Console.WriteLine($"Sending corId = {FMsgId(parentJob.RequestMsgId)}, instance:{subJob.SubJobResult.ProcessorInstanceName} for id:{subJob.ParentJob.JobId} with {subJob.Position}.");
							outQ.Send(r);
						}
						else
						{
							// Mark the SubJobResult as free.
							subJob.SubJobResult.IsFree = true;
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine("Send Queue Consuming Enumerable canceled.");
				throw;
			}
			catch (InvalidOperationException)
			{
				Debug.WriteLine("Send Queue Consuming Enumerable completed.");
				throw;
			}
		}

		private string FMsgId(string mId)
		{
			return mId.Substring(mId.Length - 5);
		}

		private Job GetNextJob(CancellationToken cts)
		{
			Job result = null;
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
						Job[] jobs = _jobs.Values.Where(j => !j.IsCompleted).ToArray();

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

		#region Replay Method

		private void ReplayExistingJob(int jobId)
		{
			if(TryGetJob(jobId, out Job job))
			{
				_replayWorkQueue.Add(job);
			}
			else
			{
				Console.WriteLine($"Could not find existing job with Id: {jobId} to replay.");
			}
		}

		#endregion

		#region GetHistogram Methods

		private FHistorgram GetHistogram(FJobRequest fJobRequest)
		{
			int jobId = fJobRequest.JobId;
			if (TryGetJob(jobId, out Job job))
			{
				IDictionary<int, int> h = job.GetHistogram();
				FHistorgram result = new FHistorgram(jobId, h);
				return result;
			}
			else
			{
				return null;
			}
		}

		private void SendHistogram(FHistorgram fHistorgram, string requestMsgId)
		{
			using (MessageQueue outQ = MqHelper.GetQ(OUTPUT_HIST_Q_PATH, QueueAccessMode.Send, null, null))
			{
				Message r = new Message(fHistorgram)
				{
					CorrelationId = requestMsgId
				};

				Console.WriteLine($"Sending a Histogram response message with corId = {requestMsgId}.");
				outQ.Send(r);
			}
		}

		#endregion

		#region Transform Coords Methods

		private FCoordsResult GetNewCoords(FJobRequest fJobRequest)
		{
			using(CoordsMath cm = new CoordsMath())
			{
				FCoordsResult result = cm.GetNewCoords(fJobRequest);
				return result;
			}
		}

		private void SendFCoordsResult(FCoordsResult fCoordsResult, string requestMsgId)
		{
			using (MessageQueue outQ = MqHelper.GetQ(OUTPUT_COORDS_Q_PATH, QueueAccessMode.Send, null, null))
			{ 
				Message r = new Message(fCoordsResult)
				{
					CorrelationId = requestMsgId
				};

				Console.WriteLine($"Sending a Coords response message with corId = {requestMsgId}.");
				outQ.Send(r);
			}
		}

		#endregion

		#region Send Test Requests

		public async Task SendTestRequestsAsync()
		{
			//string eStr = "2.718281828459045235360287471352662498";

			//Dd ddE = new Dd(eStr);

			//string s = ddE.GetStringVal();

			//Dd temp = new Dd(2.056789e-12);
			//string s2 = temp.GetStringVal();

			// Wait for 2 seconds before beginning.
			await Task.Delay(2 * 1000);

			//using (MessageQueue outQ = MqHelper.GetQ(INPUT_Q_PATH, QueueAccessMode.Send, null, null))
			//{
			//	//bool testASI = outQ.DefaultPropertiesToSend.AttachSenderId;

			//	outQ.Send(CreateJobRequest(0));

			//	//outQ.Send(CreateJobRequestSimple(0));
			//	//await Task.Delay(2 * 1000);
			//	//outQ.Send(CreateJobRequestOV(1));

			//	//await Task.Delay(2 * 1000);
			//	//outQ.Send(FJobRequest.CreateGetHistogramRequest(0));



			//	//await Task.Delay(45 * 1000);
			//	//outQ.Send(FJobRequest.CreateDeleteRequest(0, deleteRepo:false));
			//	//outQ.Send(CreateJobRequest(1));
			//}

			return;
		}

		private FJobRequest CreateJobRequestSimple(int jobId)
		{
			FJobRequest result = new FJobRequest(
				jobId,
				"simple",
				FJobRequestType.Generate,
				//new Coords("-2", "1", "-1", "1"),
				//new Coords("-1.3", "-1.0", "0.5", "0.7"),
				new MSS.Types.MSetOld.ApCoords("0.03", "0.06", "0.05", "0.07"),

				new MSS.Types.RectangleInt(new MSS.Types.PointInt(0, 0), new MSS.Types.SizeInt(1, 1)),
				new MSS.Types.SizeInt(100, 100),
				50);

			return result;
		}

		private FJobRequest CreateJobRequest(int jobId)
		{
			string sx = "-1.7857676027665607066624802717953e+00";
			string ex = "-1.7857676027665530776998846126562e+00";
			string sy = "2.6144131561272207052702956846845e-06";
			string ey = "2.6144131613543258281816096430700e-06";

			FJobRequest result = new FJobRequest(
				jobId,
				"hiRez16test",
				FJobRequestType.Generate,
				new MSS.Types.MSetOld.ApCoords(sx, ex, sy, ey),
				new MSS.Types.RectangleInt(new MSS.Types.PointInt(10, 7), new MSS.Types.SizeInt(9, 6)),
				new MSS.Types.SizeInt(12600, 8400),
				7000); 

			return result;
		}

		private FJobRequest CreateJobRequestOV(int jobId)
		{
			string sx = "-1.7857676027665607066624802717953e+00";
			string ex = "-1.7857676027665530776998846126562e+00";
			string sy = "2.6144131561272207052702956846845e-06";
			string ey = "2.6144131613543258281816096430700e-06";

			FJobRequest result = new FJobRequest(
				jobId,
				"hiRez16test_ov",
				FJobRequestType.Generate,
				new MSS.Types.MSetOld.ApCoords(sx, ex, sy, ey),
				new MSS.Types.RectangleInt(new MSS.Types.PointInt(0, 0), new MSS.Types.SizeInt(3, 2)),
				//new MqMessages.SizeInt(230, 160),
				new MSS.Types.SizeInt(300, 200),

				300);

			return result;
		}

		//private async Task HandleJobResponses(string requestMsgId, CancellationToken cToken)
		//{
		//	Type[] rTtypes = new Type[] { typeof(FJobResult) };
		//	MessagePropertyFilter mpf = new MessagePropertyFilter
		//	{
		//		Body = true,
		//		Id = true,
		//		CorrelationId = true
		//	};

		//	using (MessageQueue inQ = MqHelper.GetQ(OUTPUT_Q_PATH, QueueAccessMode.Receive, rTtypes, mpf))
		//	{
		//		while (!cToken.IsCancellationRequested)
		//		{
		//			Debug.WriteLine($"Looking for a response message with corId = {requestMsgId}.");
		//			Message m = await MqHelper.ReceiveMessageByCorrelationIdAsync(inQ, requestMsgId, WaitDuration);
		//			if (m != null)
		//			{
		//				Debug.WriteLine("Received a response message.");
		//				try
		//				{
		//					FJobResult test = (FJobResult)m.Body;
		//					Debug.WriteLine($"The message is {test.JobId}; {m.CorrelationId}.");
		//				}
		//				catch (Exception e)
		//				{
		//					Debug.WriteLine($"Got an exception while accessing a read message. The error is {e.Message}.");
		//				}
		//			}
		//			else
		//			{
		//				Debug.WriteLine("No response message present.");
		//			}
		//		}
		//	}

		//	Debug.WriteLine("Response Handler Ending.");
		//}

		#endregion

	}
}

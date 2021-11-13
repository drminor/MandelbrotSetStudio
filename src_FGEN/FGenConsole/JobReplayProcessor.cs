using qdDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FGenConsole
{
	internal class JobReplayProcessor
	{
		private readonly BlockingCollection<Job> _workQueue;
		private readonly BlockingCollection<SubJob> _sendQueue;

		private readonly CancellationTokenSource _cts;
		private Task _task;

		private readonly SubJobProcessorResultCache _resultCache;

		public JobReplayProcessor(BlockingCollection<Job> workQueue, BlockingCollection<SubJob> sendQueue)
		{
			_workQueue = workQueue;
			_sendQueue = sendQueue;
			_cts = new CancellationTokenSource();
			_task = null;
			_resultCache = new SubJobProcessorResultCache(FGenerator.BLOCK_WIDTH * FGenerator.BLOCK_HEIGHT, -2, 10);
		}


		public void Start()
		{
			_task = Task.Run(() =>
			{
				WorkProcessor(_workQueue, _sendQueue, _cts.Token);
			}, _cts.Token);
		}

		public void BeginStop()
		{
			_cts.Cancel();
		}

		public void Stop()
		{
			//_cts.Cancel();
			try
			{
				if(_task != null)
				{
					if(!_task.Wait(120 * 1000))
					{
						Debug.WriteLine($"Could not stop JobReplayProcessor.");
					}
					else
					{
						Debug.WriteLine($"SubJobProcessor has stopped.");
					}
				}
			}
			catch (TaskCanceledException)
			{
				Debug.WriteLine($"Got task cancellation exception when stopping the JobReplayProcessor.");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception when stopping the JobReplayProcessor. The error is {e.Message}.");
			}
		}

		private void WorkProcessor(BlockingCollection<Job> workQueue, BlockingCollection<SubJob> sendQueue, CancellationToken ct)
		{
			while (true)
			{
				try
				{
					Job job = _workQueue.Take(ct);
					job.ResetSubJobsRemainingToBeSent();
					List<KPoint> repoKeys = job.GetRepoKeysForJob();

					foreach (KPoint key in repoKeys)
					{
						SubJob subJob = new SubJob(job, key);
						HandleSubJob(subJob);
					}
				}
				catch (TaskCanceledException tce)
				{
					Console.WriteLine($"JobReplayProcessor got error: {tce.Message}.");
					break;
				}
				catch (OperationCanceledException oce)
				{
					Console.WriteLine($"JobReplayProcessor got error: {oce.Message}.");
					break;
				}
				catch (Exception e)
				{
					Console.WriteLine($"JobReplayProcessor got error: {e.Message}.");
					//break;
				}
			}
		}

		private void HandleSubJob(SubJob subJob)
		{
			SubJobResult subJobResult = _resultCache.GetEmptySubJobResult(readZValues: false);
			SubJobOperationType operationType = DetermineOperationType(subJob, subJobResult);

			if (operationType == SubJobOperationType.Fetch)
			{
				Console.WriteLine($"Sub Job Results were retreived from file. {GetDiagInfo(subJob)}");

				subJob.OperationType = operationType;
				subJob.SubJobResult = subJobResult;
				_sendQueue.Add(subJob);
			}
			else
			{
				// Since we are not using this SubJobResult, mark it as free
				subJobResult.IsFree = true;
			}
		}

		private SubJobOperationType DetermineOperationType(SubJob subJob, SubJobResult subJobResult)
		{
			SubJobOperationType result = SubJobOperationType.Unknown;

			if (subJob.ParentJob.Closed)
			{
				Debug.WriteLine($"Not Replaying Sub Job. {GetDiagInfo(subJob)}");
				return SubJobOperationType.None;
			}

			if (TryRetrieveWorkResultFromRepo(subJob, subJobResult))
			{
				uint targetIterations = subJob.ParentJob.FJobRequest.MaxIterations;
				if (subJobResult.IterationCount == 0 || subJobResult.IterationCount == targetIterations)
				{
					// The WorkResult read from file has the correct iteration count. (Or we are not tracking the iteration count.)
					//Console.WriteLine("Sub Job Results were retreived from file. {GetDiagInfo(subJob)}");
					result = SubJobOperationType.Fetch;
				}

				else if (subJobResult.IterationCount < targetIterations)
				{
					result = SubJobOperationType.IncreaseIterations;
				}
				else
				{
					result = SubJobOperationType.DecreaseIterations;
				}
			}
			else
			{
				result = SubJobOperationType.Build;
			}

			return result;
		}

		private bool TryRetrieveWorkResultFromRepo(SubJob subJob, SubJobResult subJobResult)
		{
			bool result = subJob.ParentJob.RetrieveWorkResultFromRepo(subJob.Position, subJobResult);
			return result;
		}

		private string GetDiagInfo(SubJob subJob)
		{
			string result = $"job:{subJob.ParentJob.JobId}, position:{subJob.Position}.";
			return result;
		}

	}
}

using qdDotNet;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FGenConsole
{
	internal class SubJobProcessor
	{
		private readonly BlockingCollection<SubJob> _workQueue;
		private readonly BlockingCollection<SubJob> _sendQueue;

		private readonly CancellationTokenSource _cts;
		private Task _task;

		private readonly SubJobProcessorResultCache _resultCache;

		public SubJobProcessor(BlockingCollection<SubJob> workQueue, BlockingCollection<SubJob> sendQueue, int instanceNum)
		{
			_workQueue = workQueue;
			_sendQueue = sendQueue;
			_cts = new CancellationTokenSource();
			_task = null;
			InstanceNum = instanceNum;
			_resultCache = new SubJobProcessorResultCache(FGenerator.BLOCK_WIDTH * FGenerator.BLOCK_HEIGHT, instanceNum, 10);
		}

		public int InstanceNum { get; }

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
						Debug.WriteLine($"Could not stop SubJobProcessor for instance: {InstanceNum}.");
					}
					else
					{
						Debug.WriteLine($"SubJobProcessor for instance: {InstanceNum} has stopped.");
					}
				}
			}
			catch (TaskCanceledException)
			{
				Debug.WriteLine($"Got task cancellation exception when stopping the SubJobProcessor for instance: {InstanceNum}.");
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception when stopping the SubJobProcessor for instance: {InstanceNum}. The error is {e.Message}.");
			}
		}

		private void WorkProcessor(BlockingCollection<SubJob> workQueue, BlockingCollection<SubJob> sendQueue, CancellationToken ct)
		{
			while (!ct.IsCancellationRequested)
			{
				try
				{
					SubJob subJob = _workQueue.Take(ct);

					SubJobResult subJobResult = _resultCache.GetEmptySubJobResult(readZValues: false);
					SubJobOperationType operationType = DetermineOperationType(subJob, subJobResult);

					if(operationType != SubJobOperationType.None && operationType != SubJobOperationType.Fetch)
					{
						subJob.OperationType = operationType;
						subJob.SubJobResult = subJobResult;
						if(ProcessSubJob(subJob, ct))
						{
							_sendQueue.Add(subJob);
						}
						else
						{
							// Since we are not using this SubJobResult, mark it as free
							subJob.SubJobResult.IsFree = true;
						}
					}
					else
					{
						// Since we are not using this SubJobResult, mark it as free
						subJobResult.IsFree = true;
					}
				}
				catch (TaskCanceledException tce)
				{
					Console.WriteLine($"Process SubJob, instance:{InstanceNum} got error: {tce.Message}.");
					break;
				}
				catch (OperationCanceledException oce)
				{
					Console.WriteLine($"Process SubJob, instance:{InstanceNum} got error: {oce.Message}.");
					break;
				}
				catch (Exception e)
				{
					Console.WriteLine($"Process SubJob, instance:{InstanceNum} got error: {e.Message}.");
					//break;
				}
			}
		}

		private SubJobOperationType DetermineOperationType(SubJob subJob, SubJobResult subJobResult)
		{
			SubJobOperationType result = SubJobOperationType.Unknown;

			if (subJob.ParentJob.Closed)
			{
				Debug.WriteLine($"Not Processing Sub Job. {GetDiagInfo(subJob)}");
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

	private bool ProcessSubJob(SubJob subJob, CancellationToken ct)
		{
			if (subJob.ParentJob.Closed)
			{
				Debug.WriteLine($"Not Processing Sub Job. {GetDiagInfo(subJob)}");
				return false;
			}

			uint targetIterations = subJob.ParentJob.FJobRequest.MaxIterations;
			switch (subJob.OperationType)
			{
				case SubJobOperationType.Unknown:
					throw new InvalidOperationException("Trying to process a subJob with op type = unknown.");
				case SubJobOperationType.Fetch:
					{
						// The WorkResult read from file has the correct iteration count. (Or we are not tracking the iteration count.)
						Console.WriteLine($"Sub Job Results were retreived from file. {GetDiagInfo(subJob)}");
						break;
					}
				case SubJobOperationType.Build:
					{
						Console.WriteLine($"Sub Job Results are being calculated from zero. {GetDiagInfo(subJob)}");

						//SubJobResult subJobResult = _resultCache.GetEmptySubJobResult(readZValues: false);

						//if (FillCountsForBlock(subJob, ct, out TimeSpan elasped))
						//{
						//	subJob.SubJobResult.IsFree = true;
						//	subJob.SubJobResult = subJobResult;

						//	if (FillCountsForBlock2(subJob, ct, out TimeSpan elasped2))
						//	{
						//		TimeSpan diff = elasped - elasped2;
						//		string et = diff.ToString(@"mm\:ss\.ffff");

						//		Console.WriteLine($"Using new ver took {et} less time.");

						//		subJob.SubJobResult.IterationCount = targetIterations;
						//		subJob.ParentJob.WriteWorkResult(subJob, overwriteResults: false);
						//	}
						//}
						//break;

						if (FillCountsForBlock(subJob, ct, out TimeSpan elasped2))
						{
							subJob.SubJobResult.IterationCount = targetIterations;
							subJob.ParentJob.WriteWorkResult(subJob, overwriteResults: false);
						}
						break;
					}
				case SubJobOperationType.IncreaseIterations:
					{
						Console.WriteLine($"Increasing the iteration count. {GetDiagInfo(subJob)}");

						// Mark the current subJobResult as free.
						subJob.SubJobResult.IsFree = true;

						// Fetch a new one with ZValues included.
						SubJobResult subJobResult = _resultCache.GetEmptySubJobResult(readZValues: true);
						TryRetrieveWorkResultFromRepo(subJob, subJobResult);
						subJob.SubJobResult = subJobResult;

						// Use the current work results to continue calculations to create
						// a result with the target iteration count.
						if (FillCountsForBlock(subJob, ct, out TimeSpan elasped2))
						{
							subJobResult.IterationCount = targetIterations;
							subJob.ParentJob.WriteWorkResult(subJob, overwriteResults: true);
						}
						break;
					}
				case SubJobOperationType.DecreaseIterations:
					throw new InvalidOperationException("Cannot reduce the number of iterations of an existing job.");
				case SubJobOperationType.None:
					throw new InvalidOperationException("Trying to process a subJob with op type = none.");
				default:
					throw new InvalidOperationException("Trying to process a subJob with an unrecognized op type.");
			}

			return true;
		}

		//private bool FillCountsForBlock_OLD(SubJob subJob, CancellationToken ct, out TimeSpan elasped)
		//{
		//	DateTime t0 = DateTime.Now;
		//	try
		//	{
		//		SubJobResult subJobResult = subJob.SubJobResult;

		//		uint[] counts = subJobResult.Counts;
		//		bool[] doneFlags = subJobResult.DoneFlags;
		//		double[] zValues = subJobResult.ZValues;

		//		for (int yPtr = 0; yPtr < FGenerator.BLOCK_WIDTH; yPtr++)
		//		{
		//			if (ct.IsCancellationRequested || subJob.ParentJob.Closed) break;
		//			subJob.ParentJob.FGenerator.FillXCounts(subJob.Position.GetPointInt(), ref counts, ref doneFlags, ref zValues, yPtr);
		//			//subJob.ParentJob.FGenerator.FillXCountsTest(subJob.Position.GetPointInt(), ref counts, ref doneFlags, ref zValues, yPtr);
		//		}

		//		subJobResult.Counts = counts;
		//		subJobResult.DoneFlags = doneFlags;
		//		subJobResult.ZValues = zValues;
		//		ReportElaspedTime(t0, $"Block: {subJob.Position}");

		//		elasped = DateTime.Now - t0;

		//		return !ct.IsCancellationRequested && !subJob.ParentJob.Closed;
		//	}
		//	catch (Exception e)
		//	{
		//		Debug.WriteLine($"Got exception while filling the XCounts. The error is {e.Message}.");
		//		throw;
		//	}
		//}

		private bool FillCountsForBlock(SubJob subJob, CancellationToken ct, out TimeSpan elasped)
		{
			DateTime t0 = DateTime.Now;
			try
			{
				SubJobResult subJobResult = subJob.SubJobResult;

				uint[] counts = subJobResult.Counts;
				bool[] doneFlags = subJobResult.DoneFlags;
				double[] zValues = subJobResult.ZValues;

				//for (int yPtr = 0; yPtr < FGenerator.BLOCK_WIDTH; yPtr++)
				//{
				//	if (ct.IsCancellationRequested || subJob.ParentJob.Closed) break;
				//	subJob.ParentJob.FGenerator.FillXCounts2(subJob.Position.GetPointInt(), ref counts, ref doneFlags, ref zValues, yPtr);
				//	//subJob.ParentJob.FGenerator.FillXCountsTest(subJob.Position.GetPointInt(), ref counts, ref doneFlags, ref zValues, yPtr);
				//}

				subJob.ParentJob.FGenerator.FillCounts(subJob.Position.GetPointInt(), ref counts, ref doneFlags, ref zValues);

				subJobResult.Counts = counts;
				subJobResult.DoneFlags = doneFlags;
				subJobResult.ZValues = zValues;
				ReportElaspedTime(t0, $"Block: {subJob.Position}");

				elasped = DateTime.Now - t0;

				return !(ct.IsCancellationRequested || subJob.ParentJob.Closed);
			}
			catch (Exception e)
			{
				Debug.WriteLine($"Got exception while filling the XCounts. The error is {e.Message}.");
				throw;
			}
		}

		private void ReportElaspedTime(DateTime start, string msg)
		{
			TimeSpan x = DateTime.Now - start;
			string et = x.ToString(@"mm\:ss\.ff");
			Console.WriteLine($"{msg} took {et}.");
		}

		private bool TryRetrieveWorkResultFromRepo(SubJob subJob, SubJobResult subJobResult)
		{
			bool result = subJob.ParentJob.RetrieveWorkResultFromRepo(subJob.Position, subJobResult);
			return result;
		}

		private string GetDiagInfo(SubJob subJob)
		{
			string result = $"inst:{InstanceNum}, job:{subJob.ParentJob.JobId}, position:{subJob.Position}.";
			return result;
		}

	}
}

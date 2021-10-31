using FSTypes;
using MFile;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace MClient
{
	internal class SubJobProcessor
	{
		private readonly BlockingCollection<SubJob> _workQueue;
		private readonly BlockingCollection<SubJob> _sendQueue;

		private readonly MapCalculator _mapCalculator;

		private readonly CancellationTokenSource _cts;
		private Task _task;

		public SubJobProcessor(BlockingCollection<SubJob> workQueue, BlockingCollection<SubJob> sendQueue)
		{
			_mapCalculator = new MapCalculator();
			_workQueue = workQueue;
			_sendQueue = sendQueue;
			_cts = new CancellationTokenSource();
		}

		public void Start()
		{
			_task = Task.Run(() => WorkProcessor(_workQueue, _sendQueue, _cts.Token), _cts.Token);
		}

		public void Stop()
		{
			_cts.Cancel();
		}

		private void WorkProcessor(BlockingCollection<SubJob> workQueue, BlockingCollection<SubJob> sendQueue, CancellationToken ct)
		{
			try
			{
				while (!ct.IsCancellationRequested)
				{
					workQueue.TryTake(out SubJob subJob, -1, ct);
					MapSectionResult msr = ProcessSubJob(subJob);
					if (msr != null)
					{
						subJob.MapSectionResult = msr;
						_sendQueue.Add(subJob);
					}
				}
			}
			catch (OperationCanceledException)
			{
				Debug.WriteLine("SubJobProcessor canceled.");
				//throw;
			}
			catch (InvalidOperationException ioe)
			{
				Debug.WriteLine($"SubJobProcessor completed. The error is {ioe.Message}.");
				//throw;
			}
		}

		private MapSectionResult ProcessSubJob(SubJob subJob)
		{
			if (subJob.ParentJob.CancelRequested)
			{
				Debug.WriteLine("Not Processing Sub Job.");
				return null;
			}

			if (subJob.ParentJob is not Job localJob)
			{
				throw new InvalidOperationException("When processing a subjob, the parent job must be implemented by the Job class.");
			}


			MapSectionWorkRequest mswr = subJob.MapSectionWorkRequest;

			KPoint key = new(mswr.HPtr, mswr.VPtr);
			MapSectionWorkResult workResult = RetrieveWorkResultFromRepo(key, localJob, readZValues: false);

			MapSectionResult result;

			if (workResult == null)
			{
				result = CalculateMapValues(subJob, localJob, ref workResult);
			}
			else
			{
				if(workResult.IterationCount == 0 || workResult.IterationCount == mswr.MaxIterations)
				{
					// The WorkResult read from file has the correct iteration count. (Or we are not tracking the interation count.)
					result = new MapSectionResult(localJob.JobId, mswr.MapSection, workResult.Counts);
				}
				else if (workResult.IterationCount < mswr.MaxIterations)
				{
					// Fetch the entire WorkResult with ZValues
					workResult = RetrieveWorkResultFromRepo(key, localJob, readZValues: true);

					// Use the current work results to continue calculations to create
					// a result with the target iteration count.
					result = CalculateMapValues(subJob, localJob, ref workResult);
				}
				else
				{
					throw new InvalidOperationException("Cannot reduce the number of iterations of an existing job.");
				}
			}

			return result;
		}

		private static bool CompareZResults(DPoint[] zVals1, DPoint[] zVals2)
		{
			for(int i = 0; i < zVals1.Length; i++)
			{
				double x = zVals1[i].X;
				double x2 = zVals2[i].X;
				if (x != x2)
				{
					Debug.WriteLine($"Zvals at index:{i} are {x} and {x2}.");
					//return false;
				}
			}

			return true;
		}

		private MapSectionResult CalculateMapValues(SubJob subJob, Job localJob, ref MapSectionWorkResult workResult)
		{
			MapSectionWorkRequest mswr = subJob.MapSectionWorkRequest;

			bool overwriteResults;
			if (workResult == null)
			{
				workResult = BuildInitialWorkingValues(mswr);
				overwriteResults = false;
			}
			else
			{
				overwriteResults = true;
			}

			double[] xValues = localJob.SamplePoints.XValueSections[mswr.HPtr];
			double[] yValues = localJob.SamplePoints.YValueSections[mswr.VPtr];

			//DateTime t0 = DateTime.Now;
			//workResult = _mapCalculator.GetWorkingValues(xValues, yValues, mswr.MaxIterations, workResult);
			//DateTime t1 = DateTime.Now;

			//int[] testCounts = new int[10000];
			//DateTime t2 = DateTime.Now;

			//_mapCalculator.ComputeApprox(xValues, yValues, mswr.MaxIterations, testCounts);
			//List<int> misMatcheIndexes = GetMismatchCount(workResult.Counts, testCounts, out double avgGap);
			//DateTime t3 = DateTime.Now;

			//Debug.WriteLine($"Block: v={mswr.VPtr}, h={mswr.HPtr} has {misMatcheIndexes.Count} mis matches, avgGap={avgGap} and avg {GetAverageCntValue(workResult.Counts)}.");
			//Debug.WriteLine($"Standard: {GetElaspedTime(t0, t1)}, Approx: {GetElaspedTime(t2, t3)}.");

			//_mapCalculator.ComputeApprox(xValues, yValues, mswr.MaxIterations, workResult.Counts);
			workResult = _mapCalculator.GetWorkingValues(xValues, yValues, mswr.MaxIterations, workResult);

			KPoint key = new(mswr.HPtr, mswr.VPtr);
			localJob.WriteWorkResult(key, workResult, overwriteResults);

			MapSectionResult msr = new(localJob.JobId, mswr.MapSection, workResult.Counts);

			return msr;
		}

		private static string GetElaspedTime(DateTime start, DateTime end)
		{
			TimeSpan x = end - start;
			string et = x.ToString(@"mm\:ss\.ff");
			return et;
		}

		private static List<int> GetMismatchCount(int[] a, int[] b, out double avgGap)
		{
			double tGap = 0;
			avgGap = 0;
			List<int> result = new();

			int i = 0;
			for (int yPtr = 0; yPtr < 99; yPtr++)
			{
				for (int xPtr = 0; xPtr < 99; xPtr++)
				{
					int af = a[i] / 10000;
					if (af != b[i])
					{
						result.Add(i);
						tGap += Math.Abs(af - b[i]);
					}
					i++;
				}
				i++; // skip over the last column of each row.
 			}

			avgGap = tGap / (i - 1);

			return result;
		}

		private static double GetAverageCntValue(int[] cnts)
		{
			double acc = 0;
			for(int i = 0; i < cnts.Length; i++)
			{
				acc += cnts[i] / 10000;
			}

			return acc / cnts.Length;
		}

		private MapSectionWorkResult RetrieveWorkResultFromRepo(KPoint key, Job localJob, bool readZValues)
		{
			MapSectionWorkResult workResult = GetEmptyResult(readZValues, Job.SECTION_WIDTH, Job.SECTION_HEIGHT);

			if (localJob.RetrieveWorkResultFromRepo(key, workResult))
			{
				return workResult;
			}
			else
			{
				return null;
			}
		}

		private MapSectionWorkResult _emptyResult = null;
		private MapSectionWorkResult _emptyResultWithZValues = null;

		private MapSectionWorkResult GetEmptyResult(bool readZValues, int jobSectionWidth, int jobSectionHeight)
		{
			//if (area.Size.W != jobSectionWidth || area.Size.H != jobSectionHeight)
			//{
			//	Debug.WriteLine("Wrong Area.");
			//}

			if(readZValues)
			{
				if (_emptyResultWithZValues == null)
				{
					_emptyResultWithZValues = new MapSectionWorkResult(jobSectionWidth * jobSectionHeight, hiRez: false, includeZValuesOnRead: true);
				}
				return _emptyResultWithZValues;
			}
			else
			{
				if (_emptyResult == null)
				{
					_emptyResult = new MapSectionWorkResult(jobSectionWidth * jobSectionHeight, hiRez: false, includeZValuesOnRead: false);
				}
				return _emptyResult;
			}
		}

		private static MapSectionWorkResult BuildInitialWorkingValues(MapSectionWorkRequest mswr)
		{
			int width = mswr.MapSection.RectangleInt.Width;
			int height = mswr.MapSection.RectangleInt.Height;

			int len = width * height;

			int[] counts = new int[len];
			bool[] doneFlags = new bool[len];
			DPoint[] zValues = new DPoint[len];

			for (int ptr = 0; ptr < len; ptr++)
			{
				zValues[ptr] = new DPoint(0, 0);
			}

			MapSectionWorkResult result = new MapSectionWorkResult(counts, mswr.MaxIterations, zValues, doneFlags);
			return result;
		}


	}
}

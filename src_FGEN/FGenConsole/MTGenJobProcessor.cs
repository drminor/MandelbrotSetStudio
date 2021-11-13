using MqMessages;
using qdDotNet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Messaging;
using System.Threading;
using System.Threading.Tasks;
using PointInt = qdDotNet.PointInt;

namespace FGenConsole
{
	class MTGenJobProcessor
	{
		//private readonly BlockingCollection<int> _lineQ;
		private readonly BlockingCollection<PointInt> _subJobQ;

		private readonly List<Tuple<JobProcessor2, Task>> _processorTasks;
		private readonly CancellationTokenSource _cts;


		public MTGenJobProcessor(
			FJobRequest fJobRequest,
			string requestMsgId,
			//MessageQueue outQ,
			int threadCount
			)
		{
			_cts = new CancellationTokenSource();
			//_lineQ = new BlockingCollection<int>();
			_subJobQ = new BlockingCollection<PointInt>();
			_processorTasks = new List<Tuple<JobProcessor2, Task>>();
			for(int cntr = 0; cntr < threadCount; cntr++)
			{
				_processorTasks.Add(BuildLineProcessor(fJobRequest, cntr, requestMsgId, _subJobQ/*, outQ*/, _cts.Token));
			}

			//AddLines(fJobRequest.Area.Size.H, _lineQ);
			LoadSubJobs(fJobRequest.Area.Size, _subJobQ);
		}

		private void LoadSubJobs(MSS.Types.SizeInt blockExtent, BlockingCollection<PointInt> subJobQ)
		{
			for(int j = 0; j < blockExtent.Height; j++)
			{
				for (int i = 0; i < blockExtent.Width; i++)
				{
					subJobQ.Add(new PointInt(i, j));
				}
			}
		}

		//private void AddLines(int lineCnt,  BlockingCollection<int> lineQ)
		//{
		//	int cnt = (lineCnt + 1)/ 2;

		//	int lLine;
		//	int hLine;
		//	if (lineCnt % 2 != 0)
		//	{
		//		_lineQ.Add(cnt);
		//		lLine = cnt - 1;
		//		hLine = cnt + 1;
		//		cnt--;
		//	}
		//	else
		//	{
		//		lLine = cnt;
		//		hLine = cnt + 1;
		//	}

		//	for (int cntr = 0; cntr < cnt; cntr++)
		//	{
		//		_lineQ.Add(lLine--);
		//		_lineQ.Add(hLine++);
		//	}
		//}

		Tuple<JobProcessor2, Task> BuildLineProcessor(FJobRequest fJobRequest, int instanceNum, string requestMsgId, BlockingCollection<PointInt> subJobQ,
			/*MessageQueue outQ,*/ CancellationToken ct)
		{
			JobProcessor2 jobProcessor = CreateJobProcessor(fJobRequest, instanceNum);
			Task t = Task.Run(() => ProcessSubJobs(jobProcessor, requestMsgId, subJobQ/*, outQ*/, ct));

			Tuple<JobProcessor2, Task> result = new Tuple<JobProcessor2, Task>(jobProcessor, t);
			return result;
		}

		private void ProcessSubJobs(JobProcessor2 jobProcessor, string requestMsgId, BlockingCollection<PointInt> subJobQ,
			/*MessageQueue outQ, */CancellationToken ct)
		{
			while(!ct.IsCancellationRequested)
			{
				try
				{
					if(subJobQ.TryTake(out PointInt position, Timeout.Infinite, ct))
					{
						ProcessSubJob(jobProcessor, position, requestMsgId/*, outQ*/);
					}
				}
				catch (TaskCanceledException)
				{
					break;
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception e)
				{
					Console.WriteLine($"Process Lines, instance:{jobProcessor.InstanceNum} got error: {e.Message}.");
				}
			}
		}

		private void ProcessSubJob(JobProcessor2 jobProcessor, PointInt position, string requestMsgId/*, MessageQueue outQ*/)
		{
			int jobId = jobProcessor.FGenJob.JobId;

			//int w = jobProcessor.FGenJob.Area.W();

			//uint[] counts = jobProcessor.GetCountsForLine(linePtr);

			SubJobResult subJobResult = jobProcessor.GetEmptySubJobResult();

			jobProcessor.FillCountsForBlock(position, subJobResult);
			//Debug.WriteLine(ReportSampleOfCounts(subJobResult.Counts));

			FJobResult fJobResult = GetResultFromSubJob(jobId, position, subJobResult);

			//Message r = new Message(fJobResult)
			//{
			//	CorrelationId = requestMsgId
			//};

			//Console.WriteLine($"Sending a response message with corId = {FMsgId(requestMsgId)}, instance:{jobProcessor.InstanceNum}.");
			//outQ.Send(r);
		}

		private FJobResult GetResultFromSubJob(int jobId, PointInt position, SubJobResult subJobResult)
		{
			MSS.Types.PointInt resultPos = new MSS.Types.PointInt(
				position.X() * FGenerator.BLOCK_WIDTH,
				position.Y() * FGenerator.BLOCK_HEIGHT
				);

			MSS.Types.SizeInt resultSize = new MSS.Types.SizeInt(FGenerator.BLOCK_WIDTH, FGenerator.BLOCK_HEIGHT);
			MSS.Types.RectangleInt area = new MSS.Types.RectangleInt(resultPos, resultSize);

			FJobResult fJobResult = new FJobResult(jobId, area, subJobResult.Counts, isFinalResult: false);
			return fJobResult;
		}

		private string FMsgId(string mId)
		{
			return mId.Substring(mId.Length - 5);
		}

		private JobProcessor2 CreateJobProcessor(FJobRequest fJobRequest, int instanceNum)
		{
			FGenJob job = CreateJob(fJobRequest);
			JobProcessor2 result = new JobProcessor2(job, instanceNum);
			return result;
		}

		private FGenJob CreateJob(FJobRequest fJobRequest)
		{
			PointDd start = new PointDd(new Dd(fJobRequest.Coords.StartingX), new Dd(fJobRequest.Coords.StartingY));
			PointDd end = new PointDd(new Dd(fJobRequest.Coords.EndingX), new Dd(fJobRequest.Coords.EndingY));

			qdDotNet.SizeInt samplePoints = new qdDotNet.SizeInt(fJobRequest.SamplePoints.Width, fJobRequest.SamplePoints.Height);
			qdDotNet.RectangleInt area = new qdDotNet.RectangleInt(
				new qdDotNet.PointInt(fJobRequest.Area.Point.X, fJobRequest.Area.Point.Y),
				new qdDotNet.SizeInt(fJobRequest.Area.Size.Width, fJobRequest.Area.Size.Height));

			FGenJob fGenJob = new FGenJob(fJobRequest.JobId, start, end, samplePoints, fJobRequest.MaxIterations, area);

			return fGenJob;
		}

		public bool Stop()
		{
			bool result = true;
			_cts.Cancel();

			foreach(Tuple<JobProcessor2, Task> pAndT in _processorTasks)
			{
				try
				{
					pAndT.Item2.Wait();
					//pAndT.Item1.Dispose();
				}
				catch(AggregateException ae)
				{
					Console.WriteLine($"Got error while stopping the JobProcessor instance: {pAndT.Item1.InstanceNum}. The error message is {ae.Message}.");
					result = false;
				}
			}

			return result;
		}

		private string ReportSampleOfCounts(int[] counts)
		{
			string result = string.Empty;
			for (int ptr = 0; ptr < counts.Length; ptr += 5)
			{
				result += counts[ptr] + ", ";
			}

			return result;
		}

	}
}

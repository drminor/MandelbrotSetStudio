using Experimental.System.Messaging;
using MSS.Types;
using MqMessages;
using System;
using System.Diagnostics;

namespace MClient
{
	public class MqHistogram
	{
		public const string OUTPUT_Q_PATH = @".\private$\FGenJobs";
		public const string INPUT_COORDS_Q_PATH = @".\private$\FHistResults";
		private static readonly TimeSpan DefaultWaitDuration = TimeSpan.FromSeconds(30);

		public MqHistogram()
		{
			WaitDuration = DefaultWaitDuration;
		}

		public TimeSpan WaitDuration { get; set; }

		public Histogram GetHistogram(int jobId)
		{
			FJobRequest fJobRequest = FJobRequest.CreateGetHistogramRequest(jobId);
			string requestMsgId = SendJobToMq(fJobRequest);

			FHistorgram fHistorgram = GetResponseFromMq(requestMsgId);
			Histogram result = new(jobId, fHistorgram.GetValues(), fHistorgram.GetOccurances());
			return result;
		}

		private FHistorgram GetResponseFromMq(string requestMsgId)
		{
			using MessageQueue inQ = GetJobResponseQueue();
			Message m = MqHelper.GetMessageByCorId(inQ, requestMsgId, WaitDuration);

			if (m == null)
			{
				Debug.WriteLine("The FHistogram did not arrive.");
				return null;
			}

			Debug.WriteLine("Received a message.");
			FHistorgram result = (FHistorgram)m.Body;

			return result;
		}

		private static MessageQueue GetJobResponseQueue()
		{
			Type[] rTtypes = new Type[] { typeof(FHistorgram) };

			MessagePropertyFilter mpf = new()
			{
				Body = true,
				//Id = true,
				CorrelationId = true
			};

			MessageQueue result = MqHelper.GetQ(INPUT_COORDS_Q_PATH, QueueAccessMode.Receive, rTtypes, mpf);
			return result;
		}


		private static string SendJobToMq(FJobRequest fJobRequest)
		{
			using MessageQueue outQ = MqHelper.GetQ(OUTPUT_Q_PATH, QueueAccessMode.Send, null, null);
			Debug.WriteLine($"Sending request with JobId {fJobRequest.JobId} to output Q.");

			Message m = new(fJobRequest);
			outQ.Send(m);

			return m.Id;
		}

	}
}

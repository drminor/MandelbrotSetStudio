using Experimental.System.Messaging;
using FSTypes;
using MqMessages;
using System;
using System.Diagnostics;

namespace MClient
{
	public class CoordsMath
	{
		public const string OUTPUT_Q_PATH = @".\private$\FGenJobs";
		public const string INPUT_COORDS_Q_PATH = @".\private$\FCoordResults";

		private static readonly TimeSpan DefaultWaitDuration = TimeSpan.FromSeconds(10);

		private int _nextJobId;

		public CoordsMath()
		{
			WaitDuration = DefaultWaitDuration;
			_nextJobId = -1;
		}

		public TimeSpan WaitDuration { get; set; }

		public Coords DoOp(SCoordsWorkRequest sCoordsWorkRequest)
		{
			FJobRequest fJobRequest = CreateFJobRequest(sCoordsWorkRequest, ++_nextJobId);
			string requestMsgId = SendJobToMq(fJobRequest);

			Coords result = GetResponseFromMq(requestMsgId);
			return result;
		}

		private Coords GetResponseFromMq(string requestMsgId)
		{
			using MessageQueue inQ = GetJobResponseQueue();
			Message m = MqHelper.GetMessageByCorId(inQ, requestMsgId, WaitDuration);

			if (m == null)
			{
				Debug.WriteLine("The FCoordsResult did not arrive.");
				return null;
			}

			Debug.WriteLine("Received a message.");
			FCoordsResult jobResult = (FCoordsResult)m.Body;

			return jobResult.Coords;
		}

		private static MessageQueue GetJobResponseQueue()
		{
			Type[] rTtypes = new Type[] { typeof(FCoordsResult) };

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

		private static FJobRequest CreateFJobRequest(SCoordsWorkRequest sCoordsWorkRequest, int jobId)
		{
			Coords coords = sCoordsWorkRequest.Coords;
			SizeInt samplePoints = sCoordsWorkRequest.CanvasSize;
			RectangleInt area = sCoordsWorkRequest.MapSection;
			TransformType transformType = sCoordsWorkRequest.TransformType;

			string name = "CoordsRequest";
			var fJobRequest = new FJobRequest(jobId, name, FJobRequestType.TransformCoords, coords, area, samplePoints, 0, transformType);

			return fJobRequest;
		}

	}
}

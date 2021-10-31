using Experimental.System.Messaging;
using FSTypes;
using MapSectionRepo;
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

		public SCoords DoOp(SCoordsWorkRequest sCoordsWorkRequest)
		{
			FJobRequest fJobRequest = CreateFJobRequest(sCoordsWorkRequest, ++_nextJobId);
			string requestMsgId = SendJobToMq(fJobRequest);

			SCoords result = GetResponseFromMq(requestMsgId);
			return result;
		}

		private SCoords GetResponseFromMq(string requestMsgId)
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

			Coords coords = jobResult.Coords;

			SPoint leftBot = new(coords.StartX, coords.StartY);
			SPoint rightTop = new(coords.EndX, coords.EndY);
			SCoords result = new(leftBot, rightTop);

			return result;
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
			SCoords sCoords = sCoordsWorkRequest.SCoords;
			MqMessages.Coords coords = new(sCoords.LeftBot.X, sCoords.RightTop.X, sCoords.LeftBot.Y, sCoords.RightTop.Y);

			SizeInt samplePoints = sCoordsWorkRequest.CanvasSize;
			RectangleInt area = sCoordsWorkRequest.MapSection;

			string name = "CoordsRequest";
			FJobRequest fJobRequest = new(jobId, name, FJobRequestType.TransformCoords, coords, area, samplePoints, 0, sCoordsWorkRequest.TransformType);

			return fJobRequest;
		}

	}
}

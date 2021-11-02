using MSS.Types;
using System;

namespace MqMessages
{
	[Serializable]
	public class FJobRequest
	{
		public int JobId { get; set; }
		public string Name { get; set; }
		public Coords Coords { get; set; }
		public RectangleInt Area { get; set; }
		public SizeInt SamplePoints { get; set; }
		public uint MaxIterations { get; set; }
		public FJobRequestType RequestType { get; set; }
		public TransformType? TransformType { get; set; }

		public FJobRequest() { }

		public FJobRequest(int jobId, string name, FJobRequestType requestType, Coords coords, RectangleInt area, SizeInt samplePoints, uint maxIterations, TransformType? transformType = null)
		{
			JobId = jobId;
			Name = name;
			Coords = coords;
			Area = area;
			SamplePoints = samplePoints;
			MaxIterations = maxIterations;
			RequestType = requestType;
			TransformType = transformType;
		}

		public static FJobRequest CreateDeleteRequest(int jobId, bool deleteRepo)
		{
			string jobName = deleteRepo ? "deljob" : "cancel";
			return new FJobRequest(jobId, jobName, FJobRequestType.Delete, null, null, null, 0);
		}

		public static FJobRequest CreateGetHistogramRequest(int jobId)
		{
			return new FJobRequest(jobId, "GetHistogram", FJobRequestType.GetHistogram, null, null, null, 0);
		}

		public static FJobRequest CreateReplayRequest(int jobId)
		{
			return new FJobRequest(jobId, "Replay", FJobRequestType.Replay, null, null, null, 0);
		}
	}
}

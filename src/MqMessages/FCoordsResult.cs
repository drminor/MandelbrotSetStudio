using System;

namespace MqMessages
{
	[Serializable]
	public class FCoordsResult
	{
		public FCoordsResult()
		{
			JobId = -1;
			Coords = null;
		}

		public FCoordsResult(int jobId, Coords coords)
		{
			JobId = jobId;
			Coords = coords ?? throw new ArgumentNullException(nameof(coords));
		}

		public int JobId { get; set; }
		public Coords Coords { get; set; }
	}
}

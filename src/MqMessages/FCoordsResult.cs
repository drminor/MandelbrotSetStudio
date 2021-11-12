using MSS.Types.MSetOld;
using System;

namespace MqMessages
{
	[Serializable]
	public class FCoordsResult
	{
		public int JobId { get; set; }
		public ApCoords Coords { get; set; }

		public FCoordsResult()
		{
			JobId = -1;
			Coords = null;
		}

		public FCoordsResult(int jobId, ApCoords coords)
		{
			JobId = jobId;
			Coords = coords ?? throw new ArgumentNullException(nameof(coords));
		}

	}
}

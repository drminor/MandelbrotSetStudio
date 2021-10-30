using System;
using FSTypes;
using MapSectionRepo;
using MqMessages;

namespace MClient
{
	public class SCoordsWorkRequest
	{
		public TransformType TransformType;

		public SCoords SCoords;

		public CanvasSize CanvasSize;

		public MapSection MapSection;

		public int JobId;

		private SCoordsWorkRequest()
		{
			TransformType = TransformType.In;
			SCoords = null;
			CanvasSize = new CanvasSize(0, 0);
			MapSection = new MapSection(new Point(0, 0), new CanvasSize(0, 0));
			JobId = -1;
		}

		public SCoordsWorkRequest(TransformType transformType, SCoords sCoords, CanvasSize canvasSize, MapSection mapSection, int jobId)
		{
			TransformType = transformType;
			SCoords = sCoords ?? throw new ArgumentNullException(nameof(sCoords));
			CanvasSize = canvasSize ?? throw new ArgumentNullException(nameof(canvasSize));
			MapSection = mapSection ?? throw new ArgumentNullException(nameof(mapSection));
			JobId = jobId;
		}
	}

}

using System;
using MSS.Types;
using MqMessages;

namespace MClient
{
	public class SCoordsWorkRequest
	{
		public TransformType TransformType;

		public Coords Coords;

		/// <summary>
		/// The size of the entire job
		/// </summary>
		public SizeInt CanvasSize;

		/// <summary>
		/// The size and location of this request
		/// </summary>
		public RectangleInt MapSection;

		public int JobId;

		private SCoordsWorkRequest()
		{
			TransformType = TransformType.In;
			Coords = new Coords();
			CanvasSize = new SizeInt();
			MapSection = new RectangleInt();
			JobId = -1;
		}

		public SCoordsWorkRequest(TransformType transformType, Coords coords, SizeInt canvasSize, RectangleInt mapSection, int jobId)
		{
			TransformType = transformType;
			Coords = coords ?? throw new ArgumentNullException(nameof(coords));
			CanvasSize  = canvasSize ?? throw new ArgumentNullException(nameof(canvasSize));
			MapSection = mapSection ?? throw new ArgumentNullException(nameof(mapSection));
			JobId = jobId;
		}
	}

}

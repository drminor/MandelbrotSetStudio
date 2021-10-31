﻿using System;
using FSTypes;
using MqMessages;

namespace MClient
{
	public class SCoordsWorkRequest
	{
		public TransformType TransformType;

		public SCoords SCoords;

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
			SCoords = new SCoords();
			CanvasSize = new SizeInt();
			MapSection = new RectangleInt();
			JobId = -1;
		}

		public SCoordsWorkRequest(TransformType transformType, SCoords sCoords, SizeInt canvasSize, RectangleInt mapSection, int jobId)
		{
			TransformType = transformType;
			SCoords = sCoords ?? throw new ArgumentNullException(nameof(sCoords));
			CanvasSize  = canvasSize ?? throw new ArgumentNullException(nameof(canvasSize));
			MapSection = mapSection ?? throw new ArgumentNullException(nameof(mapSection));
			JobId = jobId;
		}
	}

}

﻿using System;
using MSS.Types;
using MqMessages;
using MSS.Types.MSetOld;

namespace MClient
{
	public class SCoordsWorkRequest
	{
		public TransformType TransformType;

		public ApCoords ApCoords;

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
			ApCoords = new ApCoords();
			CanvasSize = new SizeInt();
			MapSection = new RectangleInt();
			JobId = -1;
		}

		public SCoordsWorkRequest(TransformType transformType, ApCoords apCoords, SizeInt canvasSize, RectangleInt mapSection, int jobId)
		{
			TransformType = transformType;
			ApCoords = apCoords ?? throw new ArgumentNullException(nameof(ApCoords));
			CanvasSize  = canvasSize;
			MapSection = mapSection;
			JobId = jobId;
		}
	}

}

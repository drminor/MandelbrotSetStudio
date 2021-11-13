using MqMessages;
using MSS.Types;
using MSS.Types.MSetOld;
using qdDotNet;
using System;
using System.Diagnostics;

namespace FGenConsole
{
	class CoordsMath : IDisposable
	{
		private readonly FCoordsMath _fCoordsMath;

		public CoordsMath()
		{
			_fCoordsMath = new FCoordsMath();
		}

		public FCoordsResult GetNewCoords(FJobRequest fJobRequest)
		{
			FCoordsResult result = null;
			if(fJobRequest.RequestType != FJobRequestType.TransformCoords)
			{
				throw new ArgumentException("The request type must be TransformCoords.");
			}

			if(!fJobRequest.TransformType.HasValue)
			{
				throw new ArgumentException("The transform type cannot be null.");
			}

			MCoordsDd curCoords = GetMCoords(fJobRequest.Coords);

			switch (fJobRequest.TransformType)
			{
				case TransformType.In:
					qdDotNet.SizeInt sizeInt = new qdDotNet.SizeInt(fJobRequest.SamplePoints.Width, fJobRequest.SamplePoints.Height);

					qdDotNet.RectangleInt area = new qdDotNet.RectangleInt(
						new qdDotNet.PointInt(fJobRequest.Area.Point.X, fJobRequest.Area.Point.Y),
						new qdDotNet.SizeInt(fJobRequest.Area.Size.Width, fJobRequest.Area.Size.Height));

					MCoordsDd newCoords = _fCoordsMath.ZoomIn(curCoords, sizeInt, area);
					result = GetResult(fJobRequest.JobId, newCoords);
					break;

				case TransformType.Out:
					double amount = GetAmount(fJobRequest.Area.Point.X);
					newCoords = _fCoordsMath.ZoomOut(curCoords, amount);
					result = GetResult(fJobRequest.JobId, newCoords);
					break;

				case TransformType.Right:
					amount = GetAmount(fJobRequest.Area.Point.X);
					newCoords = _fCoordsMath.ShiftRight(curCoords, amount);
					result = GetResult(fJobRequest.JobId, newCoords);
					break;

				case TransformType.Left:
					amount = GetAmount(fJobRequest.Area.Point.X);
					newCoords = _fCoordsMath.ShiftRight(curCoords, -amount);
					result = GetResult(fJobRequest.JobId, newCoords);
					break;

				case TransformType.Down:
					amount = GetAmount(fJobRequest.Area.Point.X);
					newCoords = _fCoordsMath.ShiftUp(curCoords, -amount);
					result = GetResult(fJobRequest.JobId, newCoords);
					break;

				case TransformType.Up:
					amount = GetAmount(fJobRequest.Area.Point.X);
					newCoords = _fCoordsMath.ShiftUp(curCoords, amount);
					result = GetResult(fJobRequest.JobId, newCoords);
					break;
				default:
					Debug.WriteLine($"Transform Type: {fJobRequest.TransformType} is not supported.");
					break;
			}

			return result;
		}

		// Takes a number between 0 and 10000 and converts it to number between 0 and 1.
		private double GetAmount(int rawAmount)
		{
			double result = rawAmount / 10000d;
			return result;
		}

		private double GetAmountFromPercent(int percentage)
		{
			double result = percentage / 100d; // 50 becomes 0.5
			return result;
		}

		private MCoordsDd GetMCoords(MSS.Types.MSetOld.ApCoords coords)
		{
			Dd startX = new Dd(coords.StartingX);
			Dd endX = new Dd(coords.EndingX);
			Dd startY = new Dd(coords.StartingY);
			Dd endY = new Dd(coords.EndingY);

			PointDd start = new PointDd(startX, startY);
			PointDd end = new PointDd(endX, endY);

			MCoordsDd result = new MCoordsDd(start, end);
			return result;
		}

		private FCoordsResult GetResult(int jobId, MCoordsDd mCoordsDd)
		{
			ApCoords coords = new ApCoords
				(
				StartingX: mCoordsDd.Start().X().GetStringVal(),
				EndingX: mCoordsDd.End().X().GetStringVal(),
				StartingY:  mCoordsDd.Start().Y().GetStringVal(),
				EndingY: mCoordsDd.End().Y().GetStringVal(), new double[0]
				);

			FCoordsResult result = new FCoordsResult(jobId, coords);
			return result;
		}

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects).
					//_fCoordsMath.Dispose();
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~CoordsMath() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}

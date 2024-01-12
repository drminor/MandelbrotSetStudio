using MSS.Types;
using System.Threading;

namespace MSS.Types
{
	public class MsrPosition
	{
		public int RequestNumber { get; init; }
		public PointInt ScreenPosition { get; init; }
		public VectorInt ScreenPositionReleativeToCenter { get; init; }
		public VectorLong SectionBlockOffset { get; init; }
		public bool IsInverted { get; init; }
		public CancellationTokenSource Cts { get; set; }


		public MsrPosition() : this(0, new PointInt(), new VectorInt(), new VectorLong(), isInverted: false)
		{ }

		public MsrPosition(int requestNumber, PointInt screenPosition, VectorInt screenPositionReleativeToCenter, VectorLong sectionBlockOffset, bool isInverted)
		{
			RequestNumber = requestNumber;
			
			ScreenPosition = screenPosition;
			ScreenPositionReleativeToCenter = screenPositionReleativeToCenter;
			SectionBlockOffset = sectionBlockOffset;
			IsInverted = isInverted;

			Cts = new CancellationTokenSource();
		}

		public bool IsCancelled => Cts.IsCancellationRequested;

		public bool Cancel()
		{
			if (Cts.IsCancellationRequested)
			{
				return false;
			}
			else
			{
				Cts.Cancel();
				return true;
			}
		}
	}

}

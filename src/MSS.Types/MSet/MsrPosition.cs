using MSS.Types;

namespace MSS.Common
{
	public class MsrPosition
	{
		public int RequestNumber { get; init; }
		public PointInt ScreenPosition { get; init; }
		public VectorInt ScreenPositionReleativeToCenter { get; init; }
		public VectorLong SectionBlockOffset { get; init; }
		public bool IsInverted { get; init; }

		public MsrPosition(int requestNumber, PointInt screenPosition, VectorInt screenPositionReleativeToCenter, VectorLong sectionBlockOffset, bool isInverted)
		{
			RequestNumber = requestNumber;
			ScreenPosition = screenPosition;
			ScreenPositionReleativeToCenter = screenPositionReleativeToCenter;

			SectionBlockOffset = sectionBlockOffset;
			IsInverted = isInverted;
		}
	}

}

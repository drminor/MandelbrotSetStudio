using MSS.Types.APValues;
using MSS.Types;
using System;
using MSS.Types.MSet;

namespace MSS.Common
{
	public class IteratorCoords
	{
		public IteratorCoords(MapBlockOffset sectionBlockOffset, PointInt screenPos, FP31Val startingCx, FP31Val startingCy, FP31Val delta)
		{
			BlockPos = sectionBlockOffset ?? throw new ArgumentNullException(nameof(sectionBlockOffset));
			ScreenPos = screenPos;
			StartingCx = startingCx;
			StartingCy = startingCy;
			Delta = delta;
		}

		public MapBlockOffset BlockPos { get; init; }
		public PointInt ScreenPos { get; init; }
		public FP31Val StartingCx { get; init;  }
		public FP31Val StartingCy { get; init; }
		public FP31Val Delta { get; init; }

		public string GetStartingCxStringVal()
		{
			var result = RValueHelper.ConvertToString(StartingCx.GetRValue());
			return result;
		}

		public string GetStartingCyStringVal()
		{
			var result = RValueHelper.ConvertToString(StartingCy.GetRValue());
			return result;

		}

		public string GetDeltaStringVal()
		{
			var result = RValueHelper.ConvertToString(Delta.GetRValue());
			return result;

		}

	}
}

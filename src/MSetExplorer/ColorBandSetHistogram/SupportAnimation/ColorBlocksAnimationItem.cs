using MSS.Types;
using System;
using System.Collections.Generic;
using System.Windows;

namespace MSetExplorer
{
	internal class ColorBlocksAnimationItem : IRectAnimationItem
	{
		public double _msPerPixel;

		public ColorBlocksAnimationItem(CbListViewItem sourceListViewItem, CbListViewItem? destinationListViewItem, double msPerPixel)
		{
			_msPerPixel = msPerPixel;
			RectTransitions = new List<RectTransition>();

			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			StartingPos = sourceListViewItem.CbColorBlock.CbColorPair.Container;

			DestinationPos = destinationListViewItem?.CbColorBlock.CbColorPair.Container
				?? GetOffScreenRect(sourceListViewItem);

			Current = StartingPos;
			Elasped = 0;

			//ShiftDuration1F = TimeSpan.FromMilliseconds(300);
		}

		#region Public Properties

		public string Name => SourceListViewItem.Name;
		public CbListViewItem SourceListViewItem { get; init; }
		public CbListViewItem? DestinationListViewItem { get; init; }
		public CbSectionLine CbSectionLine => SourceListViewItem.CbSectionLine;

		public List<RectTransition> RectTransitions { get; init; }

		public Rect StartingPos { get; set; }
		public Rect PosAfterLift { get; set; }
		public Rect PosBeforeDrop { get; set; }
		public Rect DestinationPos { get; init; }

		public Rect Current { get; set; }
		public double Elasped { get; set; }

		#endregion

		#region Public Methods

		public void BuildTimelinePos(Rect to, double velocityMultiplier = 1)
		{
			var dist = Math.Abs(to.X - Current.X);
			var durationMs = dist * _msPerPixel / velocityMultiplier;

			var rt = new RectTransition(Current, to, Elasped, durationMs);

			RectTransitions.Add(rt);
			Current = to;
			Elasped += durationMs;
		}

		public void BuildTimelineW(Rect to)
		{
			var dist = Math.Abs(Current.Width - to.Width);
			var durationMs = dist * _msPerPixel;

			var rt = new RectTransition(Current, to, Elasped, durationMs);

			RectTransitions.Add(rt);
			Current = to;
			Elasped += durationMs;
		}

		public void BuildTimelineX(double shiftAmount)
		{
			var rect = new Rect(Current.X + shiftAmount, Current.Y, Current.Width, Current.Height);
			BuildTimelinePos(rect);
		}

		public void BuildTimelineXAnchorRight(double shiftAmount)
		{
			var rect = new Rect(Current.X + shiftAmount, Current.Y, Current.Width - shiftAmount, Current.Height);
			BuildTimelinePos(rect);
		}

		public void BuildTimelineW(double shiftAmount)
		{
			var rect = new Rect(Current.X, Current.Y, Current.Width + shiftAmount, Current.Height);
			BuildTimelineW(rect);
		}

		public void MoveSourceToDestination()
		{
			if (DestinationListViewItem != null)
			{
 				var newCopy = SourceListViewItem.CbColorBlock.CbColorPair.Clone();
				
				if (DestinationListViewItem.ColorBand.IsLast)
				{
					newCopy.EndColor = ColorBandColor.Black;
				}

				DestinationListViewItem.CbColorBlock.CbColorPair = newCopy;

				SourceListViewItem.CbColorBlock.CbColorPair.TearDown();
			}
		}


		//public void CalcuateShiftDistancesAndTimes(double velocity)
		//{
		//	ShiftDistanceLeft = GetShiftDistanceLeft();
		//	ShiftDistanceRight = GetShiftDistanceRight();

		//	TimeWhenLeftStopsF = ShiftDistanceLeft / velocity;
		//	TimeWhenRightStopsF = ShiftDistanceRight / velocity;


		//	if (ShiftDistanceLeft > 0)
		//	{
		//		if (ShiftDistanceRight > 0)
		//		{
		//			// Left and Right-sides are moving forward
		//			CacluateShiftDistancesAndTimesFF(velocity);
		//		}
		//		else
		//		{
		//			// Left-side is moving forward, Right-side is moving backwards
		//			CacluateShiftDistancesAndTimesFB(velocity);
		//		}
		//	}
		//	else
		//	{
		//		if (ShiftDistanceRight > 0)
		//		{
		//			// Left side is moving backward, right-side is moving forward
		//			CacluateShiftDistancesAndTimesBF(velocity);
		//		}
		//		else
		//		{
		//			// Left and Right-sides are moving backwards

		//			CacluateShiftDistancesAndTimesBB(velocity);
		//		}
		//	}
		//}

		//private void CacluateShiftDistancesAndTimesFF(double velocity)
		//{
		//	// Left and Right-sides are moving forward

		//	// Set all Backward props to 0
		//	TimeRightIsMovingBWhileLeftIsMovingB = 0;
		//	TimeLeftIsMovingBAfterRightStops = 0;
		//	TimeRightIsMovingBAfterLeftStops = 0;

		//	ShiftDuration1B = TimeSpan.Zero;
		//	ShiftDuration2B = TimeSpan.Zero;
		//	ShiftDuration3B = TimeSpan.Zero;

		//	//if (ShiftDistanceLeft > ShiftDistanceRight)
		//	//{
		//	//	RectTransitions.Add(new RectTransition(PosAfterLift, PosAfterShift1F, TimeSpan.Zero, ))
		//	//}




		//	TimeRightIsMovingFWhileLeftIsMovingF = LeftDeltaIsGreater ? TimeWhenRightStopsF : TimeWhenLeftStopsF;
		//	TimeLeftIsMovingFAfterRightStops = LeftDeltaIsGreater ? TimeWhenLeftStopsF - TimeWhenRightStopsF : 0;
		//	TimeRightIsMovingFAfterLeftStops = LeftDeltaIsGreater ? 0 : TimeWhenRightStopsF - TimeWhenLeftStopsF;
		//}

		//private void CacluateShiftDistancesAndTimesFB(double velocity)
		//{
		//	// Left-side is moving forward, Right-side is moving backwards

		//	TimeRightIsMovingFWhileLeftIsMovingF = 0;
		//	TimeRightIsMovingBWhileLeftIsMovingB = 0;


		//	TimeLeftIsMovingBAfterRightStops = 0;
		//}

		//private void CacluateShiftDistancesAndTimesBF(double velocity)
		//{
		//	// Left side is moving backward, right-side is moving forward
		//	TimeRightIsMovingBWhileLeftIsMovingB = 0;
		//	TimeRightIsMovingFWhileLeftIsMovingF = 0;


		//	TimeLeftIsMovingFAfterRightStops = 0;



		//	TimeRightIsMovingFAfterLeftStops = 0;

		//	LeftDeltaIsGreater = ShiftDistanceLeft > ShiftDistanceRight;

		//	TimeRightIsMovingFWhileLeftIsMovingF = LeftDeltaIsGreater ? TimeWhenRightStopsF : TimeWhenLeftStopsF;
		//	TimeLeftIsMovingFAfterRightStops = LeftDeltaIsGreater ? TimeWhenLeftStopsF - TimeWhenRightStopsF : 0;
		//	TimeRightIsMovingFAfterLeftStops = LeftDeltaIsGreater ? 0 : TimeWhenRightStopsF - TimeWhenLeftStopsF;

		//}

		//private void CacluateShiftDistancesAndTimesBB(double velocity)
		//{
		//	// Left and Right-sides are moving backwards

		//	// Set all Forward props to 0
		//	TimeRightIsMovingFWhileLeftIsMovingF = 0;
		//	TimeLeftIsMovingFAfterRightStops = 0;
		//	TimeRightIsMovingFAfterLeftStops = 0;

		//	ShiftDuration1F = TimeSpan.Zero;
		//	ShiftDuration2F = TimeSpan.Zero;
		//	ShiftDuration3F = TimeSpan.Zero;

		//	LeftDeltaIsGreater = ShiftDistanceRight > ShiftDistanceLeft;

		//	TimeRightIsMovingBWhileLeftIsMovingB = LeftDeltaIsGreater ? TimeWhenRightStopsF : TimeWhenLeftStopsF;
		//	TimeLeftIsMovingFAfterRightStops = LeftDeltaIsGreater ? TimeWhenLeftStopsF - TimeWhenRightStopsF : 0;
		//	TimeRightIsMovingFAfterLeftStops = LeftDeltaIsGreater ? 0 : TimeWhenRightStopsF - TimeWhenLeftStopsF;
		//}


		public double GetDistance()
		{
			var result = DestinationPos.Left - StartingPos.Left;
			return result;
		}

		public double GetShiftDistanceLeft()
		{
			var result = PosBeforeDrop.Left - PosAfterLift.Left;
			return result;
		}

		public double GetShiftDistanceRight()
		{
			var result = PosBeforeDrop.Right - PosAfterLift.Right;
			return result;
		}

		#endregion

		private static Rect GetOffScreenRect(CbListViewItem source)
		{
			// The destination is just off the edge of the visible portion of the canvas.
			var sourceRect = source.CbColorBlock.ColorPairContainer;

			var width = source.CbColorBlock.Width * source.CbColorBlock.ContentScale.Width;
			var destinationPosition = new Point(sourceRect.X + width + 5, sourceRect.Top);

			var destRect = new Rect(destinationPosition, sourceRect.Size);

			return destRect;
		}

	}
}

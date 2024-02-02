using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MSetExplorer
{
	using AnimationItemPairList = List<(ColorBlocksAnimationItem, BlendedColorAnimationItem)>;

	internal class PushColorsAnimationInfo
	{
		private double _liftHeight;
		private double _velocity;       // Pixels Per Millisecond
		private double _msPerPixel;


		private double _totalShiftDurationMs;

		public PushColorsAnimationInfo(double liftHeight, double velocity, double totalShiftDurationMs)
		{
			_liftHeight = liftHeight;
			_velocity = velocity;
			_msPerPixel = 1 / velocity;

			_totalShiftDurationMs = totalShiftDurationMs;

			AnimationItemPairs = new AnimationItemPairList();
		}

		public AnimationItemPairList AnimationItemPairs;

		#region Public Methods

		public void Add(CbListViewItem source, CbListViewItem? destination)
		{
			var colorBlocksAItem = new ColorBlocksAnimationItem(source, destination, _msPerPixel);
			var blendedColorAItem = new BlendedColorAnimationItem(source, destination, _msPerPixel);

			AnimationItemPairs.Add((colorBlocksAItem, blendedColorAItem));
		}

		/*
			1. Move right by _liftHeight or less, as we lift it, while keeping the width the same.
			2. Move right and reduce width until width = destination width
				how far from liftpoint
			3. Move right until shift point
				how far
			4. Reduce width until width = destination
				how far
			5. Move right by _liftHeight or less as we drop it back into place, keeping the width the same.

		*/

		public void CalculateMovements()
		{
			CalculateLiftDropBookends();
			
			BuildTimeLines();
		}

		private void CalculateLiftDropBookends()
		{
			var firstHDistForBlocks = GetFirstHDistForBlocks(_liftHeight);
			var firstHDistForBlends = GetFirstHDistForBlends(_liftHeight);

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				CalculateLiftDropBookends(colorBlockItem, firstHDistForBlocks);
				CalculateLiftDropBookends(blendedItem, firstHDistForBlends);
			}
		}

		private void CalculateLiftDropBookends(IRectAnimationItem rectAnimationItem, double horizontalDistForLift)
		{
			var liftedTop = rectAnimationItem.StartingPos.Y - _liftHeight;

			// After Lift
			var liftPoint = new Point(rectAnimationItem.StartingPos.X + horizontalDistForLift, liftedTop);

			rectAnimationItem.PosAfterLift = new Rect(liftPoint, rectAnimationItem.StartingPos.Size);

			// After final narrowing, before drop
			var shiftPoint = new Point(rectAnimationItem.Destination.X - horizontalDistForLift, liftedTop);

			rectAnimationItem.PosBeforeDrop = new Rect(shiftPoint, rectAnimationItem.Destination.Size);
		}

		private void BuildTimeLines()
		{
			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				BuildRetrogradeTimelines(colorBlockItem);
				BuildRetrogradeTimelines(blendedItem);
			}

			SyncElapsed();

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				BuildTimelines(colorBlockItem);
				BuildTimelines(blendedItem);
			}

			SyncElapsed();

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				colorBlockItem.BuildTimelineX(colorBlockItem.Destination);
				blendedItem.BuildTimelineX(blendedItem.Destination);
			}
		}

		private void BuildTimelines(IRectAnimationItem rectAnimationItem)
		{
			var sDistanceLeft = rectAnimationItem.GetShiftDistanceLeft();
			var sDistanceRight = rectAnimationItem.GetShiftDistanceRight();

			if (sDistanceLeft > 0)
			{
				if (sDistanceRight > 0)
				{
					if (sDistanceLeft > sDistanceRight)
					{
						// Shift right, keeping width constant for the distance both the left and right edges must move
						rectAnimationItem.BuildTimelineX(sDistanceRight);

						// Shift left side forward, but keep the right side fixed
						rectAnimationItem.BuildTimelineXAnchorRight(sDistanceLeft - sDistanceRight);
					}
					else
					{
						// Shift right, keeping the width constant for the distance both the left and right edges must move
						rectAnimationItem.BuildTimelineX(sDistanceLeft);

						// Shift the right side forward, but keep the left side fixed - i.e., extend the width
						rectAnimationItem.BuildTimelineW(sDistanceRight - sDistanceLeft);
					}
				}
				else
				{
					// Shift right, keeping width constant for the distance both the left and right edges must move
					rectAnimationItem.BuildTimelineX(sDistanceRight);
				}
			}

			// Move to PosBeforeDrop
			rectAnimationItem.BuildTimelineX(rectAnimationItem.PosBeforeDrop);
		}

		private void BuildRetrogradeTimelines(IRectAnimationItem rectAnimationItem)
		{
			// Move to lift point
			rectAnimationItem.BuildTimelineX(rectAnimationItem.PosAfterLift);

			var sDistanceLeft = -1 * rectAnimationItem.GetShiftDistanceLeft();
			var sDistanceRight = -1 * rectAnimationItem.GetShiftDistanceRight();

			if (sDistanceLeft > 0)
			{
				if (sDistanceRight > 0)
				{
					if (sDistanceLeft > sDistanceRight)
					{
						// Both are moving left

						// Shift right, keeping width constant for the distance both the left and right edges must move
						rectAnimationItem.BuildTimelineX(sDistanceRight);

						// Shift left side forward, but keep the right side fixed
						rectAnimationItem.BuildTimelineXAnchorRight(sDistanceLeft - sDistanceRight);
					}
					else
					{
						// Shift right, keeping the width constant for the distance both the left and right edges must move
						rectAnimationItem.BuildTimelineX(sDistanceLeft);

						// Shift the right side forward, but keep the left side fixed - i.e., extend the width
						rectAnimationItem.BuildTimelineW(sDistanceRight - sDistanceLeft);
					}
				}
				else
				{
					// Shift right, keeping width constant for the distance both the left and right edges must move
					rectAnimationItem.BuildTimelineX(sDistanceRight);
				}
			}
		}

		private void SyncElapsed()
		{
			var maxColorBlockElapsed = AnimationItemPairs.Max(x => x.Item1.Elasped);
			var maxBlendedBandElapsed = AnimationItemPairs.Max(x => x.Item2.Elasped);

			var maxE = Math.Max(maxColorBlockElapsed, maxBlendedBandElapsed);

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				colorBlockItem.Elasped = maxE;
				blendedItem.Elasped = maxE;
			}
		}

		private void CalculateShiftPositionsOld()
		{
			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				// Move to lift point
				colorBlockItem.BuildTimelineX(colorBlockItem.PosAfterLift);

				var sDistanceLeft = colorBlockItem.GetShiftDistanceLeft();
				var sDistanceRight = colorBlockItem.GetShiftDistanceRight();

				//	ShiftDistanceLeft = GetShiftDistanceLeft();
				//	ShiftDistanceRight = GetShiftDistanceRight();

				//	TimeWhenLeftStopsF = ShiftDistanceLeft / velocity;
				//	TimeWhenRightStopsF = ShiftDistanceRight / velocity;


				if (sDistanceLeft > 0)
				{
					if (sDistanceRight > 0)
					{
						if (sDistanceLeft > sDistanceRight)
						{
							// Shift right, keeping width constant for the distance both the left and right edges must move
							colorBlockItem.BuildTimelineX(sDistanceRight);

							// Shift left side forward, but keep the right side fixed
							colorBlockItem.BuildTimelineXAnchorRight(sDistanceLeft - sDistanceRight);
						}
						else
						{
							// Shift right, keeping the width constant for the distance both the left and right edges must move
							colorBlockItem.BuildTimelineX(sDistanceLeft);

							// Shift the right side forward, but keep the left side fixed - i.e., extend the width
							colorBlockItem.BuildTimelineW(sDistanceRight - sDistanceLeft);
						}
					}
					else
					{

					}
				}
				else
				{

				}

				//if (colorBlockItem.TimeRightIsMovingFWhileLeftIsMovingF > 0)
				//{
				//	// First shift while keeping the  width constant
				//	// Calculate the shift amount while keeping the width
				//	var cS1 = 0;

				//	var colorBlockPoint1 = Point.Add(colorBlockItem.PosAfterLift.Location, new Vector(cS1, 0));
				//	colorBlockItem.PosAfterShift1F = new Rect(colorBlockPoint1, colorBlockItem.StartingPos.Size);

				//	colorBlockItem.ShiftDuration1F = TimeSpan.FromMilliseconds(cS1 * _velocity);
				//}
				//else
				//{
				//	colorBlockItem.PosAfterShift1F = colorBlockItem.PosAfterLift;
				//	colorBlockItem.ShiftDuration1F = TimeSpan.Zero;
				//}

				//Size colorBlockSize2;

				//if (colorBlockItem.TimeLeftIsMovingFAfterRightStops > 0)
				//{
				//	// Second shift move left, but not right.
				//	// Calculate the shift amount while narrowing the item
				//	var cS2 = 0;
				//	var cW1 = 0;

				//	var colorBlockPoint2 = Point.Add(colorBlockItem.PosAfterLift.Location, new Vector(cS2, 0));
				//	colorBlockSize2 = new Size(colorBlockItem.StartingPos.Size.Width + cW1, colorBlockHeight);
				//	colorBlockItem.PosAfterShift2F = new Rect(colorBlockPoint2, colorBlockSize2);

				//	colorBlockItem.ShiftDuration2F = TimeSpan.FromMilliseconds(cS2 * _velocity);
				//}
				//else
				//{
				//	colorBlockItem.PosAfterShift2F = colorBlockItem.PosAfterShift1F;
				//	colorBlockItem.ShiftDuration2F = TimeSpan.Zero;
				//}


				//if (colorBlockItem.TimeRightIsMovingFAfterLeftStops > 0)
				//{
				//	// Third shift widen

				//	// Calculate the amount the band needs to be widened
				//	var cW2 = 0;
				//	var colorBlockSize3 = new Size(colorBlockSize2.Width + cW2, colorBlockHeight);
				//	colorBlockItem.PosAfterShift3F = new Rect(colorBlockItem.PosAfterShift2F.Location, colorBlockSize3);

				//	colorBlockItem.ShiftDuration3F = TimeSpan.FromMilliseconds(cW2 * _velocity);

				//}
				//else
				//{
				//	colorBlockItem.PosAfterShift3F = colorBlockItem.PosAfterShift2F;
				//	colorBlockItem.ShiftDuration3F = TimeSpan.Zero;
				//}
			}
		}

		//private void CalculateShiftPositionsOld()
		//{

		//	foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
		//	{
		//		var colorBlockLiftedTop = colorBlockItem.StartingPos.Y - _liftHeight;
		//		var blendedBandLiftedTop = blendedItem.StartingPos.Y - _liftHeight;

		//		// After first shift and narrowing
		//		// Calculate the shift amount while narrowing the item
		//		var cS1 = 0;
		//		var cW1 = 0;

		//		var colorBlockPoint2 = new Point(colorBlockItem.PosAfterLift.X + cS1, colorBlockLiftedTop);
		//		var colorBlockSize2 = new Size(colorBlockItem.StartingPos.Size.Width + cW1, colorBlockLiftedTop);
		//		colorBlockItem.PosAfterShift1F = new Rect(colorBlockPoint2, colorBlockSize2);

		//		var bS1 = 0;
		//		var bW1 = 0;
		//		var blendBandPoint2 = new Point(blendedItem.PosAfterLift.X + bS1, blendedBandLiftedTop);
		//		var blendedBandSize2 = new Size(blendedItem.StartingPos.Size.Width + bW1, blendedBandLiftedTop);
		//		blendedItem.PosAfterShift1 = new Rect(blendBandPoint2, blendedBandSize2);

		//		// After second shift with constant width
		//		// Calculate the shift amount while keeping the width
		//		var cS2 = 0;

		//		var colorBlockPoint3 = new Point(colorBlockItem.PosAfterShift1F.X + cS2, colorBlockLiftedTop);
		//		colorBlockItem.PosAfterShift2F = new Rect(colorBlockPoint3, colorBlockSize2);

		//		var bS2 = 0;
		//		var blendedBandPoint3 = new Point(blendedItem.PosAfterShift1.X + bS2, blendedBandLiftedTop);
		//		blendedItem.PosAfterShift2 = new Rect(blendedBandPoint3, blendedBandSize2);


		//		// Calculate the amount the band needs to be widened
		//		var cW2 = 0;
		//		var colorBlockSize3 = new Size(colorBlockSize2.Width + cW2, colorBlockLiftedTop);
		//		colorBlockItem.PosAfterShift3F = new Rect(colorBlockPoint3, colorBlockSize3);

		//		var bW2 = 0;
		//		var blendedBandSize3 = new Size(blendedBandSize2.Width + bW2, blendedBandLiftedTop);
		//		blendedItem.PosAfterResize3 = new Rect(blendedBandPoint3, blendedBandSize3);
		//	}
		//}

		//private void CalculateDurationsOld()
		//{
		//	var maxShiftDistForBlocks = GetMaxShiftDistanceForBlocks();
		//	var maxShiftDistForBlends = GetMaxShiftDistanceForBlends();

		//	foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
		//	{
		//		var fraction = colorBlockItem.GetShiftDistanceLeft() / maxShiftDistForBlocks;
		//		colorBlockItem.ShiftDuration1F = TimeSpan.FromMilliseconds(fraction * _totalShiftDurationMs);

		//		fraction = blendedItem.GetShiftDistance() / maxShiftDistForBlends;
		//		blendedItem.ShiftDuration1 = TimeSpan.FromMilliseconds(fraction * _totalShiftDurationMs);
		//	}
		//}


		public void MoveSourcesToDestinations()
		{
			for (var i = AnimationItemPairs.Count - 2; i >= 0; i--)
			{
				var (colorBlockAItem, blendedColorAItem) = AnimationItemPairs[i];
				colorBlockAItem.MoveSourceToDestination();
				blendedColorAItem.MoveSourceToDestination();
			}
		}

		#endregion

		#region Private Methods

		private double GetFirstHDistForBlocks(double liftHeight)
		{
			// The first movement is to lift each CbColorPair up by liftHeight
			// and if possible move each CbColorPair forward by the same amount
			// so that the path is along a 45 degree slope.

			// The first horizontal movement can be no greater than 1/2 the total distance
			// for any of items.

			var minDist = AnimationItemPairs.Min(x => x.Item1.GetDistance());
			var firstMovementDistMax = minDist / 2;

			var result = Math.Min(firstMovementDistMax, liftHeight);

			return result;
		}

		private double GetFirstHDistForBlends(double liftHeight)
		{
			// The first movement is to lift each CbColorPair up by liftHeight
			// and if possible move each CbColorPair forward by the same amount
			// so that the path is along a 45 degree slope.

			// The first horizontal movement can be no greater than 1/2 the total distance
			// for any of items.

			var minDist = AnimationItemPairs.Min(x => x.Item2.GetDistance());
			var firstMovementDistMax = minDist / 2;

			var result = Math.Min(firstMovementDistMax, liftHeight);

			return result;
		}

		//private double GetMaxShiftDistanceForBlocks()
		//{
		//	var maxLeft = AnimationItemPairs.Max(x => x.Item1.GetShiftDistanceLeft());
		//	var maxRight = AnimationItemPairs.Max(x => x.Item1.GetShiftDistanceRight());
		//	return Math.Max(maxLeft, maxRight);
		//}

		//private double GetMaxShiftDistanceForBlends()
		//{
		//	var maxLeft = AnimationItemPairs.Max(x => x.Item2.GetShiftDistance());
		//	var maxRight = 0; // AnimationItemPairs.Max(x => x.Item2.GetShiftDistanceRight());

		//	return Math.Max(maxLeft, maxRight);
		//}

		#endregion
	}
}

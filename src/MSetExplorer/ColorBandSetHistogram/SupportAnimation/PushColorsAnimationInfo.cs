using MSS.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace MSetExplorer
{
	using AnimationItemPairList = List<(IRectAnimationItem, IRectAnimationItem)>;

	internal class PushColorsAnimationInfo
	{
		private double _liftHeight;
		private double _msPerPixel;

		public PushColorsAnimationInfo(double liftHeight, double velocity)
		{
			_liftHeight = liftHeight;
			_msPerPixel = 1 / velocity;

			AnimationItemPairs = new AnimationItemPairList();
		}

		public AnimationItemPairList AnimationItemPairs;

		#region Public Methods

		public void Add(CbListViewItem source, CbListViewItem? destination)
		{
			var colorBlocksAItem = new ColorBlocksAnimationItem(source, destination, _msPerPixel);
			var blendedColorAItem = new BlendedColorAnimationItem(source, destination, _msPerPixel);

			AnimationItemPairs.Add((colorBlocksAItem, blendedColorAItem));

			//source.CbColorBlock.CbColorPair.ShowDiagBorder = true;
		}

		public double CalculateMovements()
		{
			CalculateLiftDropBookends();

			var startPushSyncPoint = BuildTimeLines();

			return startPushSyncPoint;
		}

		public void MoveSourcesToDestinations()
		{
			for (var i = AnimationItemPairs.Count - 2; i >= 0; i--)
			{
				var (colorBlockAItem, blendedColorAItem) = AnimationItemPairs[i];

				//colorBlockAItem.SourceListViewItem.CbColorBlock.CbColorPair.ShowDiagBorder = false;

				colorBlockAItem.MoveSourceToDestination();
				blendedColorAItem.MoveSourceToDestination();
			}
		}

		public double GetMaxDuration()
		{
			var maxColorBlockElapsed = AnimationItemPairs.Max(x => x.Item1.Elasped);
			var maxBlendedBandElapsed = AnimationItemPairs.Max(x => x.Item2.Elasped);

			var maxE = Math.Max(maxColorBlockElapsed, maxBlendedBandElapsed);

			return maxE;
		}

		#endregion

		#region Private Methods

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
			var shiftPoint = new Point(rectAnimationItem.DestinationPos.X - horizontalDistForLift, liftedTop);

			rectAnimationItem.PosBeforeDrop = new Rect(shiftPoint, rectAnimationItem.DestinationPos.Size);
		}

		private double BuildTimeLines()
		{
			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				// Lift -- Move right by _liftHeight or less, as we lift it, while keeping the width the same.
				colorBlockItem.BuildTimelinePos(colorBlockItem.PosAfterLift, veclocityMultiplier: 0.2);
				blendedItem.BuildTimelinePos(blendedItem.PosAfterLift, veclocityMultiplier: 0.2);
			}

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				// Move left and reduce width for each item that is futher right that the destination
				BuildPullTimelines(colorBlockItem);
				BuildPullTimelines(blendedItem);
			}

			CheckForNegativeShifts();

			var startPushSyncPoint = SyncNextBeginTimeElapsed();

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				// Move right those items who are not yet at the destination.
				// Narrow items to prevent the right side moving past the destination's right side.
				BuildPushTimelines(colorBlockItem);
				BuildPushTimelines(blendedItem);
			}

			SyncNextBeginTimeElapsed();

			CheckItemsAreAtDropPoint();

			// Drop
			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				colorBlockItem.BuildTimelinePos(colorBlockItem.DestinationPos, veclocityMultiplier: 0.2);
				blendedItem.BuildTimelinePos(blendedItem.DestinationPos, veclocityMultiplier: 0.2);
			}

			return startPushSyncPoint;
		}

		private void BuildPushTimelines(IRectAnimationItem rectAnimationItem)
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

						if (sDistanceLeft != sDistanceRight)
						{
							// Shift the right side forward, but keep the left side fixed - i.e., widen
							rectAnimationItem.BuildTimelineW(sDistanceRight - sDistanceLeft);
						}
					}
				}
				else
				{
					// Shift right, keeping width constant for the distance both the left and right edges must move
					rectAnimationItem.BuildTimelineX(sDistanceRight);
				}
			}
			else
			{
				if (sDistanceRight > 0)
				{
					// Shift the right side forward, but keep the left side fixed - i.e., widen
					rectAnimationItem.BuildTimelineW(sDistanceRight);
				}
			}
		}

		private void BuildPullTimelines(IRectAnimationItem rectAnimationItem)
		{
			var sDistanceLeft = rectAnimationItem.GetShiftDistanceLeft();
			var sDistanceRight = rectAnimationItem.GetShiftDistanceRight();

			if (sDistanceLeft < 0)
			{
				if (sDistanceRight < 0)
				{
					if (sDistanceLeft < sDistanceRight)
					{
						// Both are moving left

						// Shift left, keeping width constant for the distance both the left and right edges must move
						rectAnimationItem.BuildTimelineX(sDistanceRight);

						// Shift left side backwards, but keep the right side fixed
						rectAnimationItem.BuildTimelineXAnchorRight(sDistanceLeft - sDistanceRight);
					}
					else
					{
						// Shift left, keeping the width constant for the distance both the left and right edges must move
						rectAnimationItem.BuildTimelineX(sDistanceLeft);

						if (sDistanceLeft != sDistanceRight)
						{
							// Shift the right side backward, but keep the left side fixed - i.e., decrease the width
							rectAnimationItem.BuildTimelineW(sDistanceRight - sDistanceLeft);
						}
					}
				}
			}
			else
			{
				if (sDistanceRight < 0)
				{
					// Shift the right side backward, but keep the left side fixed - i.e., decrease the width
					rectAnimationItem.BuildTimelineW(sDistanceRight);
				}
			}
		}

		private double SyncNextBeginTimeElapsed(double delay = 0)
		{
			var maxDuration = GetMaxDuration();

			var syncPoint = maxDuration + delay;

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				colorBlockItem.Elasped = syncPoint;
				blendedItem.Elasped = syncPoint;
			}

			return syncPoint;
		}

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

		[Conditional("DEBUG")]
		private void CheckForNegativeShifts()
		{
			var cntColorBlocksThatWillMoveLeft = AnimationItemPairs.Count(x => x.Item1.GetShiftDistanceLeft() < 0 || x.Item1.GetShiftDistanceRight() < 0);
			Debug.Assert(cntColorBlocksThatWillMoveLeft == 0, $"There are {cntColorBlocksThatWillMoveLeft} ColorBlocks not at the drop point.");

			//if (cntColorBlocksThatWillMoveLeft > 0)
			//{
			//	Debug.WriteLine($"There are {cntColorBlocksThatWillMoveLeft} ColorBlocks that need to move left.");
			//}

			var cntBlendedBandsThatWillMoveLeft = AnimationItemPairs.Count(x => x.Item2.GetShiftDistanceLeft() < 0 || x.Item2.GetShiftDistanceRight() < 0);
			Debug.Assert(cntBlendedBandsThatWillMoveLeft == 0, $"There are {cntBlendedBandsThatWillMoveLeft} ColorBlocks not at the drop point.");
			//if (cntBlendedBandsThatWillMoveLeft > 0)
			//{
			//	Debug.WriteLine($"There are {cntBlendedBandsThatWillMoveLeft} BlendedBands that need to move left.");
			//}
		}

		[Conditional("DEBUG")]
		private void CheckItemsAreAtDropPoint()
		{
			var cntColorBlocksNotAtDropPt = AnimationItemPairs.Count(x => !ScreenTypeHelper.IsDoubleNearZero(x.Item1.GetShiftDistanceLeft()));
			Debug.Assert(cntColorBlocksNotAtDropPt == 0, $"There are {cntColorBlocksNotAtDropPt} ColorBlocks not at the drop point.");

			//if (cntColorBlocksNotAtDropPt > 0)
			//{
			//	Debug.WriteLine($"There are {cntColorBlocksNotAtDropPt} ColorBlocks not at the drop point.");
			//}

			var cntBlendedBandsNotAtDropPt = AnimationItemPairs.Count(x => !ScreenTypeHelper.IsDoubleNearZero(x.Item2.GetShiftDistanceLeft()));
			Debug.Assert(cntBlendedBandsNotAtDropPt == 0, $"There are {cntBlendedBandsNotAtDropPt} BlendedBands not at the drop point.");

			//if (cntBlendedBandsNotAtDropPt > 0)
			//{
			//	Debug.WriteLine($"There are {cntBlendedBandsNotAtDropPt} BlendedBands not at the drop point.");
			//}
		}

		#endregion
	}
}

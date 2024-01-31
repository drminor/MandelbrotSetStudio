using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Windows.Devices.Input;
using Windows.UI.WebUI;

namespace MSetExplorer
{
	using AnimationItemPairList = List<(ColorBlocksAnimationItem, BlendedColorAnimationItem)>;

	internal class PushColorsAnimationInfo
	{
		private double _liftHeight;
		private double _totalShiftDurationMs;

		public PushColorsAnimationInfo(double liftHeight, double totalShiftDurationMs)
		{
			_liftHeight = liftHeight;
			_totalShiftDurationMs = totalShiftDurationMs;

			AnimationItemPairs = new AnimationItemPairList();
		}

		public AnimationItemPairList AnimationItemPairs;

		#region Public Methods

		public void Add(CbListViewItem source, CbListViewItem? destination)
		{
			var colorBlocksAItem = new ColorBlocksAnimationItem(source, destination);
			var blendedColorAItem = new BlendedColorAnimationItem(source, destination);

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
			var firstHDistForBlocks = GetFirstHDistForBlocks(_liftHeight);
			var firstHDistForBlends = GetFirstHDistForBlends(_liftHeight);

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				var colorBlockLiftedTop = colorBlockItem.StartingPos.Y - _liftHeight;
				var blendedBandLiftedTop = blendedItem.StartingPos.Y - _liftHeight;

				var colorBlockLiftPoint = new Point(colorBlockItem.StartingPos.X + firstHDistForBlocks, colorBlockLiftedTop);
				var blendedBandLiftPoint = new Point(blendedItem.StartingPos.X + firstHDistForBlends, blendedBandLiftedTop);

				var colorBlockShiftPoint = new Point(colorBlockItem.Destination.X - firstHDistForBlocks, colorBlockLiftedTop);
				var blendedBandShiftPoint = new Point(blendedItem.Destination.X - firstHDistForBlends, blendedBandLiftedTop);


				// After Lift
				colorBlockItem.PosAfterLift = new Rect(colorBlockLiftPoint, colorBlockItem.StartingPos.Size);
				blendedItem.PosAfterLift = new Rect(blendedBandLiftPoint, blendedItem.StartingPos.Size);

				// Calculate the amount the left edge can be pushed without changing the width

				// After first shift and narrowing
				var colorBlockX2 = colorBlockLiftPoint.X; // Change Me
				var colorBlockPoint2 = new Point(colorBlockX2, colorBlockLiftedTop);
				colorBlockItem.PosAfterShift1 = new Rect(colorBlockPoint2, colorBlockItem.Size1);

				var blendBandX2 = blendedBandLiftPoint.X; // Change Me
				var blendBandPoint2 = new Point(blendBandX2, blendedBandLiftedTop);
				blendedItem.PosAfterShift1 = new Rect(blendBandPoint2, blendedItem.Size1);

				// Calculate the amount the left edge still needs to move to get to the shift point

				// After second shift with constant width
				var colorBlockX3 = colorBlockShiftPoint.X;
				var colorBlockPoint3 = new Point(colorBlockX3, colorBlockLiftedTop);
				colorBlockItem.PosAfterShift2 = new Rect(colorBlockPoint3, colorBlockItem.Size1);

				var blendedBandX3 = blendedBandShiftPoint.X;
				var blendedBandPoint3 = new Point(blendedBandX3, blendedBandLiftedTop);
				blendedItem.PosAfterShift2 = new Rect(blendedBandPoint3, blendedItem.Size1);

				// Calculate the amount the band needs to be narrorwed to fit
				colorBlockItem.PosAfterResize3 = new Rect(colorBlockPoint3, colorBlockItem.Size2);
				blendedItem.PosAfterResize3 = new Rect(blendedBandPoint3, blendedItem.Size2);

				// After final narrowing
				colorBlockItem.PosBeforeDrop = new Rect(colorBlockShiftPoint, colorBlockItem.Destination.Size);
				blendedItem.PosBeforeDrop = new Rect(blendedBandShiftPoint, blendedItem.Destination.Size);

				// Drop
				//colorBlockItem.DropDestination = colorBlockItem.Destination.Location;
				//blendedItem.DropDestination = blendedItem.Destination.Location;
			}

			var maxShiftDistForBlocks = GetMaxShiftDistanceForBlocks();
			var maxShiftDistForBlends = GetMaxShiftDistanceForBlends();

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				var fraction = colorBlockItem.GetShiftDistance() / maxShiftDistForBlocks;
				colorBlockItem.ShiftDuration1 = TimeSpan.FromMilliseconds(fraction * _totalShiftDurationMs);

				fraction = blendedItem.GetShiftDistance() / maxShiftDistForBlends;
				blendedItem.ShiftDuration1 = TimeSpan.FromMilliseconds(fraction * _totalShiftDurationMs);
			}
		}

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

		private double GetMaxShiftDistanceForBlocks()
		{
			var result = AnimationItemPairs.Max(x => x.Item1.GetShiftDistance());
			return result;
		}

		private double GetMaxShiftDistanceForBlends()
		{
			var result = AnimationItemPairs.Max(x => x.Item2.GetShiftDistance());
			return result;
		}

		#endregion
	}
}

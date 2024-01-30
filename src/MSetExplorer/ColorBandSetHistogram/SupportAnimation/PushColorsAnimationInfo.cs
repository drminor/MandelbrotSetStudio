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

		public void CalculateMovements()
		{
			var firstHDistForBlocks = GetFirstMovementDistanceForBlocks(_liftHeight);
			var firstHDistForBlends = GetFirstMovementDistanceForBlends(_liftHeight);


			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				////// Color Blocks
				

				// Lift
				colorBlockItem.LiftDestination = new Point(colorBlockItem.StartingPos.X + firstHDistForBlocks, colorBlockItem.StartingPos.Y - _liftHeight);
				colorBlockItem.PosAfterLift = new Rect(colorBlockItem.LiftDestination, colorBlockItem.StartingPos.Size);

				// Resize1
				colorBlockItem.PosAfterResize1 = new Rect(colorBlockItem.PosAfterLift.Location, colorBlockItem.Size1);

				// Shift Right
				colorBlockItem.ShiftDestination = new Point(colorBlockItem.Destination.X - firstHDistForBlocks, colorBlockItem.StartingPos.Y - _liftHeight);
				colorBlockItem.PosAfterShift = new Rect(colorBlockItem.ShiftDestination, colorBlockItem.Size1);

				// Resize2
				colorBlockItem.PosAfterResize2 = new Rect(colorBlockItem.PosAfterShift.Location, colorBlockItem.Destination.Size);

				// Drop
				colorBlockItem.DropDestination = colorBlockItem.Destination.Location;


				////// Blended Item

				// Lift
				blendedItem.LiftDestination = new Point(blendedItem.StartingPos.X + firstHDistForBlends, blendedItem.StartingPos.Y - _liftHeight);
				blendedItem.PosAfterLift = new Rect(blendedItem.LiftDestination, blendedItem.StartingPos.Size);

				// Resize1
				blendedItem.PosAfterResize1 = new Rect(blendedItem.PosAfterLift.Location, blendedItem.Size1);

				// Shift Right
				blendedItem.ShiftDestination = new Point(blendedItem.Destination.X - firstHDistForBlends, blendedItem.StartingPos.Y - _liftHeight);
				blendedItem.PosAfterShift = new Rect(blendedItem.ShiftDestination, blendedItem.Size1);

				// Resize2
				blendedItem.PosAfterResize2 = new Rect(blendedItem.PosAfterShift.Location, blendedItem.Destination.Size);

				// Drop
				blendedItem.DropDestination = blendedItem.Destination.Location;
			}

			var maxShiftDistForBlocks = GetMaxShiftDistanceForBlocks();
			var maxShiftDistForBlends = GetMaxShiftDistanceForBlends();

			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				colorBlockItem.ShiftFraction = colorBlockItem.GetShiftDistance() / maxShiftDistForBlocks;
				colorBlockItem.ShiftDuration = TimeSpan.FromMilliseconds(colorBlockItem.ShiftFraction * _totalShiftDurationMs);

				blendedItem.ShiftFraction = blendedItem.GetShiftDistance() / maxShiftDistForBlends;
				blendedItem.ShiftDuration = TimeSpan.FromMilliseconds(blendedItem.ShiftFraction * _totalShiftDurationMs);
			}
		}

		public void TearDown()
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

		private double GetFirstMovementDistanceForBlocks(double liftHeight)
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

		private double GetFirstMovementDistanceForBlends(double liftHeight)
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MSetExplorer
{
	using AnimationItemPairList = List<(ColorBlocksAnimationItem, BlendedColorAnimationItem)>;

	internal class PushColorsAnimationInfo
	{
		private CbListViewElevations _elevations;

		public PushColorsAnimationInfo(CbListViewElevations elevations)
		{
			_elevations = elevations;

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
			var liftHeight = _elevations.ColorBlocksHeight;

			var firstHDist = GetFirstMovementDistanceForBlocks(liftHeight);
			var firstHDistForBlends = GetFirstMovementDistanceForBlends(liftHeight);


			foreach (var (colorBlockItem, blendedItem) in AnimationItemPairs)
			{
				// Lift
				colorBlockItem.FirstMovement = new Point(colorBlockItem.Current.X + firstHDist, colorBlockItem.Current.Y - liftHeight);
				colorBlockItem.Stage1 = new Rect(colorBlockItem.FirstMovement, colorBlockItem.Current.Size);

				// Resize
				colorBlockItem.Stage2 = new Rect(colorBlockItem.Stage1.Location, colorBlockItem.Destination.Size);

				// Shift Right
				//cbAnimationItem.SecondMovement = new Point(cbAnimationItem.Destination.X - firstHDist, cbAnimationItem.Stage2.Y);
				colorBlockItem.SecondMovement = new Point(colorBlockItem.Destination.X - firstHDist, colorBlockItem.Current.Y - liftHeight);
				colorBlockItem.Stage3 = new Rect(colorBlockItem.SecondMovement, colorBlockItem.Destination.Size);

				// Drop
				colorBlockItem.ThirdMovement = colorBlockItem.Destination.Location;
				//cbAnimationItem.Stage4 = cbAnimationItem.Destination;


				// Lift
				blendedItem.FirstMovement = new Point(blendedItem.Current.X + firstHDistForBlends, blendedItem.Current.Y - liftHeight);
				blendedItem.Stage1 = new Rect(blendedItem.FirstMovement, blendedItem.Current.Size);

				// Resize
				blendedItem.Stage2 = new Rect(blendedItem.Stage1.Location, blendedItem.Destination.Size);

				// Shift Right
				//cbAnimationItem.SecondMovement = new Point(cbAnimationItem.Destination.X - firstHDist, cbAnimationItem.Stage2.Y);
				blendedItem.SecondMovement = new Point(blendedItem.Destination.X - firstHDist, blendedItem.Current.Y - liftHeight);
				blendedItem.Stage3 = new Rect(blendedItem.SecondMovement, blendedItem.Destination.Size);

				// Drop
				blendedItem.ThirdMovement = blendedItem.Destination.Location;
				//cbAnimationItem.Stage4 = cbAnimationItem.Destination;

			}
		}

		public void Setup()
		{
			//var (lastColorBlockAItem, lastBlendedColorAItem) = AnimationItemPairs[^1];

			//var lastColorPair = lastColorBlockAItem.SourceCbColorPair;
			//var lastBlendedColorPair = lastBlendedColorAItem.SourceBlendedColorPair;

			//for (var i = AnimationItemPairs.Count - 2; i >= 0; i--)
			//{
			//	var (colorBlockAItem, blendedColorAItem) = AnimationItemPairs[i];
			//	colorBlockAItem.UpdateDestWithSource();
			//	blendedColorAItem.UpdateDestWithSource();
			//}


			//foreach (var (colorBlocksAItem, blendedColorAItem) in AnimationItemPairs)
			//{
			//	if (colorBlocksAItem.DestinationListViewItem != null)
			//	{
			//	}
			//	else
			//	{
			//		colorBlocksAItem.CbColorBlocks.CbColorPair.Visibility = Visibility.Hidden;
			//		colorBlocksAItem.CbColorBlocks.CbColorPair.TearDown();
			//	}

			//	//cbAnimationItem.CbColorBlocks.UsingProxy = true;

			//	if (blendedColorAItem.DestinationListViewItem != null)
			//	{

			//	}
			//	else
			//	{
			//		blendedColorAItem.CbRectangle.CbBlendedColorPair.Visibility = Visibility.Hidden;
			//		blendedColorAItem.CbRectangle.CbBlendedColorPair.TearDown();
			//	}



			//	colorBlocksAItem.CbSectionLine.TopArrowVisibility = Visibility.Hidden;
			//}

			//foreach (var cbAnimationItem in BlendedColorAnimationItems)
			//{
			//	cbAnimationItem.CbRectangle.UsingProxy = true;
			//}
		}

		public void TearDown()
		{
			//var (lastColorBlockAItem, lastBlendedColorAItem) = AnimationItemPairs[^1];

			//var lastColorPair = lastColorBlockAItem.SourceCbColorPair;
			//var lastBlendedColorPair = lastBlendedColorAItem.SourceBlendedColorPair;

			for (var i = AnimationItemPairs.Count - 2; i >= 0; i--)
			{
				var (colorBlockAItem, blendedColorAItem) = AnimationItemPairs[i];
				colorBlockAItem.MoveSourceToDestination();
				blendedColorAItem.MoveSourceToDestination();
			}

			//lastBlendedColorPair.Visibility = Visibility.Hidden;
			//lastColorPair.TearDown();

			//lastBlendedColorPair.Visibility = Visibility.Hidden;
			//lastBlendedColorPair.TearDown();

			//AnimationItemPairs.Clear();

			//foreach (var (colorBlockAItem, blendedColorAItem) in AnimationItemPairs)
			//{
			//	if (colorBlockAItem.SourceListViewItem.CbColorBlock.CbColorPairProxy != null)
			//	{
			//		if (colorBlockAItem.DestinationListViewItem != null)
			//		{
			//			var cc = colorBlockAItem.DestinationListViewItem.CbColorBlock.CbColorPair;
			//			colorBlockAItem.DestinationListViewItem.CbColorBlock.CbColorPair = colorBlockAItem.SourceListViewItem.CbColorBlock.CbColorPairProxy;
			//			cc.TearDown();
			//		}
			//		else
			//		{

			//		}

			//	}

			//	colorBlockAItem.CbColorBlocks.UsingProxy = false;
			//	colorBlockAItem.CbSectionLine.TopArrowVisibility = Visibility.Visible;
			//}

			//foreach (var (colorBlockAItem, blendedColorAItem) in AnimationItemPairs)
			//{
			//	//if (cbAnimationItem.DestinationListViewItem != null && cbAnimationItem.SourceListViewItem.CbRectangle.CbBlendedColorPairProxy != null)
			//	//{
			//	//	var cc = cbAnimationItem.DestinationListViewItem.CbRectangle.CbBlendedColorPair;
			//	//	cbAnimationItem.DestinationListViewItem.CbRectangle.CbBlendedColorPair = cbAnimationItem.SourceListViewItem.CbRectangle.CbBlendedColorPairProxy;
			//	//	cc.TearDown();
			//	//}
			//	//cbAnimationItem.CbRectangle.UsingProxy = false;
			//}
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

		#endregion
	}
}

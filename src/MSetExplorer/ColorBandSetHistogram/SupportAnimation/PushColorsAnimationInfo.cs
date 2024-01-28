using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MSetExplorer.ColorBandSetHistogram.SupportAnimation
{
	internal class PushColorsAnimationInfo
	{
		private CbListViewElevations _elevations;

		public PushColorsAnimationInfo(CbListViewElevations elevations)
		{
			_elevations = elevations;

			ColorBlocksAnimationItems = new List<ColorBlocksAnimationItem>();
			BlendedColorAnimationItems = new List<BlendedColorAnimationItem>();
		}

		public List<ColorBlocksAnimationItem> ColorBlocksAnimationItems { get; init; }
		public List<BlendedColorAnimationItem> BlendedColorAnimationItems { get; init; }


		#region Public Methods

		public void Add(CbListViewItem source, CbListViewItem? destination)
		{
			var colorBlocksAItem = new ColorBlocksAnimationItem(source, destination);
			ColorBlocksAnimationItems.Add(colorBlocksAItem);

			var blendedColorAItem = new BlendedColorAnimationItem(source, destination);
			BlendedColorAnimationItems.Add(blendedColorAItem);
		}

		public void CalculateMovements()
		{
			var liftHeight = _elevations.ColorBlocksHeight;

			CalculateColorBlockMovements(liftHeight);

			CalculateBlendedBandMovements(liftHeight);
		}

		public void CalculateColorBlockMovements(double liftHeight)
		{
			var firstHDist = GetFirstMovementDistanceForBlocks(liftHeight);

			foreach (var colorBlockItem in ColorBlocksAnimationItems)
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
			}
		}

		public void CalculateBlendedBandMovements(double liftHeight)
		{
			var firstHDist = GetFirstMovementDistanceForBlends(liftHeight);

			foreach (var blendedItem in BlendedColorAnimationItems)
			{
				// Lift
				blendedItem.FirstMovement = new Point(blendedItem.Current.X + firstHDist, blendedItem.Current.Y - liftHeight);
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
			foreach (var cbAnimationItem in ColorBlocksAnimationItems)
			{
				cbAnimationItem.CbColorBlocks.UsingProxy = true;
				cbAnimationItem.CbSectionLine.TopArrowVisibility = Visibility.Hidden;
			}

			foreach (var cbAnimationItem in BlendedColorAnimationItems)
			{
				cbAnimationItem.CbRectangle.UsingProxy = true;
			}
		}

		public void TearDown()
		{
			foreach (var cbAnimationItem in ColorBlocksAnimationItems)
			{
				if (cbAnimationItem.DestinationListViewItem != null && cbAnimationItem.SourceListViewItem.CbColorBlock.CbColorPairProxy != null)
				{
					var cc = cbAnimationItem.DestinationListViewItem.CbColorBlock.CbColorPair;
					cbAnimationItem.DestinationListViewItem.CbColorBlock.CbColorPair = cbAnimationItem.SourceListViewItem.CbColorBlock.CbColorPairProxy;
					cc.TearDown();
				}




				cbAnimationItem.CbColorBlocks.UsingProxy = false;
				cbAnimationItem.CbSectionLine.TopArrowVisibility = Visibility.Visible;
			}

			foreach (var cbAnimationItem in BlendedColorAnimationItems)
			{
				if (cbAnimationItem.DestinationListViewItem != null && cbAnimationItem.SourceListViewItem.CbRectangle.CbBlendedColorPairProxy != null)
				{
					var cc = cbAnimationItem.DestinationListViewItem.CbRectangle.CbBlendedColorPair;
					cbAnimationItem.DestinationListViewItem.CbRectangle.CbBlendedColorPair = cbAnimationItem.SourceListViewItem.CbRectangle.CbBlendedColorPairProxy;
					cc.TearDown();
				}
				cbAnimationItem.CbRectangle.UsingProxy = false;
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

			var minDist = ColorBlocksAnimationItems.Min(x => x.GetDistance());
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

			var minDist = BlendedColorAnimationItems.Min(x => x.GetDistance());
			var firstMovementDistMax = minDist / 2;

			var result = Math.Min(firstMovementDistMax, liftHeight);

			return result;
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using Windows.UI.WebUI;

namespace MSetExplorer.ColorBandSetHistogram.SupportAnimation
{
	internal class PushColorsAnimationInfo
	{
		private CbListViewElevations _elevations;

		public PushColorsAnimationInfo(CbListViewElevations elevations)
		{
			_elevations = elevations;

			ColorBlocksAnimationItems = new List<ColorBlocksAnimationItem>();
		}

		public List<ColorBlocksAnimationItem> ColorBlocksAnimationItems { get; init; }

		public void Add(CbListViewItem source, CbListViewItem? destination)
		{
			ColorBlocksAnimationItem item;

			if (destination != null)
			{
				item = new ColorBlocksAnimationItem(source, destination.CbColorBlock.ColorPairContainer);
			}
			else
			{
				// The destination is just off the edge of the visible portion of the canvas.
				var sourceRect = source.CbColorBlock.ColorPairContainer;

				var width = source.CbColorBlock.Width * source.CbColorBlock.ContentScale.Width;
				var destinationPosition = new Point(sourceRect.X + width + 5, sourceRect.Top);

				var destRect = new Rect(destinationPosition, sourceRect.Size);

				item = new ColorBlocksAnimationItem(source, destRect);
			}

			ColorBlocksAnimationItems.Add(item);
		}

		public void CalculateMovements()
		{
			var liftHeight = _elevations.ColorBlocksHeight;
			var firstHDist = GetFirstTimelineHorizontalDistance(liftHeight);

			foreach(var cbAnimationItem in ColorBlocksAnimationItems)
			{
				// Lift
				cbAnimationItem.FirstMovement = new Point(cbAnimationItem.Current.X + firstHDist, cbAnimationItem.Current.Y - liftHeight);
				cbAnimationItem.Stage1 = new Rect(cbAnimationItem.FirstMovement, cbAnimationItem.Current.Size);

				// Resize
				cbAnimationItem.Stage2 = new Rect(cbAnimationItem.Stage1.Location, cbAnimationItem.Destination.Size);

				// Shift Right
				//cbAnimationItem.SecondMovement = new Point(cbAnimationItem.Destination.X - firstHDist, cbAnimationItem.Stage2.Y);
				cbAnimationItem.SecondMovement = new Point(cbAnimationItem.Destination.X - firstHDist, cbAnimationItem.Current.Y - liftHeight);
				cbAnimationItem.Stage3 = new Rect(cbAnimationItem.SecondMovement, cbAnimationItem.Destination.Size);

				// Drop
				cbAnimationItem.ThirdMovement = cbAnimationItem.Destination.Location;
				//cbAnimationItem.Stage4 = cbAnimationItem.Destination;
			}
		}

		public double GetFirstTimelineHorizontalDistance(double liftHeight)
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

		public void TearDown()
		{
			foreach (var cbAnimationItem in ColorBlocksAnimationItems)
			{
				cbAnimationItem.TearDown();
			}
		}

	}
}

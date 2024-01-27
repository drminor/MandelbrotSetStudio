using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Windows.UI.WebUI;

namespace MSetExplorer.ColorBandSetHistogram.SupportAnimation
{
	internal class PushColorsAnimationInfo
	{
		private Canvas _canvas;
		private CbListViewElevations _elevations;

		public PushColorsAnimationInfo(Canvas canvas, CbListViewElevations elevations)
		{
			_canvas = canvas;
			_elevations = elevations;

			ColorBlocksAnimationItems = new List<ColorBlocksAnimationItem>();
		}

		public List<ColorBlocksAnimationItem> ColorBlocksAnimationItems { get; init; }

		public void Add(string name, CbColorBlocks source, CbColorBlocks? destination)
		{
			if (destination != null)
			{
				ColorBlocksAnimationItems.Add(new ColorBlocksAnimationItem(name, source, destination.ColorPairContainer));
			}
			else
			{
				// The destination is just off the edge of the visible portion of the canvas.
				var sourceRect = source.CbColorPair.Container;

				//var destinationPosition = new Point(_canvas.ActualWidth + 10, sourceRect.Top);

				var width = source.Width * source.ContentScale.Width;
				var destinationPosition = new Point(sourceRect.X + width + 5, sourceRect.Top);

				var destRect = new Rect(destinationPosition, sourceRect.Size);

				ColorBlocksAnimationItems.Add(new ColorBlocksAnimationItem(name, source, destRect));
			}
		}

		public void CalculateMovements()
		{
			var liftHeight = _elevations.ColorBlocksHeight;
			var firstHDist = GetFirstTimelineHorizontalDistance(liftHeight);

			foreach(var cbAnimationItem in ColorBlocksAnimationItems)
			{
				cbAnimationItem.FirstMovement = new Point(cbAnimationItem.Current.X + firstHDist, cbAnimationItem.Current.Y - liftHeight);
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

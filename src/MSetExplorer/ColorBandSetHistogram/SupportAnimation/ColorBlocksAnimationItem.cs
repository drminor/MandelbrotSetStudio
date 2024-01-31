using System;
using MSS.Types;
using System.Windows;

namespace MSetExplorer
{
	internal class ColorBlocksAnimationItem
	{
		public ColorBlocksAnimationItem(CbListViewItem sourceListViewItem, CbListViewItem? destinationListViewItem)
		{
			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			Source = sourceListViewItem.CbColorBlock.CbColorPair.Container;

			Destination = destinationListViewItem?.CbColorBlock.CbColorPair.Container
				?? GetOffScreenRect(sourceListViewItem);

			StartingPos = Source;
			Size1 = Destination.Size;

			ShiftDuration = TimeSpan.FromMilliseconds(300);

			Size1 = Source.Width > Destination.Width ? Destination.Size: Source.Size;
		}

		#region Public Properties

		public CbListViewItem SourceListViewItem { get; init; }
		public string Name => SourceListViewItem.Name;
		public CbListViewItem? DestinationListViewItem { get; init; }

		public CbSectionLine CbSectionLine => SourceListViewItem.CbSectionLine;

		public Rect Source { get; init; }
		public Rect Destination { get; init; }

		public Rect StartingPos { get; set; }
		public Rect PosAfterLift { get; set; }
		public Rect PosAfterResize1 { get; set; }
		public Rect PosAfterShift { get; set; }
		public Rect PosAfterResize2 { get; set; }

		public Size Size1 { get; init; }
		public TimeSpan ShiftDuration { get; set; }

		//public bool SourceIsWiderThanDest => Source.Width > Destination.Width;

		#endregion

		#region Public Methods

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

		public double GetDistance()
		{
			var result = Destination.Left - Source.Left;
			return result;
		}

		public double GetShiftDistance()
		{
			var result = PosAfterShift.Left - PosAfterLift.Left;
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

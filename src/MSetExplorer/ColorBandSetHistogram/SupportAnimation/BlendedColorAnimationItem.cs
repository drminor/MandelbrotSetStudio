using MSS.Types;
using System;
using System.Windows;

namespace MSetExplorer
{
	internal class BlendedColorAnimationItem
	{
		public BlendedColorAnimationItem(CbListViewItem sourceListViewItem, CbListViewItem? destinationListViewItem)
		{
			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			Source = sourceListViewItem.CbRectangle.CbBlendedColorPair.Container;

			Destination = destinationListViewItem?.CbRectangle.CbBlendedColorPair.Container
				?? GetOffScreenRect(sourceListViewItem);

			StartingPos = Source;
			Size1 = Destination.Size;
			ShiftDuration1 = TimeSpan.FromMilliseconds(300);

			Size1 = Source.Width > Destination.Width ? Destination.Size : Source.Size;
			Size2 = Size1;
		}

		#region Public Properties

		public CbListViewItem SourceListViewItem { get; init; }
		public string Name => SourceListViewItem.Name;
		public CbListViewItem? DestinationListViewItem { get; init; }

		public Rect Source { get; init; }
		public Rect Destination { get; init; }

		public Rect StartingPos { get; set; }
		public Rect PosAfterLift { get; set; }
		public Rect PosAfterShift1 { get; set; }
		public Rect PosAfterShift2 { get; set; }

		public Rect PosAfterResize3 { get; set; }

		public Rect PosBeforeDrop { get; set; }

		public Size Size1 { get; set; }
		public Size Size2 { get; set; }
		//public Size Size3 { get; set; }

		public double ShiftAmount1 { get; set; }
		public double ShiftAmount2 { get; set; }
		public double ShiftAmount3 { get; set; }

		public TimeSpan ShiftDuration1 { get; set; }
		public TimeSpan ShiftDuration2 { get; set; }
		public TimeSpan ShiftDuration3 { get; set; }

		//public bool SourceIsWiderThanDest => Source.Width > Destination.Width;

		#endregion

		#region Public Methods

		public void MoveSourceToDestination()
		{
			if (DestinationListViewItem != null)
			{
				var newCopy = SourceListViewItem.CbRectangle.CbBlendedColorPair.Clone();

				if (DestinationListViewItem.ColorBand.IsLast)
				{
					newCopy.EndColor = ColorBandColor.Black;
				}

				DestinationListViewItem.CbRectangle.CbBlendedColorPair = newCopy;

				SourceListViewItem.CbRectangle.CbBlendedColorPair.TearDown();
			}
		}

		public double GetDistance()
		{
			var result = Destination.Left - Source.Left;

			return result;
		}

		public double GetShiftDistance()
		{
			var result = PosBeforeDrop.Left - PosAfterLift.Left;
			return result;
		}

		#endregion

		private static Rect GetOffScreenRect(CbListViewItem source)
		{
			// The destination is just off the edge of the visible portion of the canvas.
			var sourceRect = source.CbRectangle.ColorPairContainer;

			var width = source.CbRectangle.Width * source.CbRectangle.ContentScale.Width;
			var destinationPosition = new Point(sourceRect.X + width + 5, sourceRect.Top);

			var destRect = new Rect(destinationPosition, sourceRect.Size);

			return destRect;
		}
	}
}

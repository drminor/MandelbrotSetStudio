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
			ShiftFraction = 1.0;
			ShiftDuration = TimeSpan.FromMilliseconds(300);

			Size1 = SourceIsWiderThanDest ? Destination.Size : Source.Size;
		}

		#region Public Properties

		public CbListViewItem SourceListViewItem { get; init; }
		public string Name => SourceListViewItem.Name;
		public CbListViewItem? DestinationListViewItem { get; init; }

		public Rect Source { get; init; }
		public Rect Destination { get; init; }

		public Rect StartingPos { get; set; }
		public Rect PosAfterLift { get; set; }
		public Rect PosAfterResize1 { get; set; }
		public Rect PosAfterShift { get; set; }
		public Rect PosAfterResize2 { get; set; }

		public Point LiftDestination { get; set; }

		public Size Size1 { get; set; }

		public Point ShiftDestination { get; set; }
		public Point DropDestination { get; set; }

		public double ShiftFraction { get; set; }
		public TimeSpan ShiftDuration { get; set; }

		public bool SourceIsWiderThanDest => Source.Width > Destination.Width;

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
			var result = PosAfterShift.Left - PosAfterLift.Left;
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

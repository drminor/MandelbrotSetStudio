using System.Windows;

namespace MSetExplorer
{
	internal class BlendedColorAnimationItem
	{
		public BlendedColorAnimationItem(CbListViewItem sourceListViewItem, CbListViewItem? destinationListViewItem)
		{
			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			Source = CbRectangle.CbBlendedColorPair.Container;
			Destination = destinationListViewItem != null ? destinationListViewItem.CbRectangle.ColorPairContainer : GetOffScreenRect(sourceListViewItem);

			Current = Source;
		}

		#region Public Properties

		public CbListViewItem SourceListViewItem { get; init; }
		public string Name => SourceListViewItem.Name;

		public CbRectangle CbRectangle => SourceListViewItem.CbRectangle;

		public CbListViewItem? DestinationListViewItem { get; init; }

		public Rect Source { get; init; }
		public Rect Destination { get; init; }

		public Rect Current { get; set; }
		public Rect Stage1 { get; set; }
		public Rect Stage2 { get; set; }
		public Rect Stage3 { get; set; }

		public Point FirstMovement { get; set; }

		public Point SecondMovement { get; set; }
		public Point ThirdMovement { get; set; }

		#endregion

		#region Public Methods

		public double GetDistance()
		{
			var result = Destination.Left - Source.Left;

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

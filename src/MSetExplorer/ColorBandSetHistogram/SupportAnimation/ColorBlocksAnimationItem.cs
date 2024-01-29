using System.Windows;

namespace MSetExplorer
{
	internal class ColorBlocksAnimationItem
	{
		public ColorBlocksAnimationItem(CbListViewItem sourceListViewItem, CbListViewItem? destinationListViewItem)
		{
			SourceListViewItem = sourceListViewItem;
			DestinationListViewItem = destinationListViewItem;

			Source = CbColorBlocks.CbColorPair.Container;
			Destination = destinationListViewItem != null ? destinationListViewItem.CbColorBlock.ColorPairContainer : GetOffScreenRect(sourceListViewItem);

			Current = Source;
		}

		#region Public Properties

		public CbListViewItem SourceListViewItem { get; init; }
		public string Name => SourceListViewItem.Name;

		public CbSectionLine CbSectionLine => SourceListViewItem.CbSectionLine;
		public CbColorBlocks CbColorBlocks => SourceListViewItem.CbColorBlock;

		public CbListViewItem? DestinationListViewItem { get; init; }

		public CbColorPair SourceCbColorPair => SourceListViewItem.CbColorBlock.CbColorPair;
		//public CbColorPair? DestinationCbColorPair => DestinationListViewItem?.CbColorBlock.CbColorPair;



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

		public void MoveSourceToDestination()
		{
			if (DestinationListViewItem != null)
			{
 				var newCopy = SourceListViewItem.CbColorBlock.CbColorPair.Clone();
				
				DestinationListViewItem.CbColorBlock.CbColorPair = newCopy;

				SourceListViewItem.CbColorBlock.CbColorPair.TearDown();

			}
		}

		public double GetDistance()
		{
			var result = Destination.Left - Source.Left;

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

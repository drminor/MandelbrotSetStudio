using System.Windows;

namespace MSetExplorer
{
	internal class ColorBlocksAnimationItem
	{
		public ColorBlocksAnimationItem(CbListViewItem cbListViewItem, Rect destination)
		{
			CbListViewItem = cbListViewItem;

			CbColorBlocks.UsingProxy = true;
			CbColorBlocks.ColorPairVisibility = Visibility.Hidden;

			cbListViewItem.CbSectionLine.TopArrowVisibility = Visibility.Hidden;

			Source = CbColorBlocks.CbColorPair.Container;
			Destination = destination;

			Current = Source;
		}

		#region Public Properties

		public CbListViewItem CbListViewItem { get; init; }
		public string Name => CbListViewItem.Name;

		public CbColorBlocks CbColorBlocks => CbListViewItem.CbColorBlock;

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

		public void TearDown()
		{
			CbColorBlocks.UsingProxy = false;
			CbColorBlocks.ColorPairVisibility = Visibility.Visible;
			CbListViewItem.CbSectionLine.TopArrowVisibility = Visibility.Visible;
		}

		#endregion

	}
}

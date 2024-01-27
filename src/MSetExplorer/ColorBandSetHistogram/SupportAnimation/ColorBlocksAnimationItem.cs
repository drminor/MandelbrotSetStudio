using System.Windows;

namespace MSetExplorer
{
	internal class ColorBlocksAnimationItem
	{
		public ColorBlocksAnimationItem(string name, CbColorBlocks cbColorBlocks, Rect destination)
		{
			Name = name;

			CbColorBlocks = cbColorBlocks;

			cbColorBlocks.UsingProxy = true;
			cbColorBlocks.ColorPairVisibility = Visibility.Hidden;

			Source = cbColorBlocks.CbColorPair.Container;
			Destination = destination;

			Current = Source;
		}

		#region Public Properties

		public string Name { get; init; }

		public CbColorBlocks CbColorBlocks { get; init; }
		//public CbColorPair CbColorPairProxy { get; init; }

		public Rect Source { get; init; }
		public Rect Destination { get; init; }

		public Rect Current { get; set; }

		public Point FirstMovement { get; set; }

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
		}

		#endregion

	}
}

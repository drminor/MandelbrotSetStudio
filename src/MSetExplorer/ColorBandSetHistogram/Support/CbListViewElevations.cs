using System;
using System.Diagnostics;

namespace MSetExplorer
{
	public class CbListViewElevations : ViewModelBase, ICloneable
	{
		#region Private Fields

		private const int DEFAULT_SECTION_LINES_HEIGHT = 13;
		private const int DEFAULT_COLOR_BLOCKS_HEIGHT = 15;
		private const int MINIMUM_BLEND_RECTANGLES_HEIGHT = 12;
		private const int IS_CURRENT_INDICATORS_HEIGHT = 3;

		private double _elevation;
		private double _controlHeight;


		#endregion

		#region Constructor

		public CbListViewElevations(double elevation, double controlHeight)
		{
			_elevation = elevation;
			_controlHeight = controlHeight;

			UpdateElevationsAndHeights(_elevation, _controlHeight);

		}

		#endregion

		#region Public Properties

		public double Elevation
		{
			get => _elevation;
			set
			{
				if (value != _elevation)
				{
					_elevation = value;

					UpdateElevationsAndHeights(Elevation, ControlHeight);
					//OnPropertyChanged();
				}
			}
		}

		public double ControlHeight
		{
			get => _controlHeight;

			set
			{
				if (value != _controlHeight)
				{
					_controlHeight = value;

					UpdateElevationsAndHeights(Elevation, ControlHeight);
					//OnPropertyChanged();
				}
			}
		}

		public double SectionLinesElevation { get; private set; }
		public double SectionLinesHeight { get; private set; }
		public double ColorBlocksElevation { get; private set; }
		public double ColorBlocksHeight { get; private set; }
		public double BlendRectanglesElevation { get; private set; }
		public double BlendRectanglesHeight { get; private set; }
		public double IsCurrentIndicatorsElevation { get; private set; }
		public double IsCurrentIndicatorsHeight { get; private set; }

		#endregion

		#region Public Methods

		public void SetElevationAndHeight(double elevation, double height)
		{
			UpdateElevationsAndHeights(elevation, height);
			//OnPropertyChanged(nameof(ControlHeight));
			//OnPropertyChanged(nameof(Elevation));
		}

		object ICloneable.Clone() => Clone();

		public CbListViewElevations Clone()
		{
			var result = new CbListViewElevations(Elevation, ControlHeight);
			return result;
		}

		#endregion

		#region Private Methods

		private void UpdateElevationsAndHeights(double elevation, double controlHeight)
		{
			_elevation = elevation;
			_controlHeight = controlHeight;

			SectionLinesElevation = elevation;

			var firstThreshold = MINIMUM_BLEND_RECTANGLES_HEIGHT + DEFAULT_SECTION_LINES_HEIGHT + DEFAULT_COLOR_BLOCKS_HEIGHT + IS_CURRENT_INDICATORS_HEIGHT;
			var secondThreshold = MINIMUM_BLEND_RECTANGLES_HEIGHT + DEFAULT_COLOR_BLOCKS_HEIGHT + IS_CURRENT_INDICATORS_HEIGHT;
			var thirdThreshold = MINIMUM_BLEND_RECTANGLES_HEIGHT + IS_CURRENT_INDICATORS_HEIGHT;

			if (controlHeight >= firstThreshold)
			{
				SectionLinesHeight = DEFAULT_SECTION_LINES_HEIGHT;
				ColorBlocksHeight = DEFAULT_COLOR_BLOCKS_HEIGHT;
				IsCurrentIndicatorsHeight = IS_CURRENT_INDICATORS_HEIGHT;

				BlendRectanglesHeight = controlHeight - (SectionLinesHeight + ColorBlocksHeight + IsCurrentIndicatorsHeight);
			}
			else if (controlHeight >= secondThreshold)
			{
				BlendRectanglesHeight = MINIMUM_BLEND_RECTANGLES_HEIGHT;
				SectionLinesHeight = controlHeight - (ColorBlocksHeight + BlendRectanglesHeight + IsCurrentIndicatorsHeight);
				ColorBlocksHeight = DEFAULT_COLOR_BLOCKS_HEIGHT;
				IsCurrentIndicatorsHeight = IS_CURRENT_INDICATORS_HEIGHT;
			}
			else if (controlHeight >= thirdThreshold)
			{
				SectionLinesHeight = 0;
				BlendRectanglesHeight = MINIMUM_BLEND_RECTANGLES_HEIGHT;
				ColorBlocksHeight = controlHeight - (BlendRectanglesHeight + IsCurrentIndicatorsHeight);
				IsCurrentIndicatorsHeight = IS_CURRENT_INDICATORS_HEIGHT;
			}
			else
			{
				SectionLinesHeight = 0;
				ColorBlocksHeight = 0;
				IsCurrentIndicatorsHeight = 0;
				BlendRectanglesHeight = controlHeight - 2;
			}

			ColorBlocksElevation = SectionLinesElevation + SectionLinesHeight;
			BlendRectanglesElevation = ColorBlocksElevation + ColorBlocksHeight;
			IsCurrentIndicatorsElevation = BlendRectanglesElevation + BlendRectanglesHeight;

			Debug.WriteLine($"CbListViewElevations. ControlHeight - IsCurElevation = {controlHeight - IsCurrentIndicatorsElevation}.");
		}

		#endregion
	}
}

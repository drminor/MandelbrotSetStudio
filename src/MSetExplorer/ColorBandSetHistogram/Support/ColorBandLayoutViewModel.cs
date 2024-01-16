using MSS.Types;
using System;
using System.Security.Policy;
using System.Windows.Controls;

namespace MSetExplorer
{
	public class ColorBandLayoutViewModel : ViewModelBase
	{
		#region Private Fields

		private const int SECTION_LINES_HEIGHT = 13;
		private const int COLOR_BLOCKS_HEIGHT = 15;
		private const int IS_CURRENT_INDICATORS_HEIGHT = 3;

		private SizeDbl _contentScale;

		private double _controlHeight;
		private double _blendRectanglesHeight;

		private double _selectionLinesElevation;
		private double _colorBlocksElevation;
		private double _blendRectangesElevation;
		private double _isCurrentIndicatorsElevation;

		private bool _parentIsFocused;

		#endregion

		#region Constructor

		public ColorBandLayoutViewModel(SizeDbl contentScale, double controlHeight, bool parentIsFocused, Canvas canvas, IsSelectedChangedCallback isSelectedChangedCallback, Action<int, ColorBandSetEditMode> requestContextMenuShown)
		{
			if (contentScale.IsNAN() || contentScale.Width == 0)
			{
				_contentScale = new SizeDbl(1);
			}
			else
			{
				_contentScale = new SizeDbl(contentScale.Width, 1);
			}

			_controlHeight = controlHeight;
			_blendRectanglesHeight = GetBlendRectanglesHeight(controlHeight);

			_selectionLinesElevation = 0;
			_colorBlocksElevation = _selectionLinesElevation + SECTION_LINES_HEIGHT;
			_blendRectangesElevation = _colorBlocksElevation + COLOR_BLOCKS_HEIGHT;

			_isCurrentIndicatorsElevation = _blendRectangesElevation + BlendRectangelsHeight;

			_parentIsFocused = parentIsFocused;
			Canvas = canvas;
			IsSelectedChangedCallback = isSelectedChangedCallback;
			RequestContextMenuShown = requestContextMenuShown;
		}

		#endregion

		#region Public Properties

		public Canvas Canvas { get; init; }
		public IsSelectedChangedCallback IsSelectedChangedCallback { get; init; }
		public Action<int, ColorBandSetEditMode> RequestContextMenuShown { get; init; }

		public SizeDbl ContentScale
		{
			get => _contentScale;

			set
			{
				if (value.Width != _contentScale.Width)
				{
					_contentScale = new SizeDbl(value.Width, 1);
					OnPropertyChanged();
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

					BlendRectangelsHeight = GetBlendRectanglesHeight(_controlHeight);
					OnPropertyChanged();
				}
			}
		}

		public double SectionLinesHeight => SECTION_LINES_HEIGHT;

		public double ColorBlocksHeight => COLOR_BLOCKS_HEIGHT;

		public double BlendRectangelsHeight
		{
			get => _blendRectanglesHeight;
			set
			{
				if (value != _blendRectanglesHeight)
				{
					_blendRectanglesHeight = value;
					IsCurrentIndicatorsElevation = _blendRectangesElevation + BlendRectangelsHeight;
					OnPropertyChanged();
				}
			}
		}

		public double IsCurrentIndicatorsHeight => IS_CURRENT_INDICATORS_HEIGHT;

		public double SectionLinesElevation => _selectionLinesElevation;
		public double ColorBlocksElevation => _colorBlocksElevation;
		public double BlendRectangesElevation => _blendRectangesElevation;

		public double IsCurrentIndicatorsElevation
		{
			get => _isCurrentIndicatorsElevation;
			set
			{
				if (value != _isCurrentIndicatorsElevation)
				{
					_isCurrentIndicatorsElevation = value;
					OnPropertyChanged();
				}
			}
		}

		public bool ParentIsFocused
		{
			get => _parentIsFocused;
			set
			{
				if (value != _parentIsFocused)
				{
					_parentIsFocused = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Private Methods

		private double GetBlendRectanglesHeight(double controlHeight)
		{
			var result = controlHeight - (SECTION_LINES_HEIGHT + COLOR_BLOCKS_HEIGHT + IS_CURRENT_INDICATORS_HEIGHT);
			result = Math.Max(result, 0);

			return result;
		}

		#endregion
	}
}

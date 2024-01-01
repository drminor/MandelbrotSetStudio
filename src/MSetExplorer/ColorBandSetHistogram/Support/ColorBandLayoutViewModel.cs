﻿using MSS.Types;

namespace MSetExplorer
{
	public class ColorBandLayoutViewModel : ViewModelBase
	{
		#region Private Fields

		private const int SELECTION_LINE_SELECTOR_HEIGHT = 15;
		private const int SELECTOR_HEIGHT_BOTTOM_PADDING = 5;
		private const double BORDER_THICKNESS = 1.0;

		private SizeDbl _contentScale;
		private double _controlHeight;
		private double _cbrHeight;

		#endregion

		#region Constructor

		public ColorBandLayoutViewModel(SizeDbl contentScale, double controlHeight)
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
			_cbrHeight = GetCbrHeight(controlHeight);
		}

		#endregion

		#region Public Properties

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

					CbrHeight = GetCbrHeight(_controlHeight);
					OnPropertyChanged();
				}
			}
		}

		public double CbrElevation => SELECTION_LINE_SELECTOR_HEIGHT;

		public double CbrHeight
		{
			get => _cbrHeight;
			set
			{
				if (value != _cbrHeight)
				{
					_cbrHeight = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Private Methods

		private double GetCbrHeight(double controlHeight)
		{
			var cbrHeight = controlHeight - (SELECTION_LINE_SELECTOR_HEIGHT + SELECTOR_HEIGHT_BOTTOM_PADDING);

			return cbrHeight;
		}

		#endregion
	}
}
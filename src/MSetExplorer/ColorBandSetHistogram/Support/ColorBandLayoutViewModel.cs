using MSS.Types;

namespace MSetExplorer
{
	public class ColorBandLayoutViewModel : ViewModelBase
	{
		#region Private Fields

		//private const int SCROLL_BAR_HEIGHT = 17;
		private const int SELECTION_LINE_SELECTOR_HEIGHT = 15;
		private const int SELECTOR_HEIGHT_BOTTOM_PADDING = 5;
		private const double BORDER_THICKNESS = 1.0;

		private SizeDbl _contentScale;
		private double _controlHeight;
		//private bool _isHorizontalScrollBarVisible;
		private double _cbrElevation;
		private double _cbrHeight;

		#endregion

		#region Constructor

		public ColorBandLayoutViewModel(SizeDbl contentScale, double controlHeight/*, bool isHorizontalScrollBarVisible*/, double cbrElevation, double cbrHeight)
		{
			_contentScale = contentScale;
			_controlHeight = controlHeight;
			//_isHorizontalScrollBarVisible = isHorizontalScrollBarVisible;
			_cbrElevation = cbrElevation;
			_cbrHeight = cbrHeight;
		}

		#endregion

		#region Public Properties

		public SizeDbl ContentScale
		{
			get => _contentScale;

			set
			{
				if (value != _contentScale)
				{
					_contentScale = value;
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

					(CbrElevation, CbrHeight) = GetCbrElevationAndHeight(_controlHeight/*, _isHorizontalScrollBarVisible*/);
					OnPropertyChanged();
				}
			}
		}

		//public bool IsHorizontalScrollBarVisible
		//{
		//	get => _isHorizontalScrollBarVisible;
		//	set
		//	{
		//		if (value != _isHorizontalScrollBarVisible)
		//		{
		//			_isHorizontalScrollBarVisible = value;
		//			//(CbrElevation, CbrHeight) = GetCbrElevationAndHeight(_controlHeight/*, _isHorizontalScrollBarVisible*/);
		//		}
		//	}
		//}

		public double CbrElevation
		{
			get => _cbrElevation;
			set
			{
				if (value != _cbrElevation)
				{
					_cbrElevation = value;
					OnPropertyChanged();
				}
			}
		}

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

		private (double, double) GetCbrElevationAndHeight(double controlHeight/*, bool isHorizontalScrollBarVisible*/)
		{
			var elevation = SELECTION_LINE_SELECTOR_HEIGHT;
			var cbrHeight = controlHeight - (SELECTION_LINE_SELECTOR_HEIGHT + SELECTOR_HEIGHT_BOTTOM_PADDING);

			//if (isHorizontalScrollBarVisible)
			//{
			//	cbrHeight -= SCROLL_BAR_HEIGHT;
			//}

			return (elevation, cbrHeight);
		}


		#endregion
	}
}

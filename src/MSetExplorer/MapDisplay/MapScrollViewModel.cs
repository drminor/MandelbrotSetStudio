
using MSS.Types;

namespace MSetExplorer
{
	internal class MapScrollViewModel : ViewModelBase, IMapScrollViewModel
	{
		private double _invertedVerticalPosition;
		private double _verticalPosition;
		private double _horizontalPosition;
		private SizeInt? _posterSize;

		#region Constructor

		public MapScrollViewModel(IMapDisplayViewModel mapDisplayViewModel)
		{
			MapDisplayViewModel = mapDisplayViewModel;
		}

		#endregion

		#region Public Properties 

		public IMapDisplayViewModel MapDisplayViewModel { get; init; }


		public double InvertedVerticalPosition
		{
			get => _invertedVerticalPosition;
			private set
			{
				if (value != _invertedVerticalPosition)
				{
					_invertedVerticalPosition = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.InvertedVerticalPosition));
				}
			}
		}


		public double VerticalPosition
		{
			get => _verticalPosition;
			set
			{
				if (value != _verticalPosition)
				{
					_verticalPosition = value;
					//InvertedVerticalPosition = GetInvertedYPos(value);
					InvertedVerticalPosition = _verticalPosition;
					OnPropertyChanged(nameof(IMapScrollViewModel.VerticalPosition));
				}
			}
		}

		public double HorizontalPosition
		{
			get => _horizontalPosition;
			set
			{
				if (value != _horizontalPosition)
				{
					_horizontalPosition = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.HorizontalPosition));
				}
			}
		}

		public SizeInt? PosterSize
		{
			get => _posterSize;

			set
			{
				if (value != _posterSize)
				{
					_posterSize = value;
					//InvertedVerticalPosition = GetInvertedYPos(VerticalPosition);
					OnPropertyChanged(nameof(IMapScrollViewModel.PosterSize));
				}
			}
		}

		#endregion

		#region Private Methods

		private double GetInvertedYPos(double yPos)
		{
			double result = 0;

			if (PosterSize.HasValue)
			{
				result = PosterSize.Value.Height - yPos;
				var logicalDisplayHeight = MapDisplayViewModel.LogicalDisplaySize.Height;
				result -= logicalDisplayHeight;
			}

			return result;
		}

		#endregion

	}
}

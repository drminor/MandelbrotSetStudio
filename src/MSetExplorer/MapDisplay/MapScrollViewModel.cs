
using MSS.Types;

namespace MSetExplorer
{
	internal class MapScrollViewModel : ViewModelBase, IMapScrollViewModel
	{
		//private double _vMax;
		//private double _hMax;

		private double _verticalPosition;
		private double _horizontalPosition;

		#region Constructor

		public MapScrollViewModel(IMapDisplayViewModel mapDisplayViewModel)
		{
			MapDisplayViewModel = mapDisplayViewModel;
			//VMax = 1024;
			//HMax = 1024;
		}

		#endregion

		#region Public Properties 

		public IMapDisplayViewModel MapDisplayViewModel { get; init; }

		public double VerticalPosition
		{
			get => _verticalPosition;
			set
			{
				if (value != _verticalPosition)
				{
					_verticalPosition = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.VerticalPosition));
				}
			}
		}

		public double HorizontalPosition
		{
			get => _horizontalPosition;
			set
			{
				if (value != _verticalPosition)
				{
					_horizontalPosition = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.HorizontalPosition));
				}
			}
		}

		private SizeInt? _posterSize;
		public SizeInt? PosterSize
		{
			get => _posterSize;

			set
			{
				if (value != _posterSize)
				{
					_posterSize = value;
					OnPropertyChanged(nameof(IMapScrollViewModel.PosterSize));
				}
			}
		}

		//public double VMax
		//{
		//	get => _vMax;
		//	set
		//	{
		//		if (value != _vMax)
		//		{
		//			_vMax = value;
		//			OnPropertyChanged(nameof(IMapScrollViewModel.VMax));
		//		}
		//	}
		//}

		//public double HMax
		//{
		//	get => _hMax;
		//	set
		//	{
		//		if (value != _hMax)
		//		{
		//			_hMax = value;
		//			OnPropertyChanged(nameof(IMapScrollViewModel.HMax));
		//		}
		//	}
		//}

		#endregion

	}
}

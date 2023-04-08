using MSetExplorer.MapDisplay.ScrollAndZoom;
using MSS.Types;
using System.ComponentModel;

namespace MSetExplorer
{
	public class ColorBandSetHistogramViewModel : ViewModelBase
	{
		private readonly IMapSectionHistogramProcessor _mapSectionHistogramProcessor;
		private ColorBandSet _colorBandSet;

		private int _dispWidth;
		private int _dispHeight;

		#region Constructor

		public ColorBandSetHistogramViewModel(IMapSectionHistogramProcessor mapSectionHistogramProcessor)
		{
			_mapSectionHistogramProcessor = mapSectionHistogramProcessor;
			_colorBandSet = new ColorBandSet();

			var cbshDisplayViewModel = new CbshDisplayViewModel(_mapSectionHistogramProcessor);
			CbshScrollViewModel = new CbshScrollViewModel(cbshDisplayViewModel);
			CbshScrollViewModel.PropertyChanged += CbshScrollViewModel_PropertyChanged;

			CbshDisplayViewModel.PropertyChanged += CbshDisplayViewModel_PropertyChanged;

			DispWidth = CbshScrollViewModel.CanvasSize.Width;
			DispHeight = CbshScrollViewModel.CanvasSize.Height;
		}

		#endregion

		#region Public Properties

		public CbshScrollViewModel CbshScrollViewModel { get; init; }
		public CbshDisplayViewModel CbshDisplayViewModel => CbshScrollViewModel.CbshDisplayViewModel;

		public ColorBandSet ColorBandSet
		{
			get => _colorBandSet;
			set
			{
				if (value != _colorBandSet)
				{
					_colorBandSet = value;
					CbshDisplayViewModel.ColorBandSet =_colorBandSet;

					OnPropertyChanged();
				}
			}
		}

		public int DispWidth
		{
			get => _dispWidth;
			set
			{
				if (value != _dispWidth)
				{
					_dispWidth = value;
					OnPropertyChanged();
				}
			}
		}

		public int DispHeight
		{
			get => _dispHeight;
			set
			{
				if (value != _dispHeight)
				{
					_dispHeight = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		public void RefreshHistogramDisplay()
		{
			//CbshDisplayViewModel.RefreshHistogramDisplay();
		}

		#endregion

		#region Event Handlers - Display and Scroll

		private void CbshDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CbshDisplayViewModel.CanvasSize))
			{
				DispWidth = CbshDisplayViewModel.CanvasSize.Width;
				DispHeight = CbshDisplayViewModel.CanvasSize.Height;
			}

			if (e.PropertyName == nameof(CbshDisplayViewModel.LogicalDisplaySize))
			{
				//PosterViewModel.LogicalDisplaySize = MapDisplayViewModel.LogicalDisplaySize;
			}
		}

		private void CbshScrollViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CbshScrollViewModel.HorizontalPosition))
			{
				//PosterViewModel.DisplayPosition = new VectorInt((int)Math.Round(MapScrollViewModel.HorizontalPosition), PosterViewModel.DisplayPosition.Y);
			}

			else if (e.PropertyName == nameof(CbshScrollViewModel.InvertedVerticalPosition))
			{
				//PosterViewModel.DisplayPosition = new VectorInt(PosterViewModel.DisplayPosition.X, (int)Math.Round(MapScrollViewModel.InvertedVerticalPosition));
			}

			else if (e.PropertyName == nameof(CbshScrollViewModel.DisplayZoom))
			{
				//PosterViewModel.DisplayZoom = MapScrollViewModel.DisplayZoom;
			}
		}

		#endregion
	}
}

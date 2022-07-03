using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	public class CbshScrollViewModel : ViewModelBase
	{
		private double _invertedVerticalPosition;
		private double _verticalPosition;
		private double _horizontalPosition;

		private SizeInt _canvasSize;
		private SizeInt? _histogramSize;

		private double _displayZoom;
		private double _maximumDisplayZoom;

		#region Constructor

		public CbshScrollViewModel(CbshDisplayViewModel cbshDisplayViewModel)
		{
			CbshDisplayViewModel = cbshDisplayViewModel;
			_displayZoom = 1;
			_maximumDisplayZoom = 1;

			CanvasSize = CbshDisplayViewModel.CanvasSize;

			CbshDisplayViewModel.PropertyChanged += CbshDisplayViewModel_PropertyChanged;
		}

		private void CbshDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(CbshDisplayViewModel.CanvasSize))
			{
				CanvasSize = CbshDisplayViewModel.CanvasSize;
			}

			//if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentJobAreaAndCalcSettings))
			//{
			//	PosterSize = MapDisplayViewModel.CurrentJobAreaAndCalcSettings?.MapAreaInfo.CanvasSize;
			//}
		}

		#endregion

		#region Public Properties 

		public CbshDisplayViewModel CbshDisplayViewModel { get; init; }

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					_canvasSize = value;
					MaximumDisplayZoom = GetMaximumDisplayZoom(HistogramSize, CanvasSize);

					OnPropertyChanged(nameof(IMapScrollViewModel.CanvasSize));
				}
			}
		}

		public SizeInt? HistogramSize
		{
			get => _histogramSize;

			set
			{
				if (value != _histogramSize)
				{
					_histogramSize = value;
					InvertedVerticalPosition = GetInvertedYPos(VerticalPosition);

					MaximumDisplayZoom = GetMaximumDisplayZoom(HistogramSize, CanvasSize);

					OnPropertyChanged(nameof(IMapScrollViewModel.PosterSize));
				}
			}
		}

		/// <summary>
		/// Value between 0.0 and 1.0
		/// 1.0 presents 1 map "pixel" to 1 screen pixel
		/// 0.5 presents 2 map "pixels" to 1 screen pixel
		/// </summary>
		public double DisplayZoom
		{
			get => _displayZoom;
			set
			{
				if (Math.Abs(value - DisplayZoom) > 0.001)
				{
					_displayZoom = Math.Min(MaximumDisplayZoom, value);

					CbshDisplayViewModel.DisplayZoom = _displayZoom;

					Debug.WriteLine($"The DispZoom is {DisplayZoom}.");
					OnPropertyChanged(nameof(IMapScrollViewModel.DisplayZoom));
				}
			}
		}

		public double MaximumDisplayZoom
		{
			get => _maximumDisplayZoom;
			private set
			{
				if (Math.Abs(value - _maximumDisplayZoom) > 0.001)
				{
					_maximumDisplayZoom = value;
					Debug.WriteLine($"The MaxDispZoom is {MaximumDisplayZoom}.");

					if (DisplayZoom > MaximumDisplayZoom)
					{
						DisplayZoom = MaximumDisplayZoom;
					}

					OnPropertyChanged(nameof(IMapScrollViewModel.MaximumDisplayZoom));
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
					_invertedVerticalPosition = GetInvertedYPos(value);
					Debug.WriteLine($"Vertical Pos: {VerticalPosition}, Inverted: {InvertedVerticalPosition}.");
					OnPropertyChanged(nameof(IMapScrollViewModel.VerticalPosition));
					OnPropertyChanged(nameof(IMapScrollViewModel.InvertedVerticalPosition));
				}
			}
		}

		public double InvertedVerticalPosition
		{
			get => _invertedVerticalPosition;
			set
			{
				if (value != _invertedVerticalPosition)
				{
					_invertedVerticalPosition = value;
					_verticalPosition = GetInvertedYPos(value);
					Debug.WriteLine($"Vertical Pos: {VerticalPosition}, Inverted: {InvertedVerticalPosition}.");
					OnPropertyChanged(nameof(IMapScrollViewModel.InvertedVerticalPosition));
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
					Debug.WriteLine($"Horizontal Pos: {value}.");
					OnPropertyChanged(nameof(IMapScrollViewModel.HorizontalPosition));
				}
			}
		}

		#endregion

		#region Private Methods

		private double GetInvertedYPos(double yPos)
		{
			double result = 0;

			if (HistogramSize.HasValue)
			{
				result = HistogramSize.Value.Height - yPos;
				var logicalDisplayHeight = CbshDisplayViewModel.LogicalDisplaySize.Height;
				result -= logicalDisplayHeight;
			}

			return result;
		}

		private double GetMaximumDisplayZoom(SizeInt? posterSize, SizeInt displaySize)
		{
			double result;

			if (posterSize is null || displaySize.Width < 100 || displaySize.Height < 100)
			{
				result = 1;
			}
			else
			{
				var pixelsPerSampleHorizontal = posterSize.Value.Width / (double)displaySize.Width;
				var pixelsPerSampleVertical = posterSize.Value.Height / (double)displaySize.Height;

				result = Math.Min(pixelsPerSampleHorizontal, pixelsPerSampleVertical);
				if (result < 1)
				{
					result = 1;
				}
			}

			return result;
		}

		#endregion

	}


}

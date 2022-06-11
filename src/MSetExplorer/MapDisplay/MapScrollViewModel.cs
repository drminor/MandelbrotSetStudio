
using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class MapScrollViewModel : ViewModelBase, IMapScrollViewModel
	{
		private double _invertedVerticalPosition;
		private double _verticalPosition;
		private double _horizontalPosition;

		private SizeInt _canvasSize;
		private SizeInt? _posterSize;

		private double _displayZoom;
		private double _maximumDisplayZoom;

		#region Constructor

		public MapScrollViewModel(IMapDisplayViewModel mapDisplayViewModel)
		{
			MapDisplayViewModel = mapDisplayViewModel;
			_displayZoom = 1;
			_maximumDisplayZoom = 1;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				CanvasSize = MapDisplayViewModel.CanvasSize;
			}
		}

		#endregion

		#region Public Properties 

		public IMapDisplayViewModel MapDisplayViewModel { get; init; }

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if (value != _canvasSize)
				{
					_canvasSize = value;
					MaximumDisplayZoom = GetMaximumDisplayZoom(PosterSize, CanvasSize);

					OnPropertyChanged(nameof(IMapScrollViewModel.CanvasSize));
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
					InvertedVerticalPosition = GetInvertedYPos(VerticalPosition);

					MaximumDisplayZoom = GetMaximumDisplayZoom(PosterSize, CanvasSize);

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

					VerticalPosition = 0;
					HorizontalPosition = 0;

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
					//InvertedVerticalPosition = _verticalPosition;

					Debug.WriteLine($"Vertical Pos: {value}, Inverted: {InvertedVerticalPosition}.");

					OnPropertyChanged(nameof(IMapScrollViewModel.VerticalPosition));
				}

				InvertedVerticalPosition = GetInvertedYPos(value);
			}
		}

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

			if (PosterSize.HasValue)
			{
				result = PosterSize.Value.Height - yPos;
				var logicalDisplayHeight = MapDisplayViewModel.LogicalDisplaySize.Height;
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

using MSS.Types;
using System;
using System.Diagnostics;

namespace MSetExplorer.MapDisplay.ScrollAndZoom
{
	internal class MapScrollViewModel : ViewModelBase, IMapScrollViewModel
	{
		private double _invertedVerticalPosition;
		private double _verticalPosition;
		private double _horizontalPosition;

		private SizeDbl _canvasSize;
		private SizeInt? _posterSize;

		private double _displayZoom;
		private double _maximumDisplayZoom;

		#region Constructor

		public MapScrollViewModel(IMapDisplayViewModel mapDisplayViewModel)
		{
			MapDisplayViewModel = mapDisplayViewModel;
			_displayZoom = 1;
			_maximumDisplayZoom = 1;

			CanvasSize = MapDisplayViewModel.CanvasSize;

			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				CanvasSize = MapDisplayViewModel.CanvasSize;
			}

			//if (e.PropertyName == nameof(IMapDisplayViewModel.CurrentJobAreaAndCalcSettings))
			//{
			//	PosterSize = MapDisplayViewModel.CurrentJobAreaAndCalcSettings?.MapAreaInfo.CanvasSize;
			//}
		}

		#endregion

		#region Public Properties 

		public IMapDisplayViewModel MapDisplayViewModel { get; init; }

		public SizeDbl CanvasSize
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
				//if (Math.Abs(value - DisplayZoom) > 0.001)
				//{
				//	_displayZoom = Math.Min(MaximumDisplayZoom, value);

				//	MapDisplayViewModel.DisplayZoom = _displayZoom;

				//	Debug.WriteLine($"The DispZoom is {DisplayZoom}.");
				//	OnPropertyChanged(nameof(IMapScrollViewModel.DisplayZoom));
				//}

				var previousValue = _displayZoom;

				_displayZoom = Math.Min(MaximumDisplayZoom, value);

				MapDisplayViewModel.DisplayZoom = _displayZoom;

				Debug.WriteLine($"The MapScrollViewModel's DisplayZoom is being updated to {DisplayZoom}, the previous value is {previousValue}.");
				// Log: Add Spacer
				Debug.WriteLine("\n\n");
				OnPropertyChanged(nameof(IMapScrollViewModel.DisplayZoom));
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

					if (DisplayZoom > MaximumDisplayZoom)
					{
						Debug.WriteLine($"The MapScrollViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being adjusted to be less or equal to this.");
						DisplayZoom = MaximumDisplayZoom;
					}
					else
					{
						Debug.WriteLine($"The MapScrollViewModel's MaxDispZoom is being updated to {MaximumDisplayZoom} and the DisplayZoom is being kept the same.");
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

			if (PosterSize.HasValue)
			{
				result = PosterSize.Value.Height - yPos;
				var logicalDisplayHeight = MapDisplayViewModel.LogicalDisplaySize.Height;
				result -= logicalDisplayHeight;
			}

			return result;
		}

		private double GetMaximumDisplayZoom(SizeInt? posterSize, SizeDbl displaySize)
		{
			double result;

			if (posterSize is null || displaySize.Width < 100 || displaySize.Height < 100)
			{
				result = 1;
			}
			else
			{
				var pixelsPerSampleHorizontal = posterSize.Value.Width / displaySize.Width;
				var pixelsPerSampleVertical = posterSize.Value.Height / displaySize.Height;

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

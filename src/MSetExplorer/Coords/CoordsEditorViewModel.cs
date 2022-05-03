using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Globalization;

namespace MSetExplorer
{
	public class CoordsEditorViewModel : ViewModelBase
	{
		private SingleCoordEditorViewModel _startingX;
		private SingleCoordEditorViewModel _endingX;
		private SingleCoordEditorViewModel _startingY;
		private SingleCoordEditorViewModel _endingY;

		private long _zoom;

		//private RRectangle _coords;
		//private bool _coordsAreDirty;

		public CoordsEditorViewModel(string x1, string x2, string y1, string y2)
		{
			//_coords = RMapConstants.TEST_RECTANGLE_HALF;

			_startingX = new SingleCoordEditorViewModel(x1);
			_endingX = new SingleCoordEditorViewModel(x2);
			_startingY = new SingleCoordEditorViewModel(y1);
			_endingY = new SingleCoordEditorViewModel(y2);
		}

		#region Public Properties

		//public event EventHandler<MapSettingsUpdateRequestedEventArgs>? MapSettingsUpdateRequested;

		public SingleCoordEditorViewModel StartingX
		{
			get => _startingX;
			set
			{
				if (value != _startingX)
				{
					_startingX = value;
					OnPropertyChanged();
				}
			}
		}

		public SingleCoordEditorViewModel EndingX
		{
			get => _endingX;
			set
			{
				if (value != _endingX)
				{
					_endingX = value;
					OnPropertyChanged();
				}
			}
		}

		public SingleCoordEditorViewModel StartingY
		{
			get => _startingY;
			set
			{
				if (value != _startingY)
				{
					_startingY = value;
					OnPropertyChanged();
				}
			}
		}

		public SingleCoordEditorViewModel EndingY
		{
			get => _endingY;
			set
			{
				if (value != _endingY)
				{
					_endingY = value;
					OnPropertyChanged();
				}
			}
		}

		//public RRectangle Coords
		//{
		//	get => _coords;
		//	set
		//	{
		//		if (value != _coords)
		//		{
		//			_coords = value;
		//			//StartingX = RValueHelper.ConvertToString(_coords.Left);
		//			//EndingX = RValueHelper.ConvertToString(_coords.Right);
		//			//StartingY = RValueHelper.ConvertToString(_coords.Bottom);
		//			//EndingY = RValueHelper.ConvertToString(_coords.Top);

		//			CoordsAreDirty = true;

		//			Zoom = RValueHelper.GetResolution(_coords.Width, out var precision);
		//			Precision = precision;

		//			OnPropertyChanged();
		//		}
		//	}
		//}

		//public bool CoordsAreDirty
		//{
		//	get => _coordsAreDirty;

		//	private set
		//	{
		//		if (value != _coordsAreDirty)
		//		{
		//			_coordsAreDirty = value;
		//			OnPropertyChanged();
		//		}
		//	}
		//}

		public long Zoom
		{
			get => _zoom;
			set
			{
				_zoom = value;
				OnPropertyChanged();
				OnPropertyChanged(nameof(Precision));
			}
		}

		public int Precision
		{
			get => -1;
			set => OnPropertyChanged();
		}

		#endregion

		#region Public Methods

		public void SaveCoords() 
		{
			//// TODO: Validate new values

			//	MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.NewProject, Coords, TargetIterations, RequestsPerJob));
			//}
			//else
			//{
			//	MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.Coordinates, Coords));
			//}
		}

		//public void TriggerIterationUpdate()
		//{
		//	MapSettingsUpdateRequested?.Invoke(this, new MapSettingsUpdateRequestedEventArgs(MapSettingsUpdateType.TargetIterations, TargetIterations));
		//}

		#endregion

		#region Private Methods

		#endregion
	}
}

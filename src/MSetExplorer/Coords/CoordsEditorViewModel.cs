using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer
{
	public class CoordsEditorViewModel : ViewModelBase
	{
		private const int _numDigitsForDisplayExtent = 4;

		private readonly SizeInt _displaySize;
		private readonly SizeInt _blockSize;

		private RRectangle _coords;
		private bool _coordsAreDirty;
		private string _zoom;

		#region Constructor

		public CoordsEditorViewModel(MapJobHelper mapJobHelper, MapAreaInfo2 mapAreaInfoV2, SizeInt displaySize, bool allowEdits)
		{
			var mapAreaInfo = mapJobHelper.GetMapAreaWithSizeFat(mapAreaInfoV2, _displaySize);
			_coords = mapAreaInfo.Coords;

			StartingX = new SingleCoordEditorViewModel(_coords.Left);
			EndingX = new SingleCoordEditorViewModel(_coords.Right);

			StartingY = new SingleCoordEditorViewModel(_coords.Bottom);
			EndingY= new SingleCoordEditorViewModel(_coords.Top);

			_displaySize = displaySize;
			EditsAllowed = allowEdits;
			_blockSize = RMapConstants.BLOCK_SIZE;

			MapCoordsDetail1 = new MapCoordsDetailViewModel(_coords);
			_zoom = RValueHelper.GetFormattedResolution(_coords.Width);

			MapCoordsDetail2 = new MapCoordsDetailViewModel(mapAreaInfo);
		}

		#endregion

		#region Event Handlers

		private void StartingX_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		private void EndingX_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		private void StartingY_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		private void EndingY_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}

		#endregion

		#region Public Properties

		public bool EditsAllowed { get; init; }

		public SingleCoordEditorViewModel StartingX { get; init; }
		public SingleCoordEditorViewModel EndingX { get; init; }
		public SingleCoordEditorViewModel StartingY { get; init; }
		public SingleCoordEditorViewModel EndingY { get; init; }


		public string Zoom
		{
			get => _zoom;
			set
			{
				_zoom = value;
				OnPropertyChanged();
			}
		}

		public MapCoordsDetailViewModel MapCoordsDetail1 { get; init; }
		public MapCoordsDetailViewModel MapCoordsDetail2 { get; init; }

		public RRectangle Coords
		{
			get => _coords;
			set
			{
				if (value != _coords)
				{
					_coords = value;
					//StartingX = RValueHelper.ConvertToString(_coords.Left);
					//EndingX = RValueHelper.ConvertToString(_coords.Right);
					//StartingY = RValueHelper.ConvertToString(_coords.Bottom);
					//EndingY = RValueHelper.ConvertToString(_coords.Top);

					CoordsAreDirty = true;

					Zoom = RValueHelper.GetFormattedResolution(_coords.Width);

					OnPropertyChanged();
				}
			}
		}

		public bool CoordsAreDirty
		{
			get => _coordsAreDirty;

			private set
			{
				if (value != _coordsAreDirty)
				{
					_coordsAreDirty = value;
					OnPropertyChanged();
				}
			}
		}

		#endregion

		#region Public Methods

		#endregion

		#region Private Methods

		private RRectangle GetCoords(SingleCoordEditorViewModel[] vms)
		{
			var startingX = vms[0];
			var endingX = vms[1];
			var startingY = vms[2];
			var endingY = vms[3];

			var precisionX = RValueHelper.GetPrecision(startingX.RValue, endingX.RValue, out var _);
			//var width = RValueHelper.ConvertToString(diffX, useSciNotationForLengthsGe: 6);

			precisionX += _numDigitsForDisplayExtent;
			var newX1Sme = startingX.SignManExp.ReducePrecisionTo(precisionX);
			var newX2Sme = endingX.SignManExp.ReducePrecisionTo(precisionX);

			var precisionY = RValueHelper.GetPrecision(startingY.RValue, endingY.RValue, out var _);
			//var height = RValueHelper.ConvertToString(diffY, useSciNotationForLengthsGe: 6);

			precisionY += _numDigitsForDisplayExtent;
			var newY1Sme = startingY.SignManExp.ReducePrecisionTo(precisionY);
			var newY2Sme = endingY.SignManExp.ReducePrecisionTo(precisionY);

			var result = RValueHelper.BuildRRectangle(new SignManExp[] { newX1Sme, newX2Sme,	newY1Sme, newY2Sme });

			return result;
		}

		#endregion
	}
}

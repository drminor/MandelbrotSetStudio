using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;

namespace MSetExplorer
{
	public class CoordsEditorViewModel : ViewModelBase
	{
		private const string MONGO_DB_CONN_STRING = "mongodb://localhost:27017";

		private const int _numDigitsForDisplayExtent = 4;

		private readonly SizeInt _displaySize;
		private readonly SizeInt _blockSize;

		private RRectangle _coords;
		private bool _coordsAreDirty;
		private long _zoom;


		#region Constructor

		public CoordsEditorViewModel(RRectangle coords, SizeInt displaySize) : this(new SingleCoordEditorViewModel[] {
			new SingleCoordEditorViewModel(coords.Left), new SingleCoordEditorViewModel(coords.Right),
			new SingleCoordEditorViewModel(coords.Bottom), new SingleCoordEditorViewModel(coords.Top) }, displaySize)
		{ }

		public CoordsEditorViewModel(string x1, string x2, string y1, string y2, SizeInt displaySize) : this(new SingleCoordEditorViewModel[] { 
			new SingleCoordEditorViewModel(x1), new SingleCoordEditorViewModel(x2),
			new SingleCoordEditorViewModel(y1), new SingleCoordEditorViewModel(y2) }, displaySize)
		{ }

		private CoordsEditorViewModel(SingleCoordEditorViewModel[] vms, SizeInt displaySize)
		{
			StartingX = vms[0];
			EndingX = vms[1];
			StartingY = vms[2];
			EndingY = vms[3];

			_displaySize = displaySize;
			_blockSize = RMapConstants.BLOCK_SIZE;

			MapCoordsDetail1 = new MapCoordsDetailViewModel(new RValue[] { StartingX.RValue, EndingX.RValue, StartingY.RValue, EndingY.RValue });
			MapCoordsDetail2 = new MapCoordsDetailViewModel(new RValue[] { MapCoordsDetail1.StartingX, MapCoordsDetail1.EndingX, MapCoordsDetail1.StartingY, MapCoordsDetail1.EndingY});

			_coords = GetCoords();
			_zoom = RValueHelper.GetResolution(_coords.Width);
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

		public SingleCoordEditorViewModel StartingX { get; init; }
		public SingleCoordEditorViewModel EndingX { get; init; }
		public SingleCoordEditorViewModel StartingY { get; init; }
		public SingleCoordEditorViewModel EndingY { get; init; }


		public long Zoom
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

					Zoom = RValueHelper.GetResolution(_coords.Width);

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

		private RRectangle GetCoords()
		{
			var precisionX = RValueHelper.GetPrecision(StartingX.RValue, EndingX.RValue, out var diffX);
			//var width = RValueHelper.ConvertToString(diffX, useSciNotationForLengthsGe: 6);

			precisionX += _numDigitsForDisplayExtent;
			var newX1Sme = StartingX.SignManExp.ReducePrecisionTo(precisionX);
			var newX2Sme = EndingX.SignManExp.ReducePrecisionTo(precisionX);

			//MapCoordsDetail1.PrecisionX = precisionX;
			//MapCoordsDetail1.Width = width;

			//MapCoordsDetail1.X1 = newX1Sme.GetValueAsString();
			//MapCoordsDetail1.X2 = newX2Sme.GetValueAsString();

			var precisionY = RValueHelper.GetPrecision(StartingX.RValue, EndingX.RValue, out var diffY);
			//var height = RValueHelper.ConvertToString(diffY, useSciNotationForLengthsGe: 6);

			precisionY += _numDigitsForDisplayExtent;
			var newY1Sme = StartingY.SignManExp.ReducePrecisionTo(precisionY);
			var newY2Sme = EndingY.SignManExp.ReducePrecisionTo(precisionY);

			//MapCoordsDetail1.PrecisionY = precisionY;
			//MapCoordsDetail1.Height = height;

			//MapCoordsDetail1.Y1 = newY1Sme.GetValueAsString();
			//MapCoordsDetail1.Y2 = newY2Sme.GetValueAsString();

			var result = RValueHelper.BuildRRectangleFromRVals(
				RValueHelper.ConvertToRValue(newX1Sme),
				RValueHelper.ConvertToRValue(newX2Sme),
				RValueHelper.ConvertToRValue(newY1Sme),
				RValueHelper.ConvertToRValue(newY2Sme)
				);

			var projectAdapter = MSetRepoHelper.GetProjectAdapter(MONGO_DB_CONN_STRING);
			var jobAreaInfo = MapJobHelper.GetJobAreaInfo(result, _displaySize, _blockSize, projectAdapter);

			MapCoordsDetail2.Coords = jobAreaInfo.Coords;

			//if (raisePropertyChangedEvents)
			//{
			//	OnPropertyChanged(nameof(X1));
			//	OnPropertyChanged(nameof(X2));
			//	OnPropertyChanged(nameof(Y1));
			//	OnPropertyChanged(nameof(Y2));
			//	OnPropertyChanged(nameof(Width));
			//	OnPropertyChanged(nameof(Height));
			//	OnPropertyChanged(nameof(PrecisionX));
			//	OnPropertyChanged(nameof(PrecisionY));
			//}

			return result;
		}

		#endregion

		#region Private Methods

		#endregion
	}
}

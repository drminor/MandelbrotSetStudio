using MSetRepo;
using MSS.Common;
using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class MainWindowViewModel : ViewModelBase, IMainWindowViewModel 
	{
		private readonly ProjectOpenSaveViewModelCreator _projectOpenSaveViewModelCreator;
		private readonly CbsOpenSaveViewModelCreator _cbsOpenSaveViewModelCreator;

		#region Constructor

		public MainWindowViewModel(IMapProjectViewModel mapProjectViewModel, IMapDisplayViewModel mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel, 
			ProjectOpenSaveViewModelCreator projectOpenSaveViewModelCreator, CbsOpenSaveViewModelCreator cbsOpenSaveViewModelCreator)
		{
			MapProjectViewModel = mapProjectViewModel;
			MapProjectViewModel.PropertyChanged += MapProjectViewModel_PropertyChanged;

			MapDisplayViewModel = mapDisplayViewModel;
			MapDisplayViewModel.PropertyChanged += MapDisplayViewModel_PropertyChanged;
			MapDisplayViewModel.MapViewUpdateRequested += MapDisplayViewModel_MapViewUpdateRequested;

			MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			DispWidth = MapDisplayViewModel.CanvasSize.Width;
			DispHeight = MapDisplayViewModel.CanvasSize.Height;

			_projectOpenSaveViewModelCreator = projectOpenSaveViewModelCreator;
			_cbsOpenSaveViewModelCreator = cbsOpenSaveViewModelCreator;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;

			MSetInfoViewModel = new MSetInfoViewModel();
			MSetInfoViewModel.MapSettingsUpdateRequested += MSetInfoViewModel_MapSettingsUpdateRequested;
		}

		private void MSetInfoViewModel_MapSettingsUpdateRequested(object? sender, MapSettingsUpdateRequestedEventArgs e)
		{
			if (e.MapSettingsUpdateType == MapSettingsUpdateType.TargetIterations)
			{
				ColorBandSetViewModel.HighCutOff = e.TargetIterations;
				MapProjectViewModel.UpdateTargetInterations(e.TargetIterations);
			}
			else if (e.MapSettingsUpdateType == MapSettingsUpdateType.Coordinates)
			{
				Debug.WriteLine($"MainWindow ViewModel received request to update the coords.");
			}
		}

		#endregion

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapProjectViewModel MapProjectViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }
		public MSetInfoViewModel MSetInfoViewModel { get; }

		private int _dispWidth;

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

		private int _dispHeight;

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

		//public void ExportColorBandSet(string id)
		//{
		//	//_sharedColorBandSetAdapter.CreateColorBandSet();
		//}

		//public void ImportColorBandSet(string id)
		//{
		//}


		public IProjectOpenSaveViewModel CreateAProjectOpenSaveViewModel(string? initalName, DialogType dialogType)
		{
			var result = _projectOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public IColorBandSetOpenSaveViewModel CreateACbsOpenViewModel(string? initalName, DialogType dialogType)
		{
			var result = _cbsOpenSaveViewModelCreator(initalName, dialogType);
			return result;
		}

		public void TestDiv()
		{
			var rectDividen = RMapConstants.TEST_RECTANGLE_HALF;

			var x1 = rectDividen.LeftBot.X;  // 1/2 -- 0.5
			var x2 = rectDividen.RightTop.X; // 2/2 -- 1.0

			var v1 = new RValue(x2.Value - x1.Value, x2.Exponent); // 1/2

			var divisorTarget = 128 * 10; // Not an integer power of 2
			var divisorUsed = 128 * 16; // 2 ^ 7 + 4

			var ratFromUsedToTarget = divisorTarget / (double) divisorUsed;

			var divisorUsedRRecprical = new RValue(1, 11);
			var spdUsed = new RValue(1, 12);
			var spdTarget = new RValue(5, 20); // 1 / 2 ^ 11 * 5 / 8

			// new X2 = X1 + rat * width / divisorUsed == 5/8 * (0.5 / (128 * 16))

			//var newX2 = new RValue

			// 1280 * 5 / ( 1 / 2^20)


			// 1/2 divided into 1280 parts
			// vs

			// 1/2 * 8/5 / 128 * 10 * 8/5
			// 1/2 / 128 * 16


			// X2 = 1/2 + 1/2
			// == 1/2 + 128 * 16 * (1/2 / (128 * 16))

			// == 1/2 + 


			// 1/12 = 2/24 / 4/48 / 8/96 / 16/192


		}

		#endregion

		#region Event Handlers

		private void MapProjectViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			//if (e.PropertyName == nameof(IMapProjectViewModel.CurrentProject))
			//{
			//	ColorBandSetViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
			//	MapDisplayViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
			//}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentJob))
			{
				var curJob = MapProjectViewModel.CurrentJob;

				MSetInfoViewModel.MSetInfo = curJob?.MSetInfo;
				MapDisplayViewModel.CurrentJob = curJob;
			}

			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet.Clone();
				MapDisplayViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			MapProjectViewModel.UpdateMapView(e.TransformType, e.NewArea);
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				DispWidth = MapDisplayViewModel.CanvasSize.Width;
				DispHeight = MapDisplayViewModel.CanvasSize.Height;
				MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			}
		}

		private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.ColorBandSet))
			{
				var cbs = ColorBandSetViewModel.ColorBandSet;
				if (cbs != null)
				{
					MapProjectViewModel.CurrentColorBandSet = cbs;
				}
			}
		}

		#endregion
	}
}

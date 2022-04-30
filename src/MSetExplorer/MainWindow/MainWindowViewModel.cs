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

		private int _dispWidth;
		private int _dispHeight;

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
			ColorBandSetViewModel.ColorBandSetUpdateRequested += ColorBandSetViewModel_ColorBandSetUpdateRequested;

			MSetInfoViewModel = new MSetInfoViewModel();
			MSetInfoViewModel.MapSettingsUpdateRequested += MSetInfoViewModel_MapSettingsUpdateRequested;
		}

		#endregion

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapProjectViewModel MapProjectViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }
		public MSetInfoViewModel MSetInfoViewModel { get; }

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

		//public void TestSumOld()
		//{
		//	var s = "0.3323822021484375";
		//	var rVal = RValueHelper.ConvertToRValue(s, 0);
		//	RValueHelper.Test(rVal);
		//}

		public void TestSum()
		{
			//var s = "0.152816772460937588";
			var s = "0.53557582168176593030695927477";

			var rVal = RValueHelper.Test2(s);
		}


		public void TestDivOld()
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
			// Update the MSet Info and Map Display with the new Job
			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentJob))
			{
				var curJob = MapProjectViewModel.CurrentJob;

				MSetInfoViewModel.CurrentJob = curJob;
				MapDisplayViewModel.CurrentJob = curJob;
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IMapProjectViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;
				//MapDisplayViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet; //.CreateNewCopy();
			}
		}

		private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.UseEscapeVelocities))
			{
				MapDisplayViewModel.UseEscapeVelocities = ColorBandSetViewModel.UseEscapeVelocities;
			}
		}

		private void MapDisplayViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Let the Map Project know about Map Display size changes
			if (e.PropertyName == nameof(IMapDisplayViewModel.CanvasSize))
			{
				DispWidth = MapDisplayViewModel.CanvasSize.Width;
				DispHeight = MapDisplayViewModel.CanvasSize.Height;
				MapProjectViewModel.CanvasSize = MapDisplayViewModel.CanvasSize;
			}
		}

		private void MapDisplayViewModel_MapViewUpdateRequested(object? sender, MapViewUpdateRequestedEventArgs e)
		{
			if (e.IsPreview)
			{
				// Calculate new Coords for preview
				var newCoords = MapProjectViewModel.GetUpdateCoords(e.TransformType, e.NewArea);
				if (newCoords != null)
				{
					MSetInfoViewModel.Coords = newCoords;
				}
			}
			else
			{
				// Zoom or Pan Map Coordinates
				MapProjectViewModel.UpdateMapView(e.TransformType, e.NewArea);
			}
		}

		private void MSetInfoViewModel_MapSettingsUpdateRequested(object? sender, MapSettingsUpdateRequestedEventArgs e)
		{
			// Update the Target Iterations
			if (e.MapSettingsUpdateType == MapSettingsUpdateType.TargetIterations)
			{
				ColorBandSetViewModel.ApplyChanges(e.TargetIterations);
			}

			// Jump to new Coordinates
			else if (e.MapSettingsUpdateType == MapSettingsUpdateType.Coordinates)
			{
				Debug.WriteLine($"MainWindow ViewModel received request to update the coords.");
				MapProjectViewModel.UpdateMapCoordinates(e.Coords);
			}
		}

		private void ColorBandSetViewModel_ColorBandSetUpdateRequested(object? sender, ColorBandSetUpdateRequestedEventArgs e)
		{
			var colorBandSet = e.ColorBandSet;

			if (e.IsPreview)
			{
				Debug.WriteLine($"MainWindow got a CBS preview with Id = {colorBandSet.Id}");
				MapDisplayViewModel.ColorBandSet = colorBandSet;
			}
			else
			{
				Debug.WriteLine($"MainWindow got a CBS update with Id = {colorBandSet.Id}");
				// TODO: Include the ColorBandSet in the Job class, instead of just the ColorBandSetId.
				// This will allow the MapDisplayViewModel to handle Color changes as the Job is changed.
				//MapDisplayViewModel.ColorBandSet = colorBandSet;

				MapProjectViewModel.UpdateColorBandSet(colorBandSet);
			}
		}

		#endregion
	}
}

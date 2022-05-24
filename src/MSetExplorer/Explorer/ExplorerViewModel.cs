using MSS.Common;
using MSS.Types;
using System.ComponentModel;
using System.Diagnostics;

namespace MSetExplorer
{
	public class ExplorerViewModel : ViewModelBase, IExplorerViewModel 
	{
		private readonly ProjectOpenSaveViewModelCreator _projectOpenSaveViewModelCreator;
		private readonly CbsOpenSaveViewModelCreator _cbsOpenSaveViewModelCreator;

		private int _dispWidth;
		private int _dispHeight;

		#region Constructor

		public ExplorerViewModel(IMapProjectViewModel mapProjectViewModel, IMapDisplayViewModel mapDisplayViewModel, ColorBandSetViewModel colorBandViewModel, 
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

			MapCoordsViewModel = new MapCoordsViewModel();

			MapCalcSettingsViewModel = new MapCalcSettingsViewModel();
			MapCalcSettingsViewModel.MapSettingsUpdateRequested += MapCalcSettingsViewModel_MapSettingsUpdateRequested;

			ColorBandSetViewModel = colorBandViewModel;
			ColorBandSetViewModel.PropertyChanged += ColorBandViewModel_PropertyChanged;
			ColorBandSetViewModel.ColorBandSetUpdateRequested += ColorBandSetViewModel_ColorBandSetUpdateRequested;
		}

		#endregion

		#region Public Properties

		public IMapDisplayViewModel MapDisplayViewModel { get; }
		public IMapProjectViewModel MapProjectViewModel { get; }

		public MapCoordsViewModel MapCoordsViewModel { get; } 
		public MapCalcSettingsViewModel MapCalcSettingsViewModel { get; }
		public ColorBandSetViewModel ColorBandSetViewModel { get; }

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

		#endregion

		#region Event Handlers

		private void MapProjectViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			// Update the MSet Info and Map Display with the new Job
			if (e.PropertyName == nameof(IMapProjectViewModel.CurrentJob))
			{
				var curJob = MapProjectViewModel.CurrentJob;

				MapCalcSettingsViewModel.CurrentJob = curJob;
				MapCoordsViewModel.CurrentJob = curJob;
				MapDisplayViewModel.CurrentJob = curJob;
			}

			// Update the ColorBandSet View and the MapDisplay View with the newly selected ColorBandSet
			else if (e.PropertyName == nameof(IMapProjectViewModel.CurrentColorBandSet))
			{
				ColorBandSetViewModel.ColorBandSet = MapProjectViewModel.CurrentColorBandSet;

				if (MapProjectViewModel.CurrentProject != null)
				{
					MapDisplayViewModel.SetColorBandSet(MapProjectViewModel.CurrentColorBandSet, updateDisplay: true);
				}
			}
		}

		private void ColorBandViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ColorBandSetViewModel.UseEscapeVelocities))
			{
				MapDisplayViewModel.UseEscapeVelocities = ColorBandSetViewModel.UseEscapeVelocities;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.HighlightSelectedBand))
			{
				MapDisplayViewModel.HighlightSelectedColorBand = ColorBandSetViewModel.HighlightSelectedBand;
			}

			if (e.PropertyName == nameof(ColorBandSetViewModel.CurrentColorBand))
			{
				if (MapProjectViewModel.CurrentProject != null && MapDisplayViewModel.HighlightSelectedColorBand && ColorBandSetViewModel.ColorBandSet != null)
				{
					MapDisplayViewModel.SetColorBandSet(ColorBandSetViewModel.ColorBandSet, updateDisplay: true);
				}
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
				var jobAreaInfo = MapProjectViewModel.GetUpdatedJobAreaInfo(e.TransformType, e.NewArea);
				if (jobAreaInfo != null)
				{
					MapCoordsViewModel.Preview(jobAreaInfo);
				}
			}
			else
			{
				// Zoom or Pan Map Coordinates
				MapProjectViewModel.UpdateMapView(e.TransformType, e.NewArea);
			}
		}

		private void MapCalcSettingsViewModel_MapSettingsUpdateRequested(object? sender, MapSettingsUpdateRequestedEventArgs e)
		{
			// Update the Target Iterations
			if (e.MapSettingsUpdateType == MapSettingsUpdateType.TargetIterations)
			{
				ColorBandSetViewModel.ApplyChanges(e.TargetIterations);
			}
		}

		private void ColorBandSetViewModel_ColorBandSetUpdateRequested(object? sender, ColorBandSetUpdateRequestedEventArgs e)
		{
			var colorBandSet = e.ColorBandSet;

			if (e.IsPreview)
			{
				Debug.WriteLine($"MainWindow got a CBS preview with Id = {colorBandSet.Id}");
				MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: true);
			}
			else
			{
				Debug.WriteLine($"MainWindow got a CBS update with Id = {colorBandSet.Id}");
				MapDisplayViewModel.SetColorBandSet(colorBandSet, updateDisplay: false);
				MapProjectViewModel.UpdateColorBandSet(colorBandSet);
			}
		}

		#endregion
	}
}

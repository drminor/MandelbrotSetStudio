using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		private readonly IProjectAdapter _projectAdapter;

		private SizeDbl _canvasSize;
		private SizeDbl _logicalDisplaySize;

		private Poster? _currentPoster;

		JobAreaAndCalcSettings _jobAreaAndCalcSettings;

		#region Constructor

		public PosterViewModel(IProjectAdapter projectAdapter)
		{
			_projectAdapter = projectAdapter;

			_canvasSize = new SizeDbl();
			_currentPoster = null;
			_jobAreaAndCalcSettings = JobAreaAndCalcSettings.Empty;
		}

		#endregion

		#region Public Properties - Derived

		public new bool InDesignMode => base.InDesignMode;

		public bool CurrentPosterIsDirty => CurrentPoster?.IsDirty ?? false;

		public string? CurrentPosterName => CurrentPoster?.Name;
		public bool CurrentPosterOnFile => CurrentPoster?.OnFile ?? false;

		public MapAreaInfo PosterAreaInfo => CurrentPoster?.MapAreaInfo ?? MapAreaInfo.Empty;

		#endregion

		#region Public Properties

		public SizeInt PosterSize => PosterAreaInfo.CanvasSize;

		public SizeDbl CanvasSize
		{
			get => _canvasSize;
			set
			{
				if(value != _canvasSize)
				{
					_canvasSize = value;
					OnPropertyChanged(nameof(IPosterViewModel.CanvasSize));

				}
			}
		}

		public SizeDbl LogicalDisplaySize
		{
			get => _logicalDisplaySize;
			set
			{
				if (value != _logicalDisplaySize)
				{
					_logicalDisplaySize = value;
					if (CurrentPoster != null)
					{
						UpdateMapView(CurrentPoster);
					}

					OnPropertyChanged(nameof(IPosterViewModel.LogicalDisplaySize));
				}
			}
		}

		public Poster? CurrentPoster
		{
			get => _currentPoster;
			private set
			{
				if(value != _currentPoster)
				{
					if (_currentPoster != null)
					{
						_currentPoster.PropertyChanged -= CurrentPoster_PropertyChanged;
					}

					_currentPoster = value;

					if (_currentPoster != null)
					{
						var dispPos = _currentPoster.DisplayPosition;
						OnPropertyChanged(nameof(IPosterViewModel.PosterSize));
						OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));

						// Setting the PosterSize and DisplayZoom can update the DisplayPosition. Use the value read from file.
						_currentPoster.DisplayPosition = dispPos;
						OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));

						JobAreaAndCalcSettings = GetNewJob(_currentPoster, DisplayPosition, LogicalDisplaySize.Round());
						_currentPoster.PropertyChanged += CurrentPoster_PropertyChanged;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentPoster));
					OnPropertyChanged(nameof(IPosterViewModel.ColorBandSet));
				}
			}
		}

		public ColorBandSet ColorBandSet
		{
			get => CurrentPoster?.ColorBandSet ?? new ColorBandSet();
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (value != ColorBandSet)
					{
						curPoster.ColorBandSet = value ?? new ColorBandSet();
						OnPropertyChanged(nameof(IPosterViewModel.ColorBandSet));
					}
				}
			}
		}

		public VectorInt DisplayPosition
		{
			get => CurrentPoster?.DisplayPosition ?? new VectorInt();
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (value != DisplayPosition)
					{
						curPoster.DisplayPosition = value;
						JobAreaAndCalcSettings = GetNewJob(curPoster, value, LogicalDisplaySize.Round());

						OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));
					}
				}
				else
				{
					JobAreaAndCalcSettings = JobAreaAndCalcSettings.Empty;
				}
			}
		}

		public double DisplayZoom
		{
			get => CurrentPoster?.DisplayZoom ?? 1;
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (Math.Abs(value - DisplayZoom) > 0.001)
					{
						curPoster.DisplayZoom = value;
						Debug.WriteLine($"The PosterViewModel's DispZoom is being updated to {value}.");
						OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
					}
				}
			}
		}

		/// <summary>
		/// Job Area for what is currently being displayed.
		/// </summary>
		public JobAreaAndCalcSettings JobAreaAndCalcSettings
		{
			get => _jobAreaAndCalcSettings;

			set
			{
				if (value != _jobAreaAndCalcSettings)
				{
					_jobAreaAndCalcSettings = value;
					OnPropertyChanged(nameof(IPosterViewModel.JobAreaAndCalcSettings));
				}
			}
		}

		#endregion

		#region Event Handlers

		private void CurrentPoster_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(Poster.IsDirty))
			{
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
			}

			else if (e.PropertyName == nameof(Poster.OnFile))
			{
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));
			}

			else if (e.PropertyName == nameof(DisplayZoom))
			{
				OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
			}
		}

		#endregion

		#region Public Methods

		public bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster)
		{
			return _projectAdapter.TryGetPoster(name, out poster);
		}

		public bool Open(string name)
		{
			if (_projectAdapter.TryGetPoster(name, out var poster))
			{
				CurrentPoster = poster;
				return true;
			}
			else
			{
				return false;
			}
		}

		public void Load(Poster poster)
		{
			CurrentPoster = poster;
		}

		public void Save()
		{
			var poster = CurrentPoster;

			if (poster != null)
			{
				if (!CurrentPosterOnFile)
				{
					throw new InvalidOperationException("Cannot save an unloaded project, use SaveProject instead.");
				}

				poster.Save(_projectAdapter);

				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
				OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));
			}
		}

		public bool SaveAs(string name, string? description)
		{
			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				return false;
			}

			if (_projectAdapter.TryGetPoster(name, out var existingPoster))
			{
				_projectAdapter.DeletePoster(existingPoster.Id);
			}

			var poster = new Poster(name, description, currentPoster.SourceJobId, currentPoster.MapAreaInfo, currentPoster.ColorBandSet, currentPoster.MapCalcSettings);

			if (poster is null)
			{
				return false;
			}
			else
			{
				poster.Save(_projectAdapter);
				CurrentPoster = poster;

				return true;
			}
		}

		public void Close()
		{
			CurrentPoster = null;
		}

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			//Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");

			//var currentPoster = CurrentPoster;

			//if (currentPoster == null)
			//{
			//	return;
			//}

			//// The new canvas size 
			//var canvasSize = newArea.Size;

			//var position = currentPoster.MapAreaInfo.Coords.Position;
			//var subdivision = currentPoster.MapAreaInfo.Subdivision;

			//// Use the new size and position to calculate the new map coordinates
			//var newCoords = RMapHelper.GetMapCoords(newArea, position, subdivision.SamplePointDelta);
			//var newMapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, subdivision, out var newCanvasControlOffset);

			//var newMapAreaInfo = new MapAreaInfo(newCoords, canvasSize, subdivision, newMapBlockOffset, newCanvasControlOffset);

			//UpdateMapView(currentPoster, newMapAreaInfo);
		}

		public void ResetMapView(MapAreaInfo newMapAreaInfo)
		{
			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				return;
			}

			// Update the current poster's map specification.
			currentPoster.MapAreaInfo = newMapAreaInfo;
			OnPropertyChanged(nameof(IPosterViewModel.PosterSize));
			currentPoster.DisplayPosition = new VectorInt();
			currentPoster.DisplayZoom = 1;

			_logicalDisplaySize = new SizeDbl(10, 10);
			LogicalDisplaySize = CanvasSize;

			//UpdateMapView(currentPoster);
		}

		private void UpdateMapView(Poster poster)
		{
			// Use the new map specification and the current zoom and display position to set the region to display.
			JobAreaAndCalcSettings = GetNewJob(poster, DisplayPosition, LogicalDisplaySize.Round());
		}

		public void UpdateColorBandSet(ColorBandSet colorBandSet)
		{
			if (CurrentPoster == null)
			{
				return;
			}

			if (ColorBandSet == colorBandSet)
			{
				Debug.WriteLine($"MapProjectViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
				return;
			}

			var targetIterations = colorBandSet.HighCutoff;

			if (targetIterations != CurrentPoster.MapCalcSettings.TargetIterations)
			{
				CurrentPoster.ColorBandSet = colorBandSet;

				Debug.WriteLine($"MapProjectViewModel is updating the Target Iterations. Current ColorBandSetId = {CurrentPoster.ColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				var mapCalcSettings = new MapCalcSettings(targetIterations, CurrentPoster.MapCalcSettings.RequestsPerJob);

				JobAreaAndCalcSettings = new JobAreaAndCalcSettings(JobAreaAndCalcSettings, mapCalcSettings);
			}
			else
			{
				Debug.WriteLine($"MapProjectViewModel is updating the ColorBandSet. Current ColorBandSetId = {CurrentPoster.ColorBandSet.Id}, New ColorBandSetId = {colorBandSet.Id}");
				CurrentPoster.ColorBandSet = colorBandSet;

				OnPropertyChanged(nameof(IPosterViewModel.ColorBandSet));
			}
		}

		#endregion

		#region Private Methods

		private JobAreaAndCalcSettings GetNewJob(Poster poster, VectorInt displayPosition, SizeInt logicalDisplaySize)
		{
			var viewPortArea = GetNewViewPort(poster.MapAreaInfo, displayPosition, logicalDisplaySize);

			var mapCalcSettingsCpy = poster.MapCalcSettings.Clone();
			//mapCalcSettingsCpy.DontFetchZValuesFromRepo = true;

			var jobAreaAndCalcSettings = new JobAreaAndCalcSettings(poster.SourceJobId.ToString(), JobOwnerType.Poster, viewPortArea, mapCalcSettingsCpy);

			return jobAreaAndCalcSettings;
		}

		private MapAreaInfo GetNewViewPort(MapAreaInfo currentAreaInfo, VectorInt displayPosition, SizeInt logicalDisplaySize)
		{
			var diagScreenArea = new RectangleInt(new PointInt(), currentAreaInfo.CanvasSize);
			var screenArea = new RectangleInt(new PointInt(displayPosition), logicalDisplaySize);
			var diagSqAmt = diagScreenArea.Width * diagScreenArea.Height;
			var screenSqAmt = screenArea.Width * screenArea.Height;
			var sizeRat = screenSqAmt / (double) diagSqAmt;

			Debug.WriteLine($"Creating ViewPort at pos: {displayPosition} and size: {logicalDisplaySize}.");
			Debug.WriteLine($"The new ViewPort covers {sizeRat}. Total Screen Area: {diagScreenArea}, viewPortArea: {screenArea}.");

			var totalLeft = diagScreenArea.Position.X + diagScreenArea.Width;
			var screenLeft = screenArea.Position.X + screenArea.Width;
			var totalTop = diagScreenArea.Position.Y + diagScreenArea.Height;
			var screenTop = screenArea.Position.Y + screenArea.Height;
			var trAmt = new VectorInt(Math.Max(screenLeft - totalLeft, 0), Math.Max(screenTop - totalTop, 0));
			screenArea = screenArea.Translate(trAmt);

			var mapPosition = currentAreaInfo.Coords.Position;
			var subdivision = currentAreaInfo.Subdivision;

			var newCoords = RMapHelper.GetMapCoords(screenArea, mapPosition, subdivision.SamplePointDelta);
			var newMapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, subdivision, out var newCanvasControlOffset);

			var result = new MapAreaInfo(newCoords, logicalDisplaySize, subdivision, newMapBlockOffset, newCanvasControlOffset);

			return result;
		}

		#endregion

		#region IDisposable Support

		private bool _disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					// Dispose managed state (managed objects)

					//if (CurrentPoster != null)
					//{
					//	CurrentPoster.Dispose();
					//	CurrentPoster = null;
					//}
				}

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		#endregion
	}
}

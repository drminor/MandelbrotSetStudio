using MSetRepo;
using MSS.Common;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		private readonly ProjectAdapter _projectAdapter;

		private SizeInt _canvasSize;
		private double _minimumDisplayZoom;
		private Poster? _currentPoster;

		JobAreaAndCalcSettings _jobAreaAndCalcSettings;

		#region Constructor

		public PosterViewModel(ProjectAdapter projectAdapter)
		{
			_projectAdapter = projectAdapter;

			_canvasSize = new SizeInt();
			_currentPoster = null;
			_jobAreaAndCalcSettings = JobAreaAndCalcSettings.Empty;
		}

		#endregion

		#region Public Properties - Derived

		public new bool InDesignMode => base.InDesignMode;

		public bool CurrentPosterIsDirty => CurrentPoster?.IsDirty ?? false;

		public string? CurrentPosterName => CurrentPoster?.Name;
		public bool CurrentPosterOnFile => CurrentPoster?.OnFile ?? false;

		#endregion

		#region Public Properties

		public SizeInt CanvasSize
		{
			get => _canvasSize;
			set
			{
				if(value != _canvasSize)
				{
					_canvasSize = value;
					MinimumDisplayZoom = GetMinimumDisplayZoom(CurrentPoster?.JobAreaInfo.CanvasSize, CanvasSize);

					OnPropertyChanged(nameof(IPosterViewModel.CanvasSize));

					//if (CurrentPoster != null)
					//{
					//	RerunWithNewDisplaySize(CurrentPoster);
					//}
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
					MinimumDisplayZoom = GetMinimumDisplayZoom(_currentPoster?.JobAreaInfo.CanvasSize, CanvasSize);

					if (_currentPoster != null)
					{
						var viewPortArea = GetNewViewPort(_currentPoster.JobAreaInfo, _currentPoster.DisplayPosition, CanvasSize, DisplayZoom);
						JobAreaAndCalcSettings = new JobAreaAndCalcSettings(viewPortArea, _currentPoster.MapCalcSettings);
						_currentPoster.PropertyChanged += CurrentPoster_PropertyChanged;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentPoster));
					OnPropertyChanged(nameof(IPosterViewModel.ColorBandSet));
				}
			}
		}

		public ColorBandSet? ColorBandSet
		{
			get => CurrentPoster?.ColorBandSet;
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
						var viewPortArea = GetNewViewPort(curPoster.JobAreaInfo, value, CanvasSize, DisplayZoom);
						JobAreaAndCalcSettings = new JobAreaAndCalcSettings(viewPortArea, curPoster.MapCalcSettings);

						curPoster.DisplayPosition = value;
						OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));
					}
				}
				else
				{
					JobAreaAndCalcSettings = JobAreaAndCalcSettings.Empty;
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
			get => CurrentPoster?.DisplayZoom ?? 1;
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (Math.Abs(value - DisplayZoom) > 0.001)
					{
						var newDisplayZoom = Math.Max(MinimumDisplayZoom, value);

						var viewPortArea = GetNewViewPort(curPoster.JobAreaInfo, curPoster.DisplayPosition, CanvasSize, newDisplayZoom);
						JobAreaAndCalcSettings = new JobAreaAndCalcSettings(viewPortArea, curPoster.MapCalcSettings);

						curPoster.DisplayZoom = newDisplayZoom;
						Debug.WriteLine($"The DispZoom is {DisplayZoom}.");
						OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
					}
				}
			}
		}

		public double MinimumDisplayZoom
		{
			get => _minimumDisplayZoom;
			private set
			{
				if (Math.Abs(value - _minimumDisplayZoom) > 0.001)
				{
					_minimumDisplayZoom = value;
					Debug.WriteLine($"The MinDispZoom is {MinimumDisplayZoom}.");
					OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
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

					// TODO: Handle Poster Canvas Size changes.
					MinimumDisplayZoom = GetMinimumDisplayZoom(CurrentPoster?.JobAreaInfo.CanvasSize, CanvasSize);

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
		}

		#endregion

		#region Public Methods

		public bool PosterOpen(string projectName)
		{
			if (_projectAdapter.TryGetPoster(projectName, out var poster))
			{
				CurrentPoster = poster;
				return true;
			}
			else
			{
				return false;
			}
		}

		public void PosterSave()
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

		public bool PosterSaveAs(string name, string? description)
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

			var poster = new Poster(name, description, currentPoster.SourceJobId, currentPoster.JobAreaInfo, currentPoster.ColorBandSet, currentPoster.MapCalcSettings);

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

		public void PosterClose()
		{
			CurrentPoster = null;
		}

		public void UpdateMapView(TransformType transformType, RectangleInt newArea)
		{
			Debug.Assert(transformType is TransformType.ZoomIn or TransformType.Pan or TransformType.ZoomOut, "UpdateMapView received a TransformType other than ZoomIn, Pan or ZoomOut.");

			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				return;
			}

			// The canvas size for this poster is not changing.
			var canvasSize = CanvasSize;

			var position = currentPoster.JobAreaInfo.Coords.Position;
			var subdivision = currentPoster.JobAreaInfo.Subdivision;

			var newCoords = RMapHelper.GetMapCoords(newArea, position, subdivision.SamplePointDelta);
			var newMapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, subdivision, out var newCanvasControlOffset);

			JobAreaAndCalcSettings = new JobAreaAndCalcSettings(
				new JobAreaInfo(newCoords, canvasSize, subdivision, newMapBlockOffset, newCanvasControlOffset),
				currentPoster.MapCalcSettings.Clone()
				);
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

				//LoadMap(CurrentPoster, CurrentPoster.JobAreaInfo.Coords, colorBandSet.Id, mapCalcSettings);
				JobAreaAndCalcSettings = new JobAreaAndCalcSettings(JobAreaAndCalcSettings.JobAreaInfo, mapCalcSettings);
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

		private double GetMinimumDisplayZoom(SizeInt? posterSize, SizeInt displaySize)
		{
			double result;

			if (posterSize != null)
			{
				var pixelsPerSampleHorizontal = displaySize.Width / (double)posterSize.Value.Width;
				var pixelsPerSampleVertical = displaySize.Height / (double)posterSize.Value.Height;

				result = Math.Max(pixelsPerSampleHorizontal, pixelsPerSampleVertical);
			}
			else
			{
				result = 0.9;
			}

			return result;
		}

		private JobAreaInfo GetNewViewPort(JobAreaInfo currentAreaInfo, VectorInt displayPosition, SizeInt displaySize, double displayZoom)
		{
			var logicalDispSize = displaySize.Scale(1 / displayZoom);

			var screenArea = new RectangleInt(new PointInt(displayPosition), logicalDispSize);
			var mapPosition = currentAreaInfo.Coords.Position;
			var subdivision = currentAreaInfo.Subdivision;

			var newCoords = RMapHelper.GetMapCoords(screenArea, mapPosition, subdivision.SamplePointDelta);
			var newMapBlockOffset = RMapHelper.GetMapBlockOffset(ref newCoords, subdivision, out var newCanvasControlOffset);

			var result = new JobAreaInfo(newCoords, logicalDispSize, subdivision, newMapBlockOffset, newCanvasControlOffset);

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

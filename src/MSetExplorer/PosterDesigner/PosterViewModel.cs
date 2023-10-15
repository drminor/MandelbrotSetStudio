using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly MapJobHelper _mapJobHelper;

		private Poster? _currentPoster;

		AreaColorAndCalcSettings _areaColorAndCalcSettings;
		private ColorBandSet? _previewColorBandSet;

		private readonly bool _useDetailedDebug;

		#endregion

		#region Constructor

		public PosterViewModel(IProjectAdapter projectAdapter, IMapSectionAdapter mapSectionAdapter, MapJobHelper mapJobHelper)
		{
			_useDetailedDebug = true;

			_projectAdapter = projectAdapter;
			_mapSectionAdapter = mapSectionAdapter;
			_mapJobHelper = mapJobHelper;

			_currentPoster = null;
			_areaColorAndCalcSettings = AreaColorAndCalcSettings.Empty;
			_previewColorBandSet = null;
		}

		#endregion

		#region Public Properties - Derived

		public new bool InDesignMode => base.InDesignMode;

		#endregion

		#region Public Properties

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
						if (CurrentPoster != null && !CurrentPoster.CurrentJob.IsEmpty)
						{
							var currentJob = CurrentPoster.CurrentJob;
							Debug.WriteLineIf(_useDetailedDebug, "The PosterViewModel is setting its CurrentAreaColorAndCalcSettings as its value of CurrentPoster is being updated.");

							var areaColorAndCalcSettings = new AreaColorAndCalcSettings(currentJob.Id.ToString(), OwnerType.Poster, currentJob.MapAreaInfo, CurrentPoster.CurrentColorBandSet, currentJob.MapCalcSettings.Clone());
							CurrentAreaColorAndCalcSettings = areaColorAndCalcSettings;
						}

						_currentPoster.PropertyChanged += CurrentPoster_PropertyChanged;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentPoster));
					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				}
			}
		}

		public bool CurrentPosterIsDirty => CurrentPoster?.IsDirty ?? false;

		public int GetGetNumberOfDirtyJobs()
		{
			return CurrentPoster?.GetNumberOfDirtyJobs() ?? 0;
		}

		public string? CurrentPosterName => CurrentPoster?.Name;
		public bool CurrentPosterOnFile => CurrentPoster?.OnFile ?? false;

		public Job CurrentJob
		{
			get => CurrentPoster?.CurrentJob ?? Job.Empty;
			set
			{
				var currentPoster = CurrentPoster;
				if (currentPoster != null)
				{
					if (value != CurrentJob)
					{
						currentPoster.CurrentJob = value;

						// TODO: Avoid reseting the DisplayPosition when creating a new Poster Job
						currentPoster.DisplayPosition = new VectorDbl();
						currentPoster.DisplayZoom = 1;

						UpdateCurrentAreaColorAndCalcSettings(currentPoster);
					}
					else
					{
						Debug.WriteLineIf(_useDetailedDebug, $"Not setting the CurrentJob {value.Id}, the CurrentJob already has this value.");
					}
				}
				else
				{
					Debug.WriteLine($"Not setting the CurrentJob {value.Id}, the CurrentPoster is null.");
				}
			}
		}

		private void UpdateCurrentAreaColorAndCalcSettings(Poster currentPoster)
		{
			if (!currentPoster.CurrentJob.IsEmpty)
			{
				var currentJob = currentPoster.CurrentJob;

				Debug.WriteLineIf(_useDetailedDebug, "The PosterViewModel is setting its CurrentAreaColorAndCalcSettings as its value of CurrentJob is being updated.");

				var areaColorAndCalcSettings = new AreaColorAndCalcSettings(currentJob.Id.ToString(), OwnerType.Poster, currentJob.MapAreaInfo, currentPoster.CurrentColorBandSet, currentJob.MapCalcSettings.Clone());
				CurrentAreaColorAndCalcSettings = areaColorAndCalcSettings;
			}
		}

		public MapCenterAndDelta PosterAreaInfo => CurrentPoster?.CurrentJob.MapAreaInfo ?? MapCenterAndDelta.Empty;

		public SizeDbl PosterSize
		{
			get => CurrentPoster?.PosterSize ?? new SizeDbl(1024);
			//set
			//{
			//	var curPoster = CurrentPoster;
			//	if (curPoster != null)
			//	{
			//		if (value != PosterSize)
			//		{
			//			curPoster.PosterSize = value;
			//		}
			//	}
			//}
		}

		public ColorBandSet CurrentColorBandSet
		{
			get => PreviewColorBandSet ?? CurrentPoster?.CurrentColorBandSet ?? new ColorBandSet();
			set
			{
				var currentPoster = CurrentPoster;
				if (currentPoster != null && !currentPoster.CurrentJob.IsEmpty)
				{
					CheckCurrentProject(currentPoster);

					// Discard the Preview ColorBandSet. 
					_previewColorBandSet = null;

					if (value == CurrentColorBandSet)
					{
						Debug.WriteLine($"PosterViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
					}

					var targetIterations = value.HighCutoff;
					var currentJob = currentPoster.CurrentJob;

					if (targetIterations != currentJob.MapCalcSettings.TargetIterations)
					{
						Debug.WriteLineIf(_useDetailedDebug, $"PosterViewModel is updating the Target Iterations. Current ColorBandSetId = {currentPoster.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");

						currentPoster.Add(value);

						_ = AddNewIterationUpdateJob(currentPoster, value);
					}
					else
					{
						Debug.WriteLine($"PosterViewModel is updating the ColorBandSet. Current ColorBandSetId = {currentPoster.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");
						currentPoster.CurrentColorBandSet = value;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				}
			}
		}

		public ColorBandSet? PreviewColorBandSet
		{
			get => _previewColorBandSet;
			set
			{
				if (value != _previewColorBandSet)
				{
					if (value == null || CurrentJob == null)
					{
						_previewColorBandSet = value;
					}
					else
					{
						var adjustedColorBandSet = ColorBandSetHelper.AdjustTargetIterations(value, CurrentJob.MapCalcSettings.TargetIterations);
						_previewColorBandSet = adjustedColorBandSet;
					}

					OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
				}
			}
		}

		public VectorDbl DisplayPosition
		{
			get => CurrentPoster?.DisplayPosition ?? new VectorDbl();
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (value != DisplayPosition)
					{
						Debug.WriteLineIf(_useDetailedDebug, $"The PosterViewModel's DisplayPosition is being updated to {value}.");
						curPoster.DisplayPosition = value;

						//OnPropertyChanged(nameof(IPosterViewModel.DisplayPosition));
					}
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
					if (Math.Abs(value - DisplayZoom) > RMapConstants.POSTER_DISPLAY_ZOOM_MIN_DIFF)
					{
						curPoster.DisplayZoom = value;
						Debug.WriteLineIf(_useDetailedDebug, $"The PosterViewModel's DisplayZoom is being updated to {value}.");
						//OnPropertyChanged(nameof(IPosterViewModel.DisplayZoom));
					}
				}
			}
		}

		/// <summary>
		/// Job Area for what is currently being displayed.
		/// </summary>
		public AreaColorAndCalcSettings CurrentAreaColorAndCalcSettings
		{
			get => _areaColorAndCalcSettings;

			set
			{
				if (value != _areaColorAndCalcSettings)
				{
					Debug.Assert(value.JobOwnerType == OwnerType.Poster, "The PosterViewModel is receiving a CurrentAreaColorAndCalcSetting that has an OwnerType other than 'Poster'.");
					_areaColorAndCalcSettings = value;
					OnPropertyChanged(nameof(IPosterViewModel.CurrentAreaColorAndCalcSettings));
				}
			}
		}

		private bool _saveTheZValues = false;
		public bool SaveTheZValues
		{
			get => _saveTheZValues;
			set
			{
				if (value != _saveTheZValues)
				{
					_saveTheZValues = value;
					OnPropertyChanged(nameof(IPosterViewModel.SaveTheZValues));
				}
				else
				{
					Debug.WriteLine($"ProjectViewModel is not updating the SaveTheZValues setting; the new value is the same as the existing value.");
				}
			}
		}

		private bool _calculateEscapeVelocities = true;
		public bool CalculateEscapeVelocities
		{
			get => _calculateEscapeVelocities;
			set
			{
				if (value != _calculateEscapeVelocities)
				{
					_calculateEscapeVelocities = value;
					OnPropertyChanged(nameof(IPosterViewModel.CalculateEscapeVelocities));
				}
				else
				{
					Debug.WriteLine($"ProjectViewModel is not updating the CalculateEscapeVelocities setting; the new value is the same as the existing value.");
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

			else if (e.PropertyName == nameof(Poster.CurrentColorBandSet))
			{
				Debug.WriteLineIf(_useDetailedDebug, "The PosterViewModel is raising PropertyChanged: IPosterViewModel.CurrentColorBandSet as the Poster's ColorBandSet is being updated.");
				OnPropertyChanged(nameof(IPosterViewModel.CurrentColorBandSet));
			}

			else if (e.PropertyName == nameof(Poster.CurrentJob))
			{
				Debug.WriteLineIf(_useDetailedDebug, "The PosterViewModel is updating the CurrentAreaColorAndCalcSettings as the Poster's CurrentJob is being updated.");

				if (CurrentPoster != null)
				{
					UpdateCurrentAreaColorAndCalcSettings(CurrentPoster);
				}

			}
		}

		#endregion

		#region Public Methods - Poster

		public bool TryGetPoster(string name, [MaybeNullWhen(false)] out Poster poster)
		{
			return _projectAdapter.TryGetPoster(name, out poster);
		}

		public bool PosterOpen(string name)
		{
			Debug.WriteLine($"\n\nOpening Poster: {name}.\n");
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

		public void PosterAddNewJobAndLoad(Poster poster, MapCenterAndDelta? newMapAreaInfo, SizeDbl posterSize)
		{
			if (newMapAreaInfo != null)
			{
				poster.PosterSize = posterSize;
				
				//var job = AddNewCoordinateUpdateJob(poster, newMapAreaInfo);
				//poster.CurrentJob = job;

				_ = AddNewCoordinateUpdateJob(poster, newMapAreaInfo);
			}

			PosterLoad(poster);
		}

		public void PosterLoad(Poster poster)
		{
			CurrentPoster = poster;
		}

		public bool PosterSave()
		{
			var poster = CurrentPoster;

			if (poster == null)
			{
				throw new InvalidOperationException("The poster must be non-null.");
			}

			if (!poster.OnFile)
			{
				throw new InvalidOperationException("Cannot save a new poster, use Save As instead.");
			}

			Debug.Assert(!poster.CurrentJob.IsEmpty, "Poster Savefound the CurrentJob to be empty.");


			Debug.WriteLineIf(_useDetailedDebug, $"Saving Poster: The CurrentJobId is {poster.CurrentJobId}.");

			poster.MarkAsDirty();

			var result = JobOwnerHelper.SavePoster(poster, _projectAdapter);
			
			OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterIsDirty));
			OnPropertyChanged(nameof(IPosterViewModel.CurrentPosterOnFile));

			return result;
		}

		public bool PosterSaveAs(string name, string? description, [MaybeNullWhen(true)] out string errorText)
		{
			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				throw new InvalidOperationException("The project must be non-null.");
			}

			if (_projectAdapter.PosterExists(name, out var posterId))
			{
				if (!ProjectAndMapSectionHelper.DeletePoster(posterId, _projectAdapter, _mapSectionAdapter, out var numberOfMapSectionsDeleted))
				{
					errorText = $"Could not delete existing poster having name: {name}";
					return false;
				}
				else
				{
					Debug.WriteLine($"As new Poster is being SavedAs, overwriting exiting poster: {name}, {numberOfMapSectionsDeleted} Map Sections were deleted.");
				}
			}

			Debug.Assert(!CurrentJob.IsEmpty, "ProjectSaveAs found the CurrentJob to be empty.");

			var poster = (Poster)JobOwnerHelper.CreateCopy(currentPoster, name, description, _projectAdapter, _mapSectionAdapter);

			if (JobOwnerHelper.SavePoster(poster, _projectAdapter))
			{
				CurrentPoster = poster;
				errorText = null;
				return true;
			}
			else
			{
				errorText = "Could not save the new poster record.";
				return false;
			}
		}

		public void PosterClose()
		{
			CurrentPoster = null;
		}

		public long DeleteMapSectionsForUnsavedJobs()
		{
			var currentPoster = CurrentPoster;

			if (currentPoster == null)
			{
				throw new InvalidOperationException("The project must be non-null.");
			}

			var result = JobOwnerHelper.DeleteMapSectionsForUnsavedJobs(currentPoster, _mapSectionAdapter);

			return result;
		}

		#endregion

		#region Public Methods - Job

		/// <summary>
		/// Calculate the adjusted MapAreaInfo using the new ScreenArea.
		/// Called in preparation to call UpdateMapSpecs
		/// </summary>
		/// <param name="mapAreaInfo">The original value </param>
		/// <param name="currentPosterSize">The original size in screen pixels</param>
		/// <param name="screenArea">The new size in screen pixels. ScreenTypeHelper.GetNewBoundingArea(OriginalMapArea, BeforeOffset, AfterOffset);</param>
		/// <returns></returns>
		public MapCenterAndDelta GetUpdatedMapAreaInfo(MapCenterAndDelta mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea, out double diagReciprocal)
		{
			var xFactor = newPosterSize.Width / currentPosterSize.Width;
			var yFactor = newPosterSize.Height / currentPosterSize.Height;
			var factor = Math.Min(xFactor, yFactor);

			// TODO: Use GetSmallestFactor
			//var factor = RMapHelper.GetSmallestScaleFactor(currentPosterSize, newPosterSize);

			var newCenter = screenArea.GetCenter();
			var oldCenter = new PointDbl(currentPosterSize.Scale(0.5));
			var zoomPoint = newCenter.Diff(oldCenter).Round();

			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPanThenZoom(mapAreaInfo, zoomPoint, factor, out diagReciprocal);

			Debug.WriteLineIf(_useDetailedDebug, $"PosterViewModel GetUpdatedMapAreaInfo:" +
				$"\n CurrentPosterSize: {currentPosterSize}, NewPosterSize: {newPosterSize}, ScreenArea: {screenArea}." +
				$"\n XFactor: {xFactor}, YFactor: {yFactor}, Factor: {factor}, Reciprocal: {diagReciprocal}." +
				$"\n NewCenter: {newCenter}, OldCenter: {oldCenter}, ZoomPoint: {zoomPoint}." +
				$"\n Using: {mapAreaInfo}" +
				$"\n Produces newMapAreaInfo: {newMapAreaInfo}.");

			return newMapAreaInfo;
		}

		public MapCenterAndDelta GetUpdatedMapAreaInfo(MapCenterAndDelta mapAreaInfo, TransformType transformType, VectorInt panAmount, double factor, out double diagReciprocal)
		{
			MapCenterAndDelta? newMapAreaInfo;

			if (transformType == TransformType.ZoomIn)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPanThenZoom(mapAreaInfo, panAmount, factor, out diagReciprocal);
			}
			else if (transformType == TransformType.Pan)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPan(mapAreaInfo, panAmount);
				diagReciprocal = 0.0;
			}
			else if (transformType == TransformType.ZoomOut)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoom(mapAreaInfo, factor, out diagReciprocal);
			}
			else
			{
				throw new InvalidOperationException($"AddNewCoordinateUpdateJob does not support a TransformType of {transformType}.");
			}

			return newMapAreaInfo;
		}

		// Always called after GetUpdatedMapAreaInfo
		public void AddNewCoordinateUpdateJob(MapCenterAndDelta newMapAreaInfo, SizeDbl posterSize)
		{
			if (CurrentPoster == null)
			{
				return;
			}

			CurrentPoster.PosterSize = posterSize;
			var job = AddNewCoordinateUpdateJob(CurrentPoster, newMapAreaInfo);

			if (CurrentPoster.CurrentJob != job)
			{
				throw new InvalidOperationException("Adding a job to the poster should set the value of the PosterViewModel's CurrentPoster's CurrentJob.");
			}
		}

		#endregion

		#region Private Methods

		// Create new Poster Specs using a new MapAreaInfo
		private Job AddNewCoordinateUpdateJob(Poster poster, MapCenterAndDelta mapAreaInfo)
		{
			var currentJob = poster.CurrentJob;
			Debug.Assert(!currentJob.IsEmpty, "AddNewCoordinateUpdateJob was called while the current job is empty.");

			var colorBandSetId = currentJob.ColorBandSetId;
			var mapCalcSettings = currentJob.MapCalcSettings;

			// TODO: Determine TransformType
			var transformType = TransformType.ZoomIn;

			var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, OwnerType.Poster, mapAreaInfo, colorBandSetId, mapCalcSettings, transformType, newArea: null);

			Debug.WriteLine($"Adding Poster Job with new coords: {mapAreaInfo.PositionAndDelta}. TransformType: {job.TransformType}. SamplePointDelta: {job.Subdivision.SamplePointDelta}, CanvasControlOffset: {job.CanvasControlOffset}");

			poster.Add(job);

			return job;
		}

		private Job AddNewIterationUpdateJob(Poster poster, ColorBandSet colorBandSet)
		{
			var currentJob = poster.CurrentJob;

			// Use the ColorBandSet's highCutoff to set the targetIterations of the current MapCalcSettings
			var targetIterations = colorBandSet.HighCutoff;
			var mapCalcSettings = MapCalcSettings.UpdateTargetIterations(currentJob.MapCalcSettings, targetIterations);

			// Use the current display size and Map Coordinates
			var mapAreaInfo = currentJob.MapAreaInfo;

			// This an iteration update with the same screen area
			var transformType = TransformType.IterationUpdate;
			var newScreenArea = new RectangleInt();

			var job = _mapJobHelper.BuildJob(currentJob.Id, poster.Id, OwnerType.Poster, mapAreaInfo, colorBandSet.Id, mapCalcSettings, transformType, newScreenArea);

			Debug.WriteLine($"Adding Poster Job with target iterations: {targetIterations}.");

			poster.Add(job);

			return job;
		}

		[Conditional("DEBUG2")]
		private void CheckCurrentProject(IJobOwner jobOwner)
		{
			if (jobOwner.CurrentJob.IsEmpty)
			{
				Debug.WriteLine($"The CurrentJob IsEmpty = {CurrentJob.IsEmpty}.");
			}
			else
			{
				if (jobOwner.CurrentColorBandSetId != jobOwner.CurrentJob.ColorBandSetId)
				{
					Debug.WriteLine($"The JobOwner's CurrentColorBandSet and CurrentJob's ColorBandSet are out of sync. The CurrentColorBandSet has {CurrentColorBandSet.Count} bands.");
				}
			}
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

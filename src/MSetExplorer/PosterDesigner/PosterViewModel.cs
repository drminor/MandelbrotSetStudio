using MongoDB.Bson;
using MSetRepo;
using MSS.Common;
using MSS.Common.MSet;
using MSS.Types;
using MSS.Types.MSet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace MSetExplorer
{
	internal class PosterViewModel : ViewModelBase, IPosterViewModel, IDisposable
	{
		#region Private Fields

		private readonly IProjectAdapter _projectAdapter;
		private readonly IMapSectionAdapter _mapSectionAdapter;

		private readonly MapJobHelper _mapJobHelper;
		private readonly MapSectionBuilder _mapSectionBuilder;

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
			_mapSectionBuilder = new MapSectionBuilder();

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

		public MapAreaInfo2 PosterAreaInfo => CurrentPoster?.CurrentJob.MapAreaInfo ?? MapAreaInfo2.Empty;

		public SizeDbl PosterSize
		{
			get => CurrentPoster?.PosterSize ?? new SizeDbl(1024);
			set
			{
				var curPoster = CurrentPoster;
				if (curPoster != null)
				{
					if (value != PosterSize)
					{
						curPoster.PosterSize = value;
					}
				}
			}
		}

		public ColorBandSet CurrentColorBandSet
		{
			get => PreviewColorBandSet ?? CurrentPoster?.CurrentColorBandSet ?? new ColorBandSet();
			set
			{
				var currentProject = CurrentPoster;
				if (currentProject != null && !currentProject.CurrentJob.IsEmpty)
				{
					CheckCurrentProject(currentProject);

					// Discard the Preview ColorBandSet. 
					_previewColorBandSet = null;

					if (value == CurrentColorBandSet)
					{
						Debug.WriteLine($"PosterViewModel is not updating the ColorBandSet; the new value is the same as the existing value.");
					}

					var targetIterations = value.HighCutoff;
					var currentJob = currentProject.CurrentJob;

					if (targetIterations != currentJob.MapCalcSettings.TargetIterations)
					{
						Debug.WriteLineIf(_useDetailedDebug, $"PosterViewModel is updating the Target Iterations. Current ColorBandSetId = {currentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");

						currentProject.Add(value);

						_ = AddNewIterationUpdateJob(currentProject, value);
					}
					else
					{
						Debug.WriteLine($"PosterViewModel is updating the ColorBandSet. Current ColorBandSetId = {currentProject.CurrentColorBandSet.Id}, New ColorBandSetId = {value.Id}");
						currentProject.CurrentColorBandSet = value;
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
					if (Math.Abs(value - DisplayZoom) > 0.001)
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
					Debug.Assert(value.JobOwnerType == OwnerType.Poster, "The PosterViewModel is receiving a CurrentAreaColorAndCalcSetting that has a JobOwnerType other than 'Poster'.");
					_areaColorAndCalcSettings = value;
					OnPropertyChanged(nameof(IPosterViewModel.CurrentAreaColorAndCalcSettings));
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

		public void Load(Poster poster, MapAreaInfo2? newMapArea)
		{
			if (newMapArea != null)
			{
				var job = AddNewCoordinateUpdateJob(poster, newMapArea);
				poster.CurrentJob = job;
			}

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

			if (JobOwnerHelper.SavePoster(currentPoster, _projectAdapter))
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

		public long DeleteNonEssentialMapSections(Job job, SizeDbl posterSize, bool aggressive)
		{
			var currentJobId = job.Id;
			var jobOwnerType = job.JobOwnerType;

			var jobOwnerId = job.OwnerId;	// Identifies either a Project or Poster record.

			var nonCurrentJobIds = _projectAdapter.GetAllJobIdsForPoster(jobOwnerId).Where(x => x != currentJobId).ToList();

			var numberOfMapSectionsDeleted = JobOwnerHelper.DeleteMapSectionsForJobIds(nonCurrentJobIds, OwnerType.Poster, _mapSectionAdapter);
			//numberOfMapSectionsDeleted += JobOwnerHelper.DeleteMapSectionsForJobIds(nonCurrentJobIds, OwnerType.ImageBuilder, _mapSectionAdapter);
			//numberOfMapSectionsDeleted += JobOwnerHelper.DeleteMapSectionsForJobIds(nonCurrentJobIds, OwnerType.BitmapBuilder, _mapSectionAdapter);

			if (aggressive)
			{
				// Get the set of MapSection required for this poster's current job.
				var mapSectionRequests = GetMapSectionRequests(job, posterSize);

				if (mapSectionRequests.Count == 0)
				{
					return numberOfMapSectionsDeleted;
				}

				// Get a list of all the MapSectionIds from the JobMapSection table.
				var allMapSectionIds = _mapSectionAdapter.GetMapSectionIds(currentJobId, jobOwnerType);

				if (allMapSectionIds.Count == 0)
				{
					return numberOfMapSectionsDeleted;
				}

				// For each MapSectionRequest in the provided list,
				// submit the request to fetch the MapSection from the repository if it exists.
				// If found, remove this MapSectionId from the list of allMapSectionIds.

				for (var i = 0; i < mapSectionRequests.Count; i++)
				{
					var mapSectionRequest = mapSectionRequests[i];
					var subdivisionId = new ObjectId(mapSectionRequest.SubdivisionId);
					var blockPosition = mapSectionRequest.BlockPosition;

					var mapSectionId = _mapSectionAdapter.GetMapSectionId(subdivisionId, blockPosition);

					if (mapSectionId != null)
					{
						allMapSectionIds.Remove(mapSectionId.Value);
					}
				}

				// Now the AllMapSectionIds only contains Ids not required by the current Project or Poster.

				// Delete all MapSection Records not required, that belong to the current Job.
				var result = _mapSectionAdapter.DeleteMapSectionsWithJobType(allMapSectionIds, jobOwnerType);

				if (result.HasValue)
				{
					numberOfMapSectionsDeleted += result.Value;
				}
			}

			return numberOfMapSectionsDeleted;
		}

		private List<MapSectionRequest> GetMapSectionRequests(Job job, SizeDbl posterSize)
		{
			var jobId = job.Id;
			var jobOwnerType = job.JobOwnerType;
			var mapAreaInfo = job.MapAreaInfo;
			var mapCalcSettings = job.MapCalcSettings;

			var mapAreaInfoV1 = _mapJobHelper.GetMapAreaWithSizeFat(mapAreaInfo, posterSize);
			var emptyMapSections = _mapSectionBuilder.CreateEmptyMapSections(mapAreaInfoV1, mapCalcSettings);
			var mapSectionRequests = _mapSectionBuilder.CreateSectionRequestsFromMapSections(JobType.FullScale, jobId.ToString(), jobOwnerType, mapAreaInfoV1, mapCalcSettings, emptyMapSections);

			return mapSectionRequests;
		}

		//public List<ObjectId> GetAllNonCurrentJobIds()
		//{
		//	var currentProject = CurrentPoster;
		//	if (currentProject != null && !currentProject.CurrentJob.IsEmpty)
		//	{
		//		var currentJobId = currentProject.CurrentJob.Id;

		//		var result = currentProject.GetJobs().Where(x => x.Id != currentJobId).Select(x => x.Id).ToList();
		//		return result;
		//	}
		//	else
		//	{
		//		return Enumerable.Empty<ObjectId>().ToList();
		//	}
		//}

		//public List<ObjectId> GetAllJobIdsNotMatchingCurrentSPD()
		//{
		//	var currentProject = CurrentPoster;
		//	if (currentProject != null && !currentProject.CurrentJob.IsEmpty)
		//	{
		//		var currentSpdWidth = currentProject.CurrentJob.Subdivision.SamplePointDelta.WidthNumerator;

		//		var result = currentProject.GetJobs().Where(x => x.Subdivision.SamplePointDelta.WidthNumerator != currentSpdWidth).Select(x => x.Id).ToList();
		//		return result;
		//	}
		//	else
		//	{
		//		return Enumerable.Empty<ObjectId>().ToList();
		//	}
		//}

		//public ObjectId? GetJobForZoomLevelOne()
		//{
		//	var currentProject = CurrentPoster;
		//	if (currentProject != null && !currentProject.CurrentJob.IsEmpty)
		//	{
		//		var currentSpdWidth = currentProject.CurrentJob.Subdivision.SamplePointDelta.WidthNumerator;

		//		var minExponent = currentProject.GetJobs().Where(x => x.Subdivision.SamplePointDelta.WidthNumerator == currentSpdWidth).Min(x => x.Subdivision.SamplePointDelta.Exponent);

		//		var result = currentProject.GetJobs().Where(x => x.Subdivision.SamplePointDelta.WidthNumerator == currentSpdWidth && x.Subdivision.SamplePointDelta.Exponent == minExponent).FirstOrDefault();

		//		return result?.Id ?? null;
		//	}
		//	else
		//	{
		//		return null;
		//	}
		//}

		//public List<ObjectId> GetJobIdsExceptZoomLevelOne()
		//{
		//	var currentProject = CurrentPoster;
		//	if (currentProject != null && !currentProject.CurrentJob.IsEmpty)
		//	{
		//		var currentSpdWidth = currentProject.CurrentJob.Subdivision.SamplePointDelta.WidthNumerator;

		//		var minExponent = currentProject.GetJobs().Where(x => x.Subdivision.SamplePointDelta.WidthNumerator == currentSpdWidth).Min(x => x.Subdivision.SamplePointDelta.Exponent);

		//		var result = currentProject.GetJobs().Where(x => x.Subdivision.SamplePointDelta.WidthNumerator != currentSpdWidth || x.Subdivision.SamplePointDelta.Exponent != minExponent).Select(x => x.Id).ToList();
		//		return result;
		//	}
		//	else
		//	{
		//		return Enumerable.Empty<ObjectId>().ToList();
		//	}
		//}

		#endregion

		#region Public Methods - Job

		// Called in preparation to call UpdateMapSpecs

		/// <summary>
		/// Calculate the adjusted MapAreaInfo using the new ScreenArea
		/// </summary>
		/// <param name="mapAreaInfo">The original value </param>
		/// <param name="currentPosterSize">The original size in screen pixels</param>
		/// <param name="screenArea">The new size in screen pixels. ScreenTypeHelper.GetNewBoundingArea(OriginalMapArea, BeforeOffset, AfterOffset);</param>
		/// <returns></returns>
		public MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, SizeDbl currentPosterSize, SizeDbl newPosterSize, RectangleDbl screenArea, out double diagReciprocal)
		{
			var xFactor = newPosterSize.Width / currentPosterSize.Width;
			var yFactor = newPosterSize.Height / currentPosterSize.Height;
			var factor = Math.Min(xFactor, yFactor);

			var newCenter = screenArea.GetCenter();
			var oldCenter = new PointDbl(currentPosterSize.Scale(0.5));
			var zoomPoint = newCenter.Diff(oldCenter).Round();

			var newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(mapAreaInfo, zoomPoint, factor, out diagReciprocal);

			Debug.WriteLineIf(_useDetailedDebug, $"PosterViewModel GetUpdatedMapAreaInfo:" +
				$"\n CurrentPosterSize: {currentPosterSize}, NewPosterSize: {newPosterSize}, ScreenArea: {screenArea}." +
				$"\n XFactor: {xFactor}, YFactor: {yFactor}, Factor: {factor}, Reciprocal: {diagReciprocal}." +
				$"\n NewCenter: {newCenter}, OldCenter: {oldCenter}, ZoomPoint: {zoomPoint}." +
				$"\n Using: {mapAreaInfo}" +
				$"\n Produces newMapAreaInfo: {newMapAreaInfo}.");

			return newMapAreaInfo;
		}

		public MapAreaInfo2 GetUpdatedMapAreaInfo(MapAreaInfo2 mapAreaInfo, TransformType transformType, VectorInt panAmount, double factor, out double diagReciprocal)
		{
			MapAreaInfo2? newMapAreaInfo;

			if (transformType == TransformType.ZoomIn)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomPoint(mapAreaInfo, panAmount, factor, out diagReciprocal);
			}
			else if (transformType == TransformType.Pan)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoPan(mapAreaInfo, panAmount);
				diagReciprocal = 0.0;
			}
			else if (transformType == TransformType.ZoomOut)
			{
				newMapAreaInfo = _mapJobHelper.GetMapAreaInfoZoomCenter(mapAreaInfo, factor, out diagReciprocal);
			}
			else
			{
				throw new InvalidOperationException($"AddNewCoordinateUpdateJob does not support a TransformType of {transformType}.");
			}

			return newMapAreaInfo;
		}

		// Always called after GetUpdatedMapAreaInfo
		public void UpdateMapSpecs(MapAreaInfo2 newMapAreaInfo)
		{
			if (CurrentPoster == null)
			{
				return;
			}

			_ = AddNewCoordinateUpdateJob(CurrentPoster, newMapAreaInfo);
		}

		#endregion

		#region Private Methods

		// Create new Poster Specs using a new MapAreaInfo
		private Job AddNewCoordinateUpdateJob(Poster poster, MapAreaInfo2 mapAreaInfo)
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
